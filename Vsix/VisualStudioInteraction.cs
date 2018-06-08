using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using EnvDTE80;
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
    internal static class VisualStudioInteraction
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
                var hresult = monitorSelection.GetCurrentSelection(out hierarchyPtr, out uint itemId, out var multiItemSelect, out selectionContainerPtr);
                if (ErrorHandler.Failed(hresult) || (hierarchyPtr == IntPtr.Zero) || (itemId == VSConstants.VSITEMID_NIL))
                    return null;

                if (multiItemSelect != null)
                    return null;

                if (itemId == VSConstants.VSITEMID_ROOT)
                    return null;

                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null)
                    return null;

                Guid guidProjectId = Guid.Empty;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectId)))
                    return null;

                return new VsDocument((IVsProject) hierarchy, guidProjectId, itemId);
            } finally {
                if (selectionContainerPtr != IntPtr.Zero) {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero) {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
        private static IEnumerable<T> GetSelectedSolutionExplorerItems<T>() where T: class
        {
            var selectedObjects = (IEnumerable<object>) Dte.ToolWindows.SolutionExplorer.SelectedItems;
            var selectedItems = selectedObjects.Cast<UIHierarchyItem>().ToList();

            var returnType = typeof(T);
            return selectedItems.Select(item => item.Object).Where(returnType.IsInstanceOfType).Cast<T>();
        }

        public static Window OpenFile(FileInfo fileInfo)
        {
            return Dte.ItemOperations.OpenFile(fileInfo.FullName, EnvDTE.Constants.vsViewKindTextView);
        }

        public static void SelectAll(this Window window)
        {
            ((TextSelection)window.Document.Selection).SelectAll();
        }

        private static DTE2 Dte => Package.GetGlobalService(typeof(DTE)) as DTE2;

        public static IReadOnlyCollection<Project> GetSelectedProjects(string projectExtension)
        {
            var items = GetSelectedSolutionExplorerItems<Solution>()
                .SelectMany(s => s.Projects.Cast<Project>())
                .Concat(GetSelectedSolutionExplorerItems<Project>())
                .Concat(GetSelectedSolutionExplorerItems<ProjectItem>().Where(p => p.SubProject != null).Select(p => p.SubProject))
                .Where(project => project.FullName.EndsWith(projectExtension, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
            return items;
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

        /// <returns>true iff the user answers "OK"</returns>
        public static bool ShowMessageBox(IServiceProvider serviceProvider, string title, string msg, bool showCancelButton, bool defaultOk = true)
        {
            var answeredOk = 1;
            return VsShellUtilities.ShowMessageBox(
                serviceProvider,
                msg,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                showCancelButton ? OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL : OLEMSGBUTTON.OLEMSGBUTTON_OK,
                defaultOk || !showCancelButton ? OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST : OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND) == answeredOk;
        }

        public class OutputWindow
        {
            private const string PaneName = "Code Converter";
            private static readonly Guid PaneGuid = new Guid("44F575C6-36B5-4CDB-AAAE-E096E6A446BF");
            private readonly IVsOutputWindowPane _outputPane;
            private readonly StringBuilder _cachedOutput = new StringBuilder();

            // Reference to avoid GC https://docs.microsoft.com/en-us/dotnet/api/envdte.solutionevents?view=visualstudiosdk-2017#remarks
            // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
            private readonly SolutionEvents _solutionEvents;

            public OutputWindow()
            {
                _outputPane = CreateOutputPane();

                _solutionEvents = Dte.Events.SolutionEvents;
                _solutionEvents.Opened += () => { WriteToOutputWindow(_cachedOutput.ToString()); };
            }

            private static IVsOutputWindow GetOutputWindow()
            {
                IServiceProvider serviceProvider = new ServiceProvider(Dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                return serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            }
            private static IVsOutputWindowPane CreateOutputPane()
            {
                Guid generalPaneGuid = PaneGuid;
                var outputWindow = GetOutputWindow();
                outputWindow.GetPane(ref generalPaneGuid, out var pane);

                if (pane == null) {
                    outputWindow.CreatePane(ref generalPaneGuid, PaneName, 1, 1);
                    outputWindow.GetPane(ref generalPaneGuid, out pane);
                }

                return pane;
            }

            public void Clear()
            {
                _cachedOutput.Clear();
                _outputPane.Clear();
            }

            public void ForceShowOutputPane()
            {
                Dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput).Visible = true;
                _outputPane.Activate();
            }

            public void WriteToOutputWindow(string message)
            {
                _cachedOutput.AppendLine(message);
                _outputPane.OutputString(message);
            }
        }
    }
}
