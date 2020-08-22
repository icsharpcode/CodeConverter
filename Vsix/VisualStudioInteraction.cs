using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Constants = EnvDTE.Constants;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;
using Window = EnvDTE.Window;

namespace ICSharpCode.CodeConverter.VsExtension
{
    /// <remarks>
    /// All public methods switch to the main thread, do their work, then switch back to the thread pool
    /// Private methods may also do so for convenience to suppress the analyzer warning
    /// </remarks>
    internal static class VisualStudioInteraction
    {
        private static DTE2 m_Dte;

        /// <remarks>All calls and usages must be from the main thread</remarks>>
        internal static DTE2 Dte => m_Dte ??= Package.GetGlobalService(typeof(DTE)) as DTE2;

        private static CancellationToken CancelAllToken;
        private static readonly Version m_LowestSupportedVersion = new Version(15, 7, 0, 0);
        private static readonly Version m_FullVsVersion = GetFullVsVersion();
        private static readonly string m_Title = "Code converter " + new AssemblyName(typeof(ProjectConversion).Assembly.FullName).Version.ToString(3) + " - Visual Studio " + m_FullVsVersion;

        private static Version GetFullVsVersion()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");

            if (File.Exists(path)) {
                var fvi = FileVersionInfo.GetVersionInfo(path);
                return new Version(fvi.ProductMajorPart, fvi.ProductMinorPart, fvi.ProductBuildPart,
                    fvi.ProductPrivatePart);
            } else {
                return null;
            }
        }


        internal static void Initialize(Cancellation packageCancellation)
        {
            CancelAllToken = packageCancellation.CancelAll;
        }

