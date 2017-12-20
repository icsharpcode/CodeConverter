using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Runtime.InteropServices;

namespace RefactoringEssentials.VsExtension
{
	static class VisualStudioInteraction
	{
		public static bool GetSingleSelectedItem(out IVsHierarchy hierarchy, out uint itemID)
		{
			hierarchy = null;
			itemID = VSConstants.VSITEMID_NIL;
			int hresult = VSConstants.S_OK;

			var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
			var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
			if ((monitorSelection == null) || (solution == null))
				return false;

			IVsMultiItemSelect multiItemSelect = null;
			IntPtr hierarchyPtr = IntPtr.Zero;
			IntPtr selectionContainerPtr = IntPtr.Zero;

			try {
				hresult = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemID, out multiItemSelect, out selectionContainerPtr);
				if (ErrorHandler.Failed(hresult) || (hierarchyPtr == IntPtr.Zero) || (itemID == VSConstants.VSITEMID_NIL))
					return false;

				if (multiItemSelect != null)
					return false;

				if (itemID == VSConstants.VSITEMID_ROOT)
					return false;

				hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
				if (hierarchy == null)
					return false;

				Guid guidProjectID = Guid.Empty;

				if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
					return false;

				return true;
			} finally {
				if (selectionContainerPtr != IntPtr.Zero) {
					Marshal.Release(selectionContainerPtr);
				}

				if (hierarchyPtr != IntPtr.Zero) {
					Marshal.Release(hierarchyPtr);
				}
			}
		}

		public static string GetSingleSelectedItemPath()
		{
			IVsHierarchy hierarchy = null;
			uint itemID = VSConstants.VSITEMID_NIL;
			if (!GetSingleSelectedItem(out hierarchy, out itemID))
				return null;

			string itemPath = null;
			((IVsProject)hierarchy).GetMkDocument(itemID, out itemPath);
			return itemPath;
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
