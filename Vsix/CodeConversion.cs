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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    class CodeConversion
    {
        public Func<ConverterOptionsPage> GetOptions { get; }
        private readonly IServiceProvider _serviceProvider;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        public static readonly string ConverterTitle = "Code converter";
        private static readonly string Intro = Environment.NewLine + Environment.NewLine + new string(Enumerable.Repeat('-', 80).ToArray()) + Environment.NewLine;
        private readonly VisualStudioInteraction.OutputWindow _outputWindow;
        private string SolutionDir => Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath);

        public CodeConversion(IServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace,
            Func<ConverterOptionsPage> getOptions)
        {
            GetOptions = getOptions;
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
            _outputWindow = new VisualStudioInteraction.OutputWindow();
        }
        
        public async Task PerformProjectConversion<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects) where TLanguageConversion : ILanguageConversion, new()
        {
            await Task.Run(() => {
                var convertedFiles = ConvertProjectUnhandled<TLanguageConversion>(selectedProjects);
                WriteConvertedFilesAndShowSummary(convertedFiles);
            });
        }

        public async Task PerformDocumentConversion<TLanguageConversion>(string documentFilePath, Span selected) where TLanguageConversion : ILanguageConversion, new()
        {
            var conversionResult = await Task.Run(async () => {
                var result = await ConvertDocumentUnhandled<TLanguageConversion>(documentFilePath, selected);
                WriteConvertedFilesAndShowSummary(new[] { result });
                return result;
            });

            if (GetOptions().CopyResultToClipboardForSingleDocument) {
                Clipboard.SetText(conversionResult.ConvertedCode ?? conversionResult.GetExceptionsAsString());
                _outputWindow.WriteToOutputWindow("Conversion result copied to clipboard.");
                VisualStudioInteraction.ShowMessageBox(_serviceProvider, "Conversion result copied to clipboard.", conversionResult.GetExceptionsAsString(), false);
            }

        }

        private void WriteConvertedFilesAndShowSummary(IEnumerable<ConversionResult> convertedFiles)
        {
            _outputWindow.Clear();
            _outputWindow.WriteToOutputWindow(Intro);
            _outputWindow.ForceShowOutputPane();

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

                LogProgress(convertedFile, errors);
                if (string.IsNullOrWhiteSpace(convertedFile.ConvertedCode)) continue;

                files.Add(convertedFile.TargetPathOrNull);

                if (convertedFile.ConvertedCode.Length > longestFileLength) {
                    longestFileLength = convertedFile.ConvertedCode.Length;
                    longestFilePath = convertedFile.TargetPathOrNull;
                }

                convertedFile.WriteToFile();
            }

            FinalizeConversion(files, errors, longestFilePath, filesToOverwrite);
        }

        private void FinalizeConversion(List<string> files, List<string> errors, string longestFilePath, List<ConversionResult> filesToOverwrite)
        {
            var options = GetOptions();

            var pathsToOverwrite = filesToOverwrite.Select(f => PathRelativeToSolutionDir(f.SourcePathOrNull));
            var shouldOverwriteSolutionAndProjectFiles =
                filesToOverwrite.Any() &&
                (options.AlwaysOverwriteFiles || UserHasConfirmedOverwrite(files, errors, pathsToOverwrite.ToList()));

            if (shouldOverwriteSolutionAndProjectFiles)
            {
                var titleMessage = options.CreateBackups ? "Creating backups and overwriting files:" : "Overwriting files:" + "";
                _outputWindow.WriteToOutputWindow(Environment.NewLine + titleMessage);
                foreach (var fileToOverwrite in filesToOverwrite)
                {
                    if (options.CreateBackups) File.Copy(fileToOverwrite.SourcePathOrNull, fileToOverwrite.SourcePathOrNull + ".bak", true);
                    fileToOverwrite.WriteToFile();

                    var targetPathRelativeToSolutionDir = PathRelativeToSolutionDir(fileToOverwrite.TargetPathOrNull);
                    _outputWindow.WriteToOutputWindow($"* {targetPathRelativeToSolutionDir}");
                }
                files = files.Concat(filesToOverwrite.Select(f => f.SourcePathOrNull)).ToList();
            } else if (longestFilePath != null) {
                VisualStudioInteraction.OpenFile(new FileInfo(longestFilePath)).SelectAll();
            }

            var conversionSummary = GetConversionSummary(files, errors);
            _outputWindow.WriteToOutputWindow(conversionSummary);
            _outputWindow.ForceShowOutputPane();

        }

        private bool UserHasConfirmedOverwrite(List<string> files, List<string> errors, IReadOnlyCollection<string> pathsToOverwrite)
        {
            var maxExamples = 30; // Avoid a huge unreadable dialog going off the screen
            var exampleText = pathsToOverwrite.Count > maxExamples ? $". First {maxExamples} examples" : "";
            return VisualStudioInteraction.ShowMessageBox(_serviceProvider,
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

        private void LogProgress(ConversionResult convertedFile, List<string> errors)
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

            _outputWindow.WriteToOutputWindow(output);
        }

        private string PathRelativeToSolutionDir(string path)
        {
            return path.Replace(SolutionDir, "")
                .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private string GetConversionSummary(IReadOnlyCollection<string> files, IReadOnlyCollection<string> errors)
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

            WriteStatusBarText(oneLine + " - see output window");
            return Environment.NewLine + Environment.NewLine
                                       + oneLine
                                       + Environment.NewLine + successSummary
                                       + Environment.NewLine;
        }

        async Task<ConversionResult> ConvertDocumentUnhandled<TLanguageConversion>(string documentPath, Span selected) where TLanguageConversion : ILanguageConversion, new()
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

        private IEnumerable<ConversionResult> ConvertProjectUnhandled<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects)
            where TLanguageConversion : ILanguageConversion, new()
        {
            var projectsByPath =
                _visualStudioWorkspace.CurrentSolution.Projects.ToLookup(p => p.FilePath, p => p);
            var projects = selectedProjects.Select(p => projectsByPath[p.FullName].First()).ToList();
            var convertedFiles = SolutionConverter.CreateFor<TLanguageConversion>(projects, new Progress<string>(s => _outputWindow.WriteToOutputWindow(Environment.NewLine + s))).Convert();
            return convertedFiles;
        }

        void WriteStatusBarText(string text)
        {
            IVsStatusbar statusBar = (IVsStatusbar)_serviceProvider.GetService(typeof(SVsStatusbar));
            if (statusBar == null)
                return;

            int frozen;
            statusBar.IsFrozen(out frozen);
            if (frozen != 0) {
                statusBar.FreezeOutput(0);
            }

            statusBar.SetText(text);
            statusBar.FreezeOutput(1);
        }
        
        public static bool IsCSFileName(string fileName)
        {
            return fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVBFileName(string fileName)
        {
            return fileName.EndsWith(".vb", StringComparison.OrdinalIgnoreCase);
        }

        public ITextSelection GetSelectionInCurrentView(Func<string, bool> predicate)
        {
            IWpfTextViewHost viewHost = GetCurrentViewHost(predicate);
            if (viewHost == null)
                return null;

            return viewHost.TextView.Selection;
        }

        public IWpfTextViewHost GetCurrentViewHost(Func<string, bool> predicate)
        {
            IWpfTextViewHost viewHost = VisualStudioInteraction.GetCurrentViewHost(_serviceProvider);
            if (viewHost == null)
                return null;

            ITextDocument textDocument = viewHost.GetTextDocument();
            if (textDocument == null || !predicate(textDocument.FilePath))
                return null;

            return viewHost;
        }
    }
}
