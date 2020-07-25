using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace ICSharpCode.CodeConverter.VsExtension
{
    internal class CodeConversion
    {
        public Func<Task<ConverterOptionsPage>> GetOptions { get; }
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private static readonly string Intro = Environment.NewLine + Environment.NewLine + new string(Enumerable.Repeat('-', 80).ToArray()) + Environment.NewLine;
        private readonly OutputWindow _outputWindow;
        private readonly Cancellation _packageCancellation;

        private string SolutionDir => Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath);

        public static async Task<CodeConversion> CreateAsync(CodeConverterPackage serviceProvider, VisualStudioWorkspace visualStudioWorkspace, Func<Task<ConverterOptionsPage>> getOptions)
        {
            return new CodeConversion(serviceProvider, serviceProvider.JoinableTaskFactory, serviceProvider.PackageCancellation, visualStudioWorkspace,
                getOptions, await OutputWindow.CreateAsync());
        }

        public CodeConversion(IAsyncServiceProvider serviceProvider,
            JoinableTaskFactory joinableTaskFactory, Cancellation packageCancellation, VisualStudioWorkspace visualStudioWorkspace,
            Func<Task<ConverterOptionsPage>> getOptions, OutputWindow outputWindow)
        {
            JoinableTaskFactorySingleton.Instance = joinableTaskFactory;
            GetOptions = getOptions;
            _serviceProvider = serviceProvider;
            _joinableTaskFactory = joinableTaskFactory;
            _visualStudioWorkspace = visualStudioWorkspace;
            _outputWindow = outputWindow;
            _packageCancellation = packageCancellation;
        }

        public async Task ConvertProjectsAsync<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects, CancellationToken cancellationToken) where TLanguageConversion : ILanguageConversion, new()
        {
            try {
                await EnsureBuiltAsync();
                await _joinableTaskFactory.RunAsync(async () => {
                    var convertedFiles = ConvertProjectUnhandled<TLanguageConversion>(selectedProjects, cancellationToken);
                    await WriteConvertedFilesAndShowSummaryAsync(convertedFiles);
                });
            } catch (OperationCanceledException) {
                if (!_packageCancellation.CancelAll.IsCancellationRequested) {
                    await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + "Previous conversion cancelled", forceShow: true);
                }
            }
        }

        public async Task ConvertDocumentAsync<TLanguageConversion>(string documentFilePath, Span selected, CancellationToken cancellationToken) where TLanguageConversion : ILanguageConversion, new()
        {
            try {
                await EnsureBuiltAsync();
                var conversionResult = await _joinableTaskFactory.RunAsync(async () => {
                    var result = await ConvertDocumentUnhandledAsync<TLanguageConversion>(documentFilePath, selected, cancellationToken);
                    await WriteConvertedFilesAndShowSummaryAsync(new[] { result }.ToAsyncEnumerable());
                    return result;
                });

                if ((await GetOptions()).CopyResultToClipboardForSingleDocument) {
                    await SetClipboardTextOnUiThreadAsync(conversionResult.ConvertedCode ?? conversionResult.GetExceptionsAsString());
                    await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + "Conversion result copied to clipboard.");
                    await VisualStudioInteraction.ShowMessageBoxAsync(_serviceProvider, "Conversion result copied to clipboard.", $"Conversion result copied to clipboard. {conversionResult.GetExceptionsAsString()}", false);
                }
            } catch (OperationCanceledException) {
                if (!_packageCancellation.CancelAll.IsCancellationRequested) {
                    await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + "Previous conversion cancelled", forceShow: true);
                }
            }
        }

        /// <remarks>
        /// https://github.com/icsharpcode/CodeConverter/issues/592
        /// https://github.com/dotnet/roslyn/issues/6615
        /// </remarks>
        private async Task EnsureBuiltAsync()
        {
            await VisualStudioInteraction.EnsureBuiltAsync(m => _outputWindow.WriteToOutputWindowAsync(m));
        }

        private static async Task SetClipboardTextOnUiThreadAsync(string conversionResultConvertedCode)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Clipboard.SetText(conversionResultConvertedCode);
            await TaskScheduler.Default;
        }

        private async Task WriteConvertedFilesAndShowSummaryAsync(IAsyncEnumerable<ConversionResult> convertedFiles)
        {
            await _outputWindow.WriteToOutputWindowAsync(Intro, forceShow: true);

            var files = new List<string>();
            var filesToOverwrite = new List<ConversionResult>();
            var errors = new List<string>();
            string longestFilePath = null;
            var longestFileLength = -1;
            await foreach (var convertedFile in convertedFiles) {
                if (convertedFile.SourcePathOrNull == null) continue;

                if (WillOverwriteSource(convertedFile)) {
                    filesToOverwrite.Add(convertedFile);
                    continue;
                }

                var exceptionsAsString = convertedFile.GetExceptionsAsString();
                if (!string.IsNullOrWhiteSpace(exceptionsAsString)) {
                    errors.Add(exceptionsAsString);
                }

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
                    await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + $"* {targetPathRelativeToSolutionDir}");
                }
                files = files.Concat(filesToOverwrite.Select(f => f.SourcePathOrNull)).ToList();
            } else if (longestFilePath != null) {
                await (await VisualStudioInteraction.OpenFileAsync(new FileInfo(longestFilePath))).SelectAllAsync();
            }

            var conversionSummary = await GetConversionSummaryAsync(files, errors);
            await _outputWindow.WriteToOutputWindowAsync(conversionSummary, false, true);

        }

        private async Task<bool> UserHasConfirmedOverwriteAsync(List<string> files, List<string> errors, IReadOnlyCollection<string> pathsToOverwrite)
        {
            var maxExamples = 30; // Avoid a huge unreadable dialog going off the screen
            var exampleText = pathsToOverwrite.Count > maxExamples ? $". First {maxExamples} examples" : "";
            await _outputWindow.WriteToOutputWindowAsync(Environment.NewLine + "Awaiting user confirmation for overwrite....", forceShow: true);
            bool shouldOverwrite = await VisualStudioInteraction.ShowMessageBoxAsync(_serviceProvider,
                "Overwrite solution and referencing projects?",
                $@"The current solution file and any referencing projects will be overwritten to reference the new project(s){exampleText}:
* {string.Join(Environment.NewLine + "* ", pathsToOverwrite.Take(maxExamples))}

The old contents will be copied to 'currentFilename.bak'.
Please 'Reload All' when Visual Studio prompts you.", true, files.Count > errors.Count);
            await _outputWindow.WriteToOutputWindowAsync(shouldOverwrite ? "confirmed" : "declined");
            return shouldOverwrite;
        }

        private static bool WillOverwriteSource(ConversionResult convertedFile)
        {
            return string.Equals(convertedFile.SourcePathOrNull, convertedFile.TargetPathOrNull, StringComparison.OrdinalIgnoreCase);
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

        private async Task<ConversionResult> ConvertDocumentUnhandledAsync<TLanguageConversion>(string documentPath, Span selected, CancellationToken cancellationToken) where TLanguageConversion : ILanguageConversion, new()
        {
            await _outputWindow.WriteToOutputWindowAsync($"Converting {documentPath}...", true, true);

            //TODO Figure out when there are multiple document ids for a single file path
            var documentId = _visualStudioWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(documentPath).SingleOrDefault();
            if (documentId == null) {
                //If document doesn't belong to any project
                await _outputWindow.WriteToOutputWindowAsync("File is not part of a compiling project, using best effort text conversion (less accurate).");
                return await ConvertFileTextAsync<TLanguageConversion>(documentPath, selected, cancellationToken);
            }
            var document = _visualStudioWorkspace.CurrentSolution.GetDocument(documentId);
            var selectedTextSpan = new TextSpan(selected.Start, selected.Length);
            return await ProjectConversion.ConvertSingleAsync<TLanguageConversion>(document, new SingleConversionOptions {SelectedTextSpan = selectedTextSpan}, CreateOutputWindowProgress(), cancellationToken);
        }

        private async Task<ConversionResult> ConvertFileTextAsync<TLanguageConversion>(string documentPath, Span selected, CancellationToken cancellationToken)
            where TLanguageConversion : ILanguageConversion, new()
        {
            var documentText = File.ReadAllText(documentPath);
            if (selected.Length > 0 && documentText.Length >= selected.End)
            {
                documentText = documentText.Substring(selected.Start, selected.Length);
            }

            var convertTextOnly = await ProjectConversion.ConvertTextAsync<TLanguageConversion>(documentText, new TextConversionOptions(DefaultReferences.NetStandard2, documentPath), CreateOutputWindowProgress(), cancellationToken);
            convertTextOnly.SourcePathOrNull = documentPath;
            return convertTextOnly;
        }

        private async IAsyncEnumerable<ConversionResult> ConvertProjectUnhandled<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects, [EnumeratorCancellation] CancellationToken cancellationToken)
            where TLanguageConversion : ILanguageConversion, new()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (selectedProjects.Count > 1) {
                await _outputWindow.WriteToOutputWindowAsync($"Converting {selectedProjects.Count} projects...", true, true);
            }

            var projectsByPath =
                _visualStudioWorkspace.CurrentSolution.Projects.ToLookup(p => p.FilePath, p => p);
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread - ToList ensures this happens within the same thread just switched to above
            var projects = selectedProjects.Select(p => projectsByPath[p.FullName].First()).ToList();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            await TaskScheduler.Default;

            var solutionConverter = SolutionConverter.CreateFor<TLanguageConversion>(projects, progress: CreateOutputWindowProgress(), cancellationToken: cancellationToken);

            var results = solutionConverter.Convert();
            await foreach(var result in results) yield return result;
        }

        private Progress<ConversionProgress> CreateOutputWindowProgress()
        {
            return new Progress<ConversionProgress>(s => {
                _outputWindow.WriteToOutputWindowAsync(s.ToString()).ForgetNoThrow();
            });
        }

        public static bool IsCSFileName(string fileName)
        {
            return fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        /// <remarks>https://github.com/dotnet/roslyn/blob/91571a3bb038e05e7bf2ab87510273a1017faed0/src/VisualStudio/VisualBasic/Impl/LanguageService/VisualBasicPackage.vb#L45-L52</remarks>
        public static bool IsVBFileName(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLower()) {
                case ".vb":
                case ".bas":
                case ".cls":
                case ".ctl":
                case ".dob":
                case ".dsr":
                case ".frm":
                case ".pag":
                    return true;
            }
            return false;
        }
    }
}
