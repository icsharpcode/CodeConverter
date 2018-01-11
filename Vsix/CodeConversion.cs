using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Windows;
using ICSharpCode.CodeConverter;
using Microsoft.VisualStudio.LanguageServices;

namespace RefactoringEssentials.VsExtension
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
            return CodeConverter.Convert(codeWithOptions);
        }

        public void PerformVBToCSConversion(string inputCode)
        {
            string convertedText = null;
            try {
                var result = TryConvertingVBToCSCode(inputCode); // Switch to calling VisualBasicConverter.ConvertSingle() directly with the relevant workspace document
                if (!result.Success) {
                    var newLines = Environment.NewLine + Environment.NewLine;
                    VsShellUtilities.ShowMessageBox(
                        serviceProvider,
                        $"Selected VB code seems to have errors or to be incomplete:{newLines}{result.GetExceptionsAsString()}",
                        VBToCSConversionTitle,
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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

        ConversionResult TryConvertingVBToCSCode(string inputCode)
        {
            var codeWithOptions = new CodeWithOptions(inputCode)
                .SetFromLanguage("Visual Basic", 14)
                .SetToLanguage("C#", 6)
                .WithDefaultReferences();
            return CodeConverter.Convert(codeWithOptions);
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

        IWpfTextViewHost GetCurrentVBViewHost()
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