        public static async Task<string> GetSingleSelectedItemPathOrDefaultAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var selectedItem = await GetSingleSelectedItemOrDefaultAsync();
            var itemPath = selectedItem?.ItemPath;
            await TaskScheduler.Default;
            return itemPath;
        }

        public static async Task<Window> OpenFileAsync(FileInfo fileInfo)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var window = Dte.ItemOperations.OpenFile(fileInfo.FullName, Constants.vsViewKindTextView);
            await TaskScheduler.Default;
            return window;
        }

        public static async Task SelectAllAsync(this Window window)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            ((TextSelection)window.Document.Selection).SelectAll();
            await TaskScheduler.Default;
        }
        public static async Task<IReadOnlyCollection<Project>> GetSelectedProjectsAsync(string projectExtension)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var projects = GetSelectedSolutionExplorerItems<Solution>().SelectMany(s => s.GetAllProjects())
                .Concat(GetSelectedSolutionExplorerItems<Project>().SelectMany(p => p.GetProjects()))
                .Concat(GetSelectedSolutionExplorerItems<ProjectItem>().Where(p => p.SubProject != null).SelectMany(p => p.SubProject.GetProjects()))
                .Where(project => project.FullName.EndsWith(projectExtension, StringComparison.InvariantCultureIgnoreCase))
                .Distinct().ToList();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            await TaskScheduler.Default;
            return projects;
        }

        public static async Task<ITextDocument> GetTextDocumentAsync(this IWpfTextViewHost viewHost)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            viewHost.TextView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument);
            await TaskScheduler.Default;
            return textDocument;
        }

        public static async Task ShowExceptionAsync(Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            if (!CancelAllToken.IsCancellationRequested) {
                var versionMessageSuffix = "";
                if (m_FullVsVersion < m_LowestSupportedVersion) {
                    versionMessageSuffix = $"{Environment.NewLine}This extension only supports VS {m_LowestSupportedVersion}+, you are currently using {m_FullVsVersion}";
                }
                if (m_FullVsVersion.Major < 16) {
                    versionMessageSuffix = $"{Environment.NewLine}Support for VS2017 (15.*) is likely to end this year. You're using: {m_FullVsVersion}";
                }
                MessageBox.Show($"An error has occured during conversion - press Ctrl+C to copy the details: {ex}{versionMessageSuffix}",
                    m_Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <returns>true iff the user answers "OK"</returns>
        public static async Task<bool> ShowMessageBoxAsync(IAsyncServiceProvider serviceProvider, string title, string msg, bool showCancelButton, bool defaultOk = true)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            if (CancelAllToken.IsCancellationRequested) return false;
            var userAnswer = MessageBox.Show(msg, title,
                showCancelButton ? MessageBoxButton.OKCancel : MessageBoxButton.OK,
                MessageBoxImage.Information,
                defaultOk || !showCancelButton ? MessageBoxResult.OK : MessageBoxResult.Cancel);
            return userAnswer == MessageBoxResult.OK;
        }

        public static async Task EnsureBuiltAsync(Func<string, Task> writeMessageAsync)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var build = Dte.Solution.SolutionBuild;
            if (build.BuildState == vsBuildState.vsBuildStateInProgress) {
                throw new InvalidOperationException("Build in progress, please wait for it to complete before conversion.");
            }
            await writeMessageAsync("Building solution prior to conversion for maximum accuracy...");
            build.Build(true);
            await TaskScheduler.Default;
        }

        public static async Task WriteStatusBarTextAsync(IAsyncServiceProvider serviceProvider, string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var statusBar = await serviceProvider.GetServiceAsync<SVsStatusbar, IVsStatusbar>();
            if (statusBar == null)
                return;

            statusBar.IsFrozen(out int frozen);
            if (frozen != 0) {
                statusBar.FreezeOutput(0);
            }

            statusBar.SetText(text);
            statusBar.FreezeOutput(1);
            await TaskScheduler.Default;
        }

        public static async Task<Span?> GetFirstSelectedSpanInCurrentViewAsync(IAsyncServiceProvider serviceProvider,
            Func<string, bool> predicate, bool mustHaveFocus)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var span = await FirstSelectedSpanInCurrentViewPrivateAsync(serviceProvider, predicate, mustHaveFocus);
            await TaskScheduler.Default;
            return span;
        }

        public static async Task<(string FilePath, Span? Selection)> GetCurrentFilenameAndSelectionAsync(
            IAsyncServiceProvider asyncServiceProvider, Func<string, bool> predicate, bool mustHaveFocus)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);

            var span = await GetFirstSelectedSpanInCurrentViewAsync(asyncServiceProvider, predicate, mustHaveFocus);
            var currentViewHostAsync =
                await GetCurrentViewHostAsync(asyncServiceProvider, predicate, mustHaveFocus);
            if (currentViewHostAsync == null) return (null, null);
            using (var textDocumentAsync = await currentViewHostAsync.GetTextDocumentAsync())
            {
                var result = (textDocumentAsync?.FilePath, span);
                await TaskScheduler.Default;
                return result;
            }

        }

        private static async Task<VsDocument> GetSingleSelectedItemOrDefaultAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            if ((monitorSelection == null) || (solution == null))
                return null;

            var hResult = monitorSelection.GetCurrentSelection(out var hierarchyPtr, out uint itemId, out var multiItemSelect, out var selectionContainerPtr);
            try {
                if (ErrorHandler.Failed(hResult) || hierarchyPtr == IntPtr.Zero || itemId == VSConstants.VSITEMID_NIL ||
                    multiItemSelect != null || itemId == VSConstants.VSITEMID_ROOT ||
                    !(Marshal.GetObjectForIUnknown(hierarchyPtr) is IVsHierarchy hierarchy)) {
                    return null;
                }

                int result = solution.GetGuidOfProject(hierarchy, out Guid guidProjectId);
                // ReSharper disable once SuspiciousTypeConversion.Global - COM Object
                return ErrorHandler.Succeeded(result) ? new VsDocument((IVsProject) hierarchy, guidProjectId, itemId) : null;

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

        private static async Task<IWpfTextViewHost> GetCurrentViewHostAsync(IAsyncServiceProvider serviceProvider, bool mustHaveFocus)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancelAllToken);
            var txtMgr = await serviceProvider.GetServiceAsync<SVsTextManager, IVsTextManager>();
            if (txtMgr == null) {
                return null;
            }

            txtMgr.GetActiveView(mustHaveFocus ? 1 : 0, null, out IVsTextView vTextView);
            // ReSharper disable once SuspiciousTypeConversion.Global - COM Object
            if (!(vTextView is IVsUserData userData)) {
                return null;
            }

            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out var holder);

            return holder as IWpfTextViewHost;
        }

        private static async Task<Span?> FirstSelectedSpanInCurrentViewPrivateAsync(
            IAsyncServiceProvider serviceProvider,
            Func<string, bool> predicate, bool mustHaveFocus)
        {
            var selection = await GetSelectionInCurrentViewAsync(serviceProvider, predicate, mustHaveFocus);
            return selection?.SelectedSpans.First().Span;
        }

        private static async Task<ITextSelection> GetSelectionInCurrentViewAsync(IAsyncServiceProvider serviceProvider,
            Func<string, bool> predicate, bool mustHaveFocus)
        {
            var viewHost = await GetCurrentViewHostAsync(serviceProvider, predicate, mustHaveFocus);
            return viewHost?.TextView.Selection;
        }

        private static async Task<IWpfTextViewHost> GetCurrentViewHostAsync(IAsyncServiceProvider serviceProvider,
            Func<string, bool> predicate, bool mustHaveFocus)
        {
            var viewHost = await GetCurrentViewHostAsync(serviceProvider, mustHaveFocus);
            if (viewHost == null)
                return null;

            var textDocument = await viewHost.GetTextDocumentAsync();
            return textDocument != null && predicate(textDocument.FilePath) ? viewHost : null;
        }
    }
}
