using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Project = EnvDTE.Project;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    class CodeConversion
    {
        private readonly IServiceProvider serviceProvider;
        private readonly VisualStudioWorkspace visualStudioWorkspace;
        public static readonly string CSToVBConversionTitle = "Convert C# to VB:";
        public static readonly string VBToCSConversionTitle = "Convert VB to C#:";

        public CodeConversion(IServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace)
        {
            this.serviceProvider = serviceProvider;
            this.visualStudioWorkspace = visualStudioWorkspace;
        }

        public void PerformCSToVBConversion(string inputCode)
        {
            string convertedText = null;
            try {
                var result = TryConvertingCSToVBCode(inputCode);
                if (!result.Success) {
                    var newLines = Environment.NewLine + Environment.NewLine;
                    VsShellUtilities.ShowMessageBox(
                        serviceProvider,
                        $"Selected C# code seems to have errors or to be incomplete:{newLines}{result.GetExceptionsAsString()}",
                        CSToVBConversionTitle,
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                convertedText = result.ConvertedCode;
            } catch (Exception ex) {
                VisualStudioInteraction.ShowException(serviceProvider, CSToVBConversionTitle, ex);
                return;
            }

            // Direct output for debugging
            //string message = convertedText;
            //VsShellUtilities.ShowMessageBox(
            //    serviceProvider,
            //    message,
            //    CSToVBConversionTitle,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            WriteStatusBarText("Copied converted VB code to clipboard.");

            Clipboard.SetText(convertedText);
        }

        ConversionResult TryConvertingCSToVBCode(string inputCode)
        {
            var codeWithOptions = new CodeWithOptions(inputCode)
                .SetFromLanguage("C#")
                .SetToLanguage("Visual Basic")
                .WithDefaultReferences();
            return ICSharpCode.CodeConverter.CodeConverter.Convert(codeWithOptions);
        }

        public void ConvertVBProjects(IReadOnlyCollection<Project> selectedProjects)
        {
            var projectsByPath = visualStudioWorkspace.CurrentSolution.Projects.ToDictionary(p => p.FilePath, p => p);
            var projects = selectedProjects.Select(p => projectsByPath[p.FullName]);
            var convertedFiles = ProjectConversion.ConvertProjects(projects);
            var errors = new List<string>();
            foreach (var convertedFile in convertedFiles) {
                if (convertedFile.Success && convertedFile.SourcePathOrNull != null) {
                    var path = Path.ChangeExtension(convertedFile.SourcePathOrNull, ".cs");
                    File.WriteAllText(path, convertedFile.ConvertedCode);
                } else {
                    errors.Add(convertedFile.GetExceptionsAsString());
                }
            }

            if (errors.Any()) {
                ShowNonFatalError(string.Join(Environment.NewLine, errors));
            }
        }

        public async Task PerformVBToCSConversion(string documentFilePath, Span selected)
        {
            string convertedText = null;
            try {
                var result = await TryConvertingVBToCSCode(documentFilePath, selected);
                if (!result.Success) {
                    ShowNonFatalError(result.GetExceptionsAsString());
                    return;
                }

                convertedText = result.ConvertedCode;
            } catch (Exception ex) {
                VisualStudioInteraction.ShowException(serviceProvider, VBToCSConversionTitle, ex);
                return;
            }

            // Direct output for debugging
            //string message = convertedText;
            //VsShellUtilities.ShowMessageBox(
            //    serviceProvider,
            //    message,
            //    VBToCSConversionTitle,
            //    OLEMSGICON.OLEMSGICON_INFO,
            //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            WriteStatusBarText("Copied converted C# code to clipboard.");

            Clipboard.SetText(convertedText);
        }

        private void ShowNonFatalError(string error)
        {
            var newLines = Environment.NewLine + Environment.NewLine;
            VsShellUtilities.ShowMessageBox(
                serviceProvider,
                $"Errors converting selected VB code (press Ctrl + C to copy whole error):{newLines}{error}",
                VBToCSConversionTitle,
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        async Task<ConversionResult> TryConvertingVBToCSCode(string documentPath, Span selected)
        {   
            //TODO Figure out when there are multiple document ids for a single file path
            var documentId = visualStudioWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(documentPath).Single();
            var document = visualStudioWorkspace.CurrentSolution.GetDocument(documentId);
            var compilation = await document.Project.GetCompilationAsync();
            var documentSyntaxTree = await document.GetSyntaxTreeAsync();

            var selectedTextSpan = new TextSpan(selected.Start, selected.Length);
            return await ProjectConversion.ConvertSingle((VisualBasicCompilation)compilation, (VisualBasicSyntaxTree)documentSyntaxTree, selectedTextSpan);
        }
        void WriteStatusBarText(string text)
        {
            IVsStatusbar statusBar = (IVsStatusbar)serviceProvider.GetService(typeof(SVsStatusbar));
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

        IWpfTextViewHost GetCurrentCSViewHost()
        {
            IWpfTextViewHost viewHost = VisualStudioInteraction.GetCurrentViewHost(serviceProvider);
            if (viewHost == null)
                return null;

            ITextDocument textDocument = viewHost.GetTextDocument();
            if ((textDocument == null) || !IsCSFileName(textDocument.FilePath))
                return null;

            return viewHost;
        }

        public static bool IsCSFileName(string fileName)
        {
            return fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        }

        public ITextSelection GetCSSelectionInCurrentView()
        {
            IWpfTextViewHost viewHost = GetCurrentCSViewHost();
            if (viewHost == null)
                return null;

            return viewHost.TextView.Selection;
        }

        public IWpfTextViewHost GetCurrentVBViewHost()
        {
            IWpfTextViewHost viewHost = VisualStudioInteraction.GetCurrentViewHost(serviceProvider);
            if (viewHost == null)
                return null;

            ITextDocument textDocument = viewHost.GetTextDocument();
            if ((textDocument == null) || !IsVBFileName(textDocument.FilePath))
                return null;

            return viewHost;
        }

        public static bool IsVBFileName(string fileName)
        {
            return fileName.EndsWith(".vb", StringComparison.OrdinalIgnoreCase);
        }

        public ITextSelection GetVBSelectionInCurrentView()
        {
            IWpfTextViewHost viewHost = GetCurrentVBViewHost();
            if (viewHost == null)
                return null;

            return viewHost.TextView.Selection;
        }
    }
}
