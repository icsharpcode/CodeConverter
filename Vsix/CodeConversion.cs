using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    class CodeConversion
    {
        public Func<Task<ConverterOptionsPage>> GetOptions { get; }
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        public static readonly string ConverterTitle = "Code converter";
        private static readonly string Intro = Environment.NewLine + Environment.NewLine + new string(Enumerable.Repeat('-', 80).ToArray()) + Environment.NewLine;
        private readonly VisualStudioInteraction.OutputWindow _outputWindow;
        private string SolutionDir => Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath);

        public static async Task<CodeConversion> CreateAsync(REConverterPackage serviceProvider, VisualStudioWorkspace visualStudioWorkspace, Func<Task<ConverterOptionsPage>> getOptions)
        {
            return new CodeConversion(serviceProvider, visualStudioWorkspace, 
                getOptions, await VisualStudioInteraction.OutputWindow.CreateAsync());
        }

        public CodeConversion(IAsyncServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace,
            Func<Task<ConverterOptionsPage>> getOptions, VisualStudioInteraction.OutputWindow outputWindow)
        {
            GetOptions = getOptions;
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
            _outputWindow = outputWindow;
        }
        
        public async Task PerformProjectConversionAsync<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects) where TLanguageConversion : ILanguageConversion, new()
        {
            await Task.Run(async () => {
                var convertedFiles = ConvertProjectUnhandledAsync<TLanguageConversion>(selectedProjects);
                await WriteConvertedFilesAndShowSummaryAsync(await convertedFiles);
            });
        }

        public async Task PerformDocumentConversionAsync<TLanguageConversion>(string documentFilePath, Span selected) where TLanguageConversion : ILanguageConversion, new()
        {
            var conversionResult = await Task.Run(async () => {
                var result = await ConvertDocumentUnhandledAsync<TLanguageConversion>(documentFilePath, selected);
                await WriteConvertedFilesAndShowSummaryAsync(new[] { result });
                return result;
            });

            if ((await GetOptions()).CopyResultToClipboardForSingleDocument) {
                Clipboard.SetText(conversionResult.ConvertedCode ?? conversionResult.GetExceptionsAsString());
                await _outputWindow.WriteToOutputWindowAsync("Conversion result copied to clipboard.");
                await VisualStudioInteraction.ShowMessageBoxAsync(_serviceProvider, "Conversion result copied to clipboard.", conversionResult.GetExceptionsAsString(), false);
            }

        }

        private async Task WriteConvertedFilesAndShowSummaryAsync(IEnumerable<ConversionResult> convertedFiles)
        {
            await _outputWindow.ClearAsync();
            await _outputWindow.WriteToOutputWindowAsync(Intro);
            await _outputWindow.ForceShowOutputPaneAsync();

            var files = new List<string>();
            var filesToOverwrite = new List<ConversionResult>();
            var errors = new List<string>();
            string longestFilePath = null;
            var longestFileLength = -1;
            foreach (var convertedFile in convertedFiles) {
                if (convertedFile.SourcePathOrNull == null) continue;

                if (WillOverwriteSource(convertedFile)) {
                    filesToOverwrite.Add(convertedFile);
                    continue;
                }

                await LogProgressAsync(convertedFile, errors);
                if (string.IsNullOrWhiteSpace(convertedFile.ConvertedCode)) continue;

                files.Add(convertedFile.TargetPathOrNull);

                if (convertedFile.ConvertedCode.Length > longestFileLength) {
                    longestFileLength = convertedFile.ConvertedCode.Length;
                    longestFilePath = convertedFile.TargetPathOrNull;
                }

                convertedFile.WriteToFile();
            }

            await FinalizeConversionAsync(files, errors, longestFilePath, filesToOverwrite);
        }

        private async Task FinalizeConversionAsync(List<string> files, List<string> errors, string longestFilePath, List<ConversionResult> filesToOverwrite)
        {
            var options = await GetOptions();

            var pathsToOverwrite = filesToOverwrite.Select(f => PathRelativeToSolutionDir(f.SourcePathOrNull));
            var shouldOverwriteSolutionAndProjectFiles =
                filesToOverwrite.Any() &&
                (options.AlwaysOverwriteFiles || await UserHasConfirmedOverwriteAsync(files, errors, pathsToOverwrite.ToList()));

            if (shouldOverwriteSolutionAndProjectFiles)
            {
                var titleMessage = options.CreateBackups ? "Creating backups and overwriting files:" : "Overwriting files:" + "";
                await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + titleMessage);
                foreach (var fileToOverwrite in filesToOverwrite)
                {
                    if (options.CreateBackups) File.Copy(fileToOverwrite.SourcePathOrNull, fileToOverwrite.SourcePathOrNull + ".bak", true);
                    fileToOverwrite.WriteToFile();

                    var targetPathRelativeToSolutionDir = PathRelativeToSolutionDir(fileToOverwrite.TargetPathOrNull);
                    await _outputWindow.WriteToOutputWindowAsync($"* {targetPathRelativeToSolutionDir}");
                }
                files = files.Concat(filesToOverwrite.Select(f => f.SourcePathOrNull)).ToList();
            } else if (longestFilePath != null) {
                await (await VisualStudioInteraction.OpenFileAsync(new FileInfo(longestFilePath))).SelectAllAsync();
            }

            var conversionSummary = await GetConversionSummaryAsync(files, errors);
            await _outputWindow.WriteToOutputWindowAsync(conversionSummary);
            await _outputWindow.ForceShowOutputPaneAsync();

        }

        private Task<bool> UserHasConfirmedOverwriteAsync(List<string> files, List<string> errors, IReadOnlyCollection<string> pathsToOverwrite)
        {
            var maxExamples = 30; // Avoid a huge unreadable dialog going off the screen
            var exampleText = pathsToOverwrite.Count > maxExamples ? $". First {maxExamples} examples" : "";
            return VisualStudioInteraction.ShowMessageBoxAsync(_serviceProvider,
                "Overwrite solution and referencing projects?",
                $@"The current solution file and any referencing projects will be overwritten to reference the new project(s){exampleText}:
* {string.Join(Environment.NewLine + "* ", pathsToOverwrite.Take(maxExamples))}

The old contents will be copied to 'currentFilename.bak'.
Please 'Reload All' when Visual Studio prompts you.", true, files.Count > errors.Count);
        }

        private static bool WillOverwriteSource(ConversionResult convertedFile)
        {
            return string.Equals(convertedFile.SourcePathOrNull, convertedFile.TargetPathOrNull, StringComparison.OrdinalIgnoreCase);
        }

        private async Task LogProgressAsync(ConversionResult convertedFile, List<string> errors)
        {
            var exceptionsAsString = convertedFile.GetExceptionsAsString();
            var indentedException = exceptionsAsString.Replace(Environment.NewLine, Environment.NewLine + "    ").TrimEnd();
            var targetPathRelativeToSolutionDir = PathRelativeToSolutionDir(convertedFile.TargetPathOrNull ?? "unknown");
            string output = $"* {targetPathRelativeToSolutionDir}";
            var containsErrors = !string.IsNullOrWhiteSpace(exceptionsAsString);

            if (containsErrors) {
                errors.Add(exceptionsAsString);
            }

            if (string.IsNullOrWhiteSpace(convertedFile.ConvertedCode))
            {
                var sourcePathRelativeToSolutionDir = PathRelativeToSolutionDir(convertedFile.SourcePathOrNull ?? "unknown");
                output = $"* Failure processing {sourcePathRelativeToSolutionDir}{Environment.NewLine}    {indentedException}";    
            }
            else if (containsErrors){
                output += $" contains errors{Environment.NewLine}    {indentedException}";
            }

            await _outputWindow.WriteToOutputWindowAsync(output);
        }

        private string PathRelativeToSolutionDir(string path)
        {
            return path.Replace(SolutionDir, "")
                .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private async Task<string> GetConversionSummaryAsync(IReadOnlyCollection<string> files, IReadOnlyCollection<string> errors)
        {
            var oneLine = "Code conversion failed";
            var successSummary = "";
            if (files.Any()) {
                oneLine = "Code conversion completed";
                successSummary = $"{files.Count} files have been written to disk.";
            }

            if (errors.Any()) {
                oneLine += $" with {errors.Count} error" + (errors.Count == 1 ? "" : "s");
            }

            if (files.Count > errors.Count * 2) {
                successSummary += Environment.NewLine + "Please report issues at https://github.com/icsharpcode/CodeConverter/issues and consider rating at https://marketplace.visualstudio.com/items?itemName=SharpDevelopTeam.CodeConverter#review-details";
            } else {
                successSummary += Environment.NewLine + "Please report issues at https://github.com/icsharpcode/CodeConverter/issues";
            }

            await VisualStudioInteraction.WriteStatusBarTextAsync(_serviceProvider, oneLine + " - see output window");
            return Environment.NewLine + Environment.NewLine
                                       + oneLine
                                       + Environment.NewLine + successSummary
                                       + Environment.NewLine;
        }

        async Task<ConversionResult> ConvertDocumentUnhandledAsync<TLanguageConversion>(string documentPath, Span selected) where TLanguageConversion : ILanguageConversion, new()
        {   
            //TODO Figure out when there are multiple document ids for a single file path
            var documentId = _visualStudioWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(documentPath).SingleOrDefault();
            if (documentId == null) {
                //If document doesn't belong to any project
                return ConvertTextOnly<TLanguageConversion>(documentPath, selected);
            }
            var document = _visualStudioWorkspace.CurrentSolution.GetDocument(documentId);
            var compilation = await document.Project.GetCompilationAsync();
            var documentSyntaxTree = await document.GetSyntaxTreeAsync();

            var selectedTextSpan = new TextSpan(selected.Start, selected.Length);
            return await ProjectConversion.ConvertSingle(compilation, documentSyntaxTree, selectedTextSpan, new TLanguageConversion(), document.Project);
        }

        private static ConversionResult ConvertTextOnly<TLanguageConversion>(string documentPath, Span selected)
            where TLanguageConversion : ILanguageConversion, new()
        {
            var documentText = File.ReadAllText(documentPath);
            if (selected.Length > 0 && documentText.Length >= selected.End)
            {
                documentText = documentText.Substring(selected.Start, selected.Length);
            }

            var convertTextOnly = ProjectConversion.ConvertText<TLanguageConversion>(documentText, DefaultReferences.NetStandard2);
            convertTextOnly.SourcePathOrNull = documentPath;
            return convertTextOnly;
        }

        private async Task<IEnumerable<ConversionResult>> ConvertProjectUnhandledAsync<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects)
            where TLanguageConversion : ILanguageConversion, new()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var projectsByPath =
                _visualStudioWorkspace.CurrentSolution.Projects.ToLookup(p => p.FilePath, p => p);
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread - ToList ensures this happens within the same thread just switched to above
            var projects = selectedProjects.Select(p => projectsByPath[p.FullName].First()).ToList();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            var solutionConverter = SolutionConverter.CreateFor<TLanguageConversion>(projects, new Progress<string>(s => {
                var unusedFireAndForget = LogProgressAsync(s);
            }));
            
            return await ThreadHelper.JoinableTaskFactory.RunAsync(() => solutionConverter.Convert());
        }

        private async Task LogProgressAsync(string s)
        {
            try {
                await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + s);
            } catch (Exception) {
                //Logging failed. TODO consider logging such issues to external file
            }
        }

        public static bool IsCSFileName(string fileName)
        {
            return fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVBFileName(string fileName)
        {
            return fileName.EndsWith(".vb", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ITextSelection> GetSelectionInCurrentViewAsync(Func<string, bool> predicate)
        {
            IWpfTextViewHost viewHost = await GetCurrentViewHostAsync(predicate);
            if (viewHost == null)
                return null;

            return viewHost.TextView.Selection;
        }

        public async Task<IWpfTextViewHost> GetCurrentViewHostAsync(Func<string, bool> predicate)
        {
            IWpfTextViewHost viewHost = await VisualStudioInteraction.GetCurrentViewHostAsync(_serviceProvider);
            if (viewHost == null)
                return null;

            ITextDocument textDocument = await viewHost.GetTextDocumentAsync();
            if (textDocument == null || !predicate(textDocument.FilePath))
                return null;

            return viewHost;
        }
    }
}
