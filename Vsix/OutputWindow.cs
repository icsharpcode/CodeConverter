using System;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Constants = EnvDTE.Constants;
using Task = System.Threading.Tasks.Task;

namespace ICSharpCode.CodeConverter.VsExtension
{
    /// <remarks>
    /// All public methods switch to the main thread, do their work, then switch back to the thread pool
    /// Private methods may also do so for convenience to suppress the analyzer warning
    /// </remarks>
    internal class OutputWindow
    {
        private const string PaneName = "Code Converter";
        private static readonly Guid PaneGuid = new Guid("44F575C6-36B5-4CDB-AAAE-E096E6A446BF");
        private readonly IVsOutputWindowPane _outputPane;
        private bool _hasOutputSinceSolutionOpened = false;

        // Reference to avoid GC https://docs.microsoft.com/en-us/dotnet/api/envdte.solutionevents?view=visualstudiosdk-2017#remarks
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly SolutionEvents _solutionEvents;

        public static async Task<OutputWindow> CreateAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Guid generalPaneGuid = PaneGuid;
            var outputWindow = await GetOutputWindowAsync();
            outputWindow.GetPane(ref generalPaneGuid, out var pane);

            if (pane == null) {
                outputWindow.CreatePane(ref generalPaneGuid, PaneName, 1, 0);
                outputWindow.GetPane(ref generalPaneGuid, out pane);
            }

            var window = new OutputWindow(pane);

            await TaskScheduler.Default;
            return window;
        }

        public OutputWindow(IVsOutputWindowPane outputPaneAsync)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _outputPane = outputPaneAsync;

            _solutionEvents = VisualStudioInteraction.Dte.Events.SolutionEvents;
            _solutionEvents.Opened += () => OnSolutionOpenedAsync().Forget();
        }

        public async Task ClearAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _hasOutputSinceSolutionOpened = false;
            _outputPane.Clear();
            await TaskScheduler.Default;
        }

        public async Task ForceShowOutputPaneAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ForceShowInner();
            await TaskScheduler.Default;
        }

        public async Task WriteToOutputWindowAsync(string message, bool clearFirst = false, bool forceShow = false)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _hasOutputSinceSolutionOpened = true;
            if (clearFirst) _outputPane.Clear();
            _outputPane.OutputStringThreadSafe(message);
            if (forceShow) ForceShowInner();
            await TaskScheduler.Default;
        }

        private void ForceShowInner()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VisualStudioInteraction.Dte.Windows.Item(Constants.vsWindowKindOutput).Visible = true;
            _outputPane.Activate();
        }

        private async Task OnSolutionOpenedAsync()
        {
            if (_hasOutputSinceSolutionOpened) await ForceShowOutputPaneAsync();
            _hasOutputSinceSolutionOpened = false;
        }

        private static async Task<IVsOutputWindow> GetOutputWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IServiceProvider serviceProvider = new ServiceProvider(VisualStudioInteraction.Dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            return serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }
    }
}
