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

        public CodeConversion(IServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace)
        {
            _serviceProvider = serviceProvider;
            _visualStudioWorkspace = visualStudioWorkspace;
        }
        
        public void PerformProjectConversion<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects) where TLanguageConversion : ILanguageConversion, new()
        {
            var convertedFiles = ConvertProjectUnhandled<TLanguageConversion>(selectedProjects);
            WriteConvertedFilesAndShowSummary(convertedFiles);
        }

        public async Task PerformDocumentConversion<TLanguageConversion>(string documentFilePath, Span selected) where TLanguageConversion : ILanguageConversion, new()
        {
            var result = await ConvertDocumentUnhandled<TLanguageConversion>(documentFilePath, selected);
            WriteConvertedFilesAndShowSummary(new[] { result });
        }

        private void WriteConvertedFilesAndShowSummary(IEnumerable<ConversionResult> convertedFiles)
        {
            var files = new List<string>();
            var errors = new List<string>();
            var longestFileLength = -1;

            foreach (var convertedFile in convertedFiles)
            {
                var sourcePath = convertedFile.SourcePathOrNull;
                if (convertedFile.Success && sourcePath != null)
                {
                    var path = ToggleExtension(sourcePath);
                    if (convertedFile.ConvertedCode.Length > longestFileLength)
                    {
                        longestFileLength = convertedFile.ConvertedCode.Length;
                        files.Insert(0, path);
                    } else {
                        files.Add(path);
                    }

                    File.WriteAllText(path, convertedFile.ConvertedCode);
                }
                else
                {
                    errors.Add(convertedFile.GetExceptionsAsString());
                }
            }
            ShowFirstResultAndConversionSummary(files, errors);
        }

        private void ShowFirstResultAndConversionSummary(List<string> files, List<string> errors)
        {
            if (files.Any()) {
                VisualStudioInteraction.OpenFile(new FileInfo(files.First()));
                files[0] = files[0] + " (opened in adjacent code window)";
            }

            VisualStudioInteraction.NewTextWindow("Conversion result summary", GetConversionSummary(files, errors));
        }

        private string GetConversionSummary(IReadOnlyCollection<string> files, IReadOnlyCollection<string> errors)
        {
            var solutionDir = Path.GetDirectoryName(_visualStudioWorkspace.CurrentSolution.FilePath);
            var relativeFilePaths = files.Select(fn => fn.Replace(solutionDir, "").Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).OrderBy(s => s);


            var introSummary = "Code conversion failed";
            var summaryOfSuccesses = "";
            if (files.Any()) {
                introSummary = "Code conversion completed";
                summaryOfSuccesses = "The following files have been written to disk (but are not part of the solution):"
                    + Environment.NewLine + "* " + string.Join(Environment.NewLine + "* ", relativeFilePaths);
            }

            if (errors.Any()) {
                introSummary += " with errors";
                WriteStatusBarText(introSummary);
                return introSummary
                       + Environment.NewLine + summaryOfSuccesses
                       + Environment.NewLine 
                       + Environment.NewLine
                       + string.Join(Environment.NewLine, errors);
            } else {
                WriteStatusBarText(introSummary);
                return introSummary
                       + Environment.NewLine
                       + summaryOfSuccesses;
            }
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentExtension), currentExtension, null);
            }
        }

        async Task<ConversionResult> ConvertDocumentUnhandled<TLanguageConversion>(string documentPath, Span selected) where TLanguageConversion : ILanguageConversion, new()
        {   
            //TODO Figure out when there are multiple document ids for a single file path
            var documentId = _visualStudioWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(documentPath).Single();
            var document = _visualStudioWorkspace.CurrentSolution.GetDocument(documentId);
            var compilation = await document.Project.GetCompilationAsync();
            var documentSyntaxTree = await document.GetSyntaxTreeAsync();

            var selectedTextSpan = new TextSpan(selected.Start, selected.Length);
            return await ProjectConversion<TLanguageConversion>.ConvertSingle(compilation, documentSyntaxTree, selectedTextSpan);
        }

        private IEnumerable<ConversionResult> ConvertProjectUnhandled<TLanguageConversion>(IReadOnlyCollection<Project> selectedProjects)
            where TLanguageConversion : ILanguageConversion, new()
        {
            var projectsByPath =
                _visualStudioWorkspace.CurrentSolution.Projects.ToDictionary(p => p.FilePath, p => p);
            var projects = selectedProjects.Select(p => projectsByPath[p.FullName]).ToList();
            var convertedFiles = ProjectConversion<TLanguageConversion>.ConvertProjects(projects);
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
