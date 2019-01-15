using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
using Constants = EnvDTE.Constants;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    internal static class VisualStudioInteraction
    {
        public static async Task<VsDocument> GetSingleSelectedItemOrDefaultAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
            ThreadHelper.ThrowIfNotOnUIThread();
            var selectedObjects = (IEnumerable<object>) Dte.ToolWindows.SolutionExplorer.SelectedItems;
            var selectedItems = selectedObjects.Cast<UIHierarchyItem>().ToList();

            return ObjectOfType<T>(selectedItems);
        }

        private static IEnumerable<T> ObjectOfType<T>(IReadOnlyCollection<UIHierarchyItem> selectedItems) where T : class
        {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var returnType = typeof(T);
            return selectedItems.Select(item => item.Object).Where(returnType.IsInstanceOfType).Cast<T>();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        public static async Task<Window> OpenFileAsync(FileInfo fileInfo)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return Dte.ItemOperations.OpenFile(fileInfo.FullName, Constants.vsViewKindTextView);
        }

        public static async Task SelectAllAsync(this Window window)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ((TextSelection)window.Document.Selection).SelectAll();
        }

        private static DTE2 Dte => Package.GetGlobalService(typeof(DTE)) as DTE2;

        public static async Task<IReadOnlyCollection<Project>> GetSelectedProjectsAsync(string projectExtension)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var projects = GetSelectedSolutionExplorerItems<Solution>().SelectMany(s => s.GetAllProjects())
                .Concat(GetSelectedSolutionExplorerItems<Project>().SelectMany(p => p.GetProjects()))
                .Concat(GetSelectedSolutionExplorerItems<ProjectItem>().Where(p => p.SubProject != null).SelectMany(p => p.SubProject.GetProjects()))
                .Where(project => project.FullName.EndsWith(projectExtension, StringComparison.InvariantCultureIgnoreCase))
                .Distinct().ToList();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            return projects;
        }

        public static async Task<IWpfTextViewHost> GetCurrentViewHostAsync(IAsyncServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var txtMgr = await serviceProvider.GetServiceAsync<SVsTextManager, IVsTextManager>();
            int mustHaveFocus = 1;
            if (txtMgr == null) {
                return null;
            }

            txtMgr.GetActiveView(mustHaveFocus, null, out IVsTextView vTextView);
            if (!(vTextView is IVsUserData userData)) {
                return null;
            }

            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out var holder);

            return holder as IWpfTextViewHost;
        }

        public static async Task<ITextDocument> GetTextDocumentAsync(this IWpfTextViewHost viewHost)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            viewHost.TextView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument);
            return textDocument;
        }

        public static async Task<VisualStudioWorkspace> GetWorkspaceAsync(IAsyncServiceProvider serviceProvider)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            return await serviceProvider.GetServiceAsync<VisualStudioWorkspace>(); 
        }

        public static async Task ShowExceptionAsync(IAsyncServiceProvider serviceProvider, string title, Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            MessageBox.Show($"An error has occured during conversion: {ex}",
                title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <returns>true iff the user answers "OK"</returns>
        public static async Task<bool> ShowMessageBoxAsync(IAsyncServiceProvider serviceProvider, string title, string msg, bool showCancelButton, bool defaultOk = true)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var userAnswer = MessageBox.Show(msg, title,
                showCancelButton ? MessageBoxButton.OKCancel : MessageBoxButton.OK,
                MessageBoxImage.Information,
                defaultOk || !showCancelButton ? MessageBoxResult.OK : MessageBoxResult.Cancel);
            return userAnswer == MessageBoxResult.OK;
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

            public static async Task<OutputWindow> CreateAsync()
            {
                return new OutputWindow(await CreateOutputPaneAsync());
            }

            public OutputWindow(IVsOutputWindowPane outputPaneAsync)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _outputPane = outputPaneAsync;

                _solutionEvents = Dte.Events.SolutionEvents;
                _solutionEvents.Opened += OnSolutionOpened;
            }

#pragma warning disable VSTHRD100 // Avoid async void methods - fire and forget event handler
            private async void OnSolutionOpened()
#pragma warning restore VSTHRD100 // Avoid async void methods
            {
                await WriteToOutputWindowAsync(_cachedOutput.ToString());
            }

            private static async Task<IVsOutputWindow> GetOutputWindowAsync()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                IServiceProvider serviceProvider = new ServiceProvider(Dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                return serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            }

            private static async Task<IVsOutputWindowPane> CreateOutputPaneAsync()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Guid generalPaneGuid = PaneGuid;
                var outputWindow = await GetOutputWindowAsync();
                outputWindow.GetPane(ref generalPaneGuid, out var pane);

                if (pane == null) {
                    outputWindow.CreatePane(ref generalPaneGuid, PaneName, 1, 1);
                    outputWindow.GetPane(ref generalPaneGuid, out pane);
                }

                return pane;
            }

            public async Task ClearAsync()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _cachedOutput.Clear();
                _outputPane.Clear();
            }

            public async Task ForceShowOutputPaneAsync()
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Dte.Windows.Item(Constants.vsWindowKindOutput).Visible = true;
                _outputPane.Activate();
            }

            public async Task WriteToOutputWindowAsync(string message)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                lock (_outputPane) {
                    _cachedOutput.AppendLine(message);
                    _outputPane.OutputStringThreadSafe(message);
                }
            }
        }

        public static async Task WriteStatusBarTextAsync(IAsyncServiceProvider serviceProvider, string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var statusBar = await serviceProvider.GetServiceAsync<SVsStatusbar, IVsStatusbar>();
            if (statusBar == null)
                return;

            statusBar.IsFrozen(out int frozen);
            if (frozen != 0) {
                statusBar.FreezeOutput(0);
            }

            statusBar.SetText(text);
            statusBar.FreezeOutput(1);
        }
    }
}
