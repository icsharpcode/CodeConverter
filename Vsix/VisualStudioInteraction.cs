using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CodeConverter.VsExtension
{
    static class VisualStudioInteraction
    {
        public static VsDocument GetSingleSelectedItemOrDefault()
        {
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            if ((monitorSelection == null) || (solution == null))
                return null;

            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try {
                var hresult = monitorSelection.GetCurrentSelection(out hierarchyPtr, out uint itemID, out var multiItemSelect, out selectionContainerPtr);
                if (ErrorHandler.Failed(hresult) || (hierarchyPtr == IntPtr.Zero) || (itemID == VSConstants.VSITEMID_NIL))
                    return null;

                if (multiItemSelect != null)
                    return null;

                if (itemID == VSConstants.VSITEMID_ROOT)
                    return null;

                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null)
                    return null;

                Guid guidProjectID = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
                    return null;

                return new VsDocument((IVsProject) hierarchy, guidProjectID, itemID);
            } finally {
                if (selectionContainerPtr != IntPtr.Zero) {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero) {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

        public static IWpfTextViewHost GetCurrentViewHost(IServiceProvider serviceProvider)
        {
            IVsTextManager txtMgr = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));
            IVsTextView vTextView = null;
            int mustHaveFocus = 1;
            txtMgr.GetActiveView(mustHaveFocus, null, out vTextView);
            IVsUserData userData = vTextView as IVsUserData;
            if (userData == null)
                return null;

            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out holder);

            return holder as IWpfTextViewHost;
        }

        public static ITextDocument GetTextDocument(this IWpfTextViewHost viewHost)
        {
            ITextDocument textDocument = null;
            viewHost.TextView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out textDocument);
            return textDocument;
        }

        public static VisualStudioWorkspace GetWorkspace(IServiceProvider serviceProvider)
        {
            return (VisualStudioWorkspace) serviceProvider.GetService(typeof(VisualStudioWorkspace)); 
        }

        public static void ShowException(IServiceProvider serviceProvider, string title, Exception ex)
        {
            VsShellUtilities.ShowMessageBox(
                serviceProvider,
                $"An error has occured during conversion: {ex}",
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
