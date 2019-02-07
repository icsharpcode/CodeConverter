using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.VB;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using OleMenuCommand = Microsoft.VisualStudio.Shell.OleMenuCommand;
using OleMenuCommandService = Microsoft.VisualStudio.Shell.OleMenuCommandService;
using Task = System.Threading.Tasks.Task;

namespace CodeConverter.VsExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConvertCSToVBCommand
    {
        public const int MainMenuCommandId = 0x0100;
        public const int CtxMenuCommandId = 0x0101;
        public const int ProjectItemCtxMenuCommandId = 0x0102;
        public const int SolutionOrProjectCtxMenuCommandId = 0x0103;
        private const string ProjectExtension = ".csproj";

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a3378a21-e939-40c9-9e4b-eb0cec7b7854");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        readonly REConverterPackage _package;

        private CodeConversion _codeConversion;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ConvertCSToVBCommand Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        IAsyncServiceProvider ServiceProvider {
            get {
                return _package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(REConverterPackage package)
        {
            CodeConversion codeConversion = await CodeConversion.CreateAsync(package, package.VsWorkspace, package.GetOptionsAsync);
            OleMenuCommandService oleMenuCommandService = await package.GetServiceAsync<IMenuCommandService, OleMenuCommandService>();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Instance = new ConvertCSToVBCommand(package, codeConversion, oleMenuCommandService);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertCSToVBCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="codeConversion"></param>
        /// <param name="commandService"></param>
        /// <remarks>Must be called on the UI thread due to VS 2017's implementation of AddCommand which calls GetService</remarks>
        ConvertCSToVBCommand(REConverterPackage package, CodeConversion codeConversion, OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this._package = package ?? throw new ArgumentNullException(nameof(package));
            _codeConversion = codeConversion;
            
            if (commandService != null) {
                // Command in main menu
                var menuCommandId = new CommandID(CommandSet, MainMenuCommandId);
                var menuItem = new BlockingOleMenuCommand(CodeEditorMenuItemCallbackAsync, menuCommandId);
                menuItem.BeforeQueryStatus += CodeEditorMenuItem_BeforeQueryStatusAsync;
                commandService.AddCommand(menuItem);

                // Command in code editor's context menu
                var ctxMenuCommandId = new CommandID(CommandSet, CtxMenuCommandId);
                var ctxMenuItem = new BlockingOleMenuCommand(CodeEditorMenuItemCallbackAsync, ctxMenuCommandId);
                ctxMenuItem.BeforeQueryStatus += CodeEditorMenuItem_BeforeQueryStatusAsync;
                commandService.AddCommand(ctxMenuItem);

                // Command in project item context menu
                var projectItemCtxMenuCommandId = new CommandID(CommandSet, ProjectItemCtxMenuCommandId);
                var projectItemCtxMenuItem = new BlockingOleMenuCommand(ProjectItemMenuItemCallbackAsync, projectItemCtxMenuCommandId);
                projectItemCtxMenuItem.BeforeQueryStatus += ProjectItemMenuItem_BeforeQueryStatusAsync;
                commandService.AddCommand(projectItemCtxMenuItem);

                // Command in project context menu
                var solutionOrProjectCtxMenuCommandId = new CommandID(CommandSet, SolutionOrProjectCtxMenuCommandId);
                var solutionOrProjectCtxMenuItem = new BlockingOleMenuCommand(SolutionOrProjectMenuItemCallbackAsync, solutionOrProjectCtxMenuCommandId);
                solutionOrProjectCtxMenuItem.BeforeQueryStatus += SolutionOrProjectMenuItem_BeforeQueryStatusAsync;
                commandService.AddCommand(solutionOrProjectCtxMenuItem);
            }
        }

        private async Task CodeEditorMenuItem_BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuItem) {
                var selectionInCurrentViewAsync = await _codeConversion.GetSelectionInCurrentViewAsync(CodeConversion.IsCSFileName);
                menuItem.Visible = !selectionInCurrentViewAsync?.StreamSelectionSpan.IsEmpty ?? false;
            }
        }

        private async Task ProjectItemMenuItem_BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuItem) {
                menuItem.Visible = false;
                menuItem.Enabled = false;

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                string itemPath = (await VisualStudioInteraction.GetSingleSelectedItemOrDefaultAsync())?.ItemPath;
                if (itemPath == null || !CodeConversion.IsCSFileName(itemPath))
                    return;

                menuItem.Visible = true;
                menuItem.Enabled = true;
            }
        }

        private async Task SolutionOrProjectMenuItem_BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuItem) {
                var selectedProjectsAsync = await VisualStudioInteraction.GetSelectedProjectsAsync(ProjectExtension);
                menuItem.Visible = menuItem.Enabled = selectedProjectsAsync.Any();
            }
        }

        private async Task CodeEditorMenuItemCallbackAsync(object sender, EventArgs e)
        {
            var selectionInCurrentViewAsync = await _codeConversion.GetSelectionInCurrentViewAsync(CodeConversion.IsCSFileName);
            var span = selectionInCurrentViewAsync.SelectedSpans.First().Span;
            var currentViewHostAsync = await _codeConversion.GetCurrentViewHostAsync(CodeConversion.IsCSFileName);
            var textDocumentAsync = await currentViewHostAsync.GetTextDocumentAsync();
            await ConvertDocumentAsync(textDocumentAsync.FilePath, span);
        }

        private async Task ProjectItemMenuItemCallbackAsync(object sender, EventArgs e)
        {

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string itemPath = (await VisualStudioInteraction.GetSingleSelectedItemOrDefaultAsync())?.ItemPath;
            await ConvertDocumentAsync(itemPath, new Span(0, 0));
        }

        private async Task SolutionOrProjectMenuItemCallbackAsync(object sender, EventArgs e)
        {
            try {
                var projects = VisualStudioInteraction.GetSelectedProjectsAsync(ProjectExtension);
                await _codeConversion.PerformProjectConversionAsync<CSToVBConversion>(await projects);
            } catch (Exception ex) {
                await VisualStudioInteraction.ShowExceptionAsync(ServiceProvider, CodeConversion.ConverterTitle, ex);
            }
        }

        private async Task ConvertDocumentAsync(string documentPath, Span selected)
        {
            if (documentPath == null || !CodeConversion.IsCSFileName(documentPath))
                return;

            try {
                await _codeConversion.PerformDocumentConversionAsync<CSToVBConversion>(documentPath, selected);
            } catch (Exception ex) {
                await VisualStudioInteraction.ShowExceptionAsync(ServiceProvider, CodeConversion.ConverterTitle, ex);
            }
        }
    }
}
