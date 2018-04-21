using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        public static readonly string ConverterTitle = "Code converter";
        private static readonly string Intro = Environment.NewLine + Environment.NewLine + new string(Enumerable.Repeat('-', 80).ToArray()) + Environment.NewLine + "Writing converted files to disk:";

        public CodeConversion(IServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace)
        {
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
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
            await Task.Run(async () => {
                var result = await ConvertDocumentUnhandled<TLanguageConversion>(documentFilePath, selected);
                WriteConvertedFilesAndShowSummary(new[] { result });
            });
        }

        private void WriteConvertedFilesAndShowSummary(IEnumerable<ConversionResult> convertedFiles)
        {
            var files = new List<string>();
            var errors = new List<string>();
            string longestFilePath = null;
            var longestFileLength = -1;

            var solutionDir = Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath);
            VisualStudioInteraction.OutputWindow.WriteToOutputWindow(Intro);
            VisualStudioInteraction.OutputWindow.ForceShowOutputPane();

            foreach (var convertedFile in convertedFiles) {

                var sourcePath = convertedFile.SourcePathOrNull ?? "";
                var sourcePathRelativeToSolutionDir = PathRelativeToSolutionDir(solutionDir, sourcePath);
                if (sourcePath != "") {
                    if (!string.IsNullOrWhiteSpace(convertedFile.ConvertedCode)) {
                        var path = ToggleExtension(sourcePath);
                        if (convertedFile.ConvertedCode.Length > longestFileLength) {
                            longestFileLength = convertedFile.ConvertedCode.Length;
                            longestFilePath = path;
                        }

                        files.Add(path);
                        File.WriteAllText(path, convertedFile.ConvertedCode);
                    }

                    LogProgress(convertedFile, errors, sourcePathRelativeToSolutionDir);
                }
            }
            
            VisualStudioInteraction.OutputWindow.WriteToOutputWindow(GetConversionSummary(files, errors));
            VisualStudioInteraction.OutputWindow.ForceShowOutputPane();

            if (longestFilePath != null) {
                VisualStudioInteraction.OpenFile(new FileInfo(longestFilePath)).SelectAll();
            }
        }

        private void LogProgress(ConversionResult convertedFile, List<string> errors, string sourcePathRelativeToSolutionDir)
        {
            var exceptionsAsString = convertedFile.GetExceptionsAsString();
            var indentedException = exceptionsAsString.Replace(Environment.NewLine, Environment.NewLine + "    ");
            string output = Environment.NewLine + $"* {ToggleExtension(sourcePathRelativeToSolutionDir)}";
            var containsErrors = !string.IsNullOrWhiteSpace(exceptionsAsString);

            if (containsErrors) {
                errors.Add(exceptionsAsString);
            }

            if (string.IsNullOrWhiteSpace(convertedFile.ConvertedCode))
            {
                output = Environment.NewLine +
                         $"* Failure processing {sourcePathRelativeToSolutionDir}{Environment.NewLine}    {indentedException}";    
            }
            else if (containsErrors){
                output += $" contains errors{Environment.NewLine}    {indentedException}";
            }

            VisualStudioInteraction.OutputWindow.WriteToOutputWindow(output);
        }

        private static string PathRelativeToSolutionDir(string solutionDir, string path)
        {
            return path.Replace(solutionDir, "")
                .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private string GetConversionSummary(IReadOnlyCollection<string> files, IReadOnlyCollection<string> errors)
        {
            var oneLine = "Code conversion failed";
            var successSummary = "";
            if (files.Any()) {
                oneLine = "Code conversion completed";
                successSummary = $"{files.Count} files have been written to disk.";
                if (files.Count > 1) {
                    successSummary += Environment.NewLine + "One file has been opened as an example, to see others in Visual Studio's solution explorer, you can use its 'Show All Files' button.";
                }
            }

            if (errors.Any()) {
                oneLine += $" with {errors.Count} error" + (errors.Count == 1 ? "" : "s");
            }

            WriteStatusBarText(oneLine + " - see output window");
            return Environment.NewLine + Environment.NewLine
                                       + oneLine
                                       + Environment.NewLine + successSummary
                                       + Environment.NewLine;
        }

        private static string ToggleExtension(string sourcePath)
        {
            var currentExtension = Path.GetExtension(sourcePath)?.ToLowerInvariant() ?? "";
            return Path.ChangeExtension(sourcePath, ConvertExtension(currentExtension));
        }

        private static string ConvertExtension(string currentExtension)
        {
            switch (currentExtension)
            {
                case ".vb":
                    return ".cs";
                case ".cs":
                    return ".vb";
                case ".txt":
                    return ".txt";
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentExtension), currentExtension, null);
            }
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
            return await ProjectConversion.ConvertSingle(compilation, documentSyntaxTree, selectedTextSpan, new TLanguageConversion());
        }

        private static ConversionResult ConvertTextOnly<TLanguageConversion>(string documentPath, Span selected)
            where TLanguageConversion : ILanguageConversion, new()
        {
            var documentText = File.ReadAllText(documentPath);
            if (selected.Length > 0 && documentText.Length >= selected.End)
            {
                documentText = documentText.Substring(selected.Start, selected.Length);
            }

            var convertTextOnly = ProjectConversion.ConvertText<TLanguageConversion>(documentText, CodeWithOptions.DefaultMetadataReferences);
            convertTextOnly.SourcePathOrNull = documentPath;
            return convertTextOnly;
        }

        private IEnumerable<ConversionResult> ConvertProjectUnhandled<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects)
            where TLanguageConversion : ILanguageConversion, new()
        {
            var projectsByPath =
                _visualStudioWorkspace.CurrentSolution.Projects.ToLookup(p => p.FilePath, p => p);
            var projects = selectedProjects.Select(p => projectsByPath[p.FullName].First()).ToList();
            var convertedFiles = SolutionConverter.CreateFor<TLanguageConversion>(projects).Convert();
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
