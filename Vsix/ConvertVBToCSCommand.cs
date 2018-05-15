using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.VisualStudio.Text;
using OleMenuCommand = Microsoft.VisualStudio.Shell.OleMenuCommand;
using OleMenuCommandService = Microsoft.VisualStudio.Shell.OleMenuCommandService;

namespace CodeConverter.VsExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConvertVBToCSCommand
    {
        public const int MainMenuCommandId = 0x0200;
        public const int CtxMenuCommandId = 0x0201;
        public const int ProjectItemCtxMenuCommandId = 0x0202;
        public const int SolutionOrProjectCtxMenuCommandId = 0x0203;
        private const string ProjectExtension = ".vbproj";

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
        public static ConvertVBToCSCommand Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        IServiceProvider ServiceProvider {
            get {
                return this._package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(REConverterPackage package)
        {
            Instance = new ConvertVBToCSCommand(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertVBToCSCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        ConvertVBToCSCommand(REConverterPackage package)
        {
            this._package = package ?? throw new ArgumentNullException(nameof(package));
            _codeConversion = new CodeConversion(package, package.VsWorkspace, () => package.Options);

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null) {
                // Command in main menu
                var menuCommandId = new CommandID(CommandSet, MainMenuCommandId);
                var menuItem = new OleMenuCommand(CodeEditorMenuItemCallback, menuCommandId);
                menuItem.BeforeQueryStatus += CodeEditorMenuItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);

                // Command in code editor's context menu
                var ctxMenuCommandId = new CommandID(CommandSet, CtxMenuCommandId);
                var ctxMenuItem = new OleMenuCommand(CodeEditorMenuItemCallback, ctxMenuCommandId);
                ctxMenuItem.BeforeQueryStatus += CodeEditorMenuItem_BeforeQueryStatus;
                commandService.AddCommand(ctxMenuItem);

                // Command in project item context menu
                var projectItemCtxMenuCommandId = new CommandID(CommandSet, ProjectItemCtxMenuCommandId);
                var projectItemCtxMenuItem = new OleMenuCommand(ProjectItemMenuItemCallback, projectItemCtxMenuCommandId);
                projectItemCtxMenuItem.BeforeQueryStatus += ProjectItemMenuItem_BeforeQueryStatus;
                commandService.AddCommand(projectItemCtxMenuItem);

                // Command in project context menu
                var solutionOrProjectCtxMenuCommandId = new CommandID(CommandSet, SolutionOrProjectCtxMenuCommandId);
                var solutionOrProjectCtxMenuItem = new OleMenuCommand(SolutionOrProjectMenuItemCallback, solutionOrProjectCtxMenuCommandId);
                solutionOrProjectCtxMenuItem.BeforeQueryStatus += SolutionOrProjectMenuItem_BeforeQueryStatus;
                commandService.AddCommand(solutionOrProjectCtxMenuItem);
            }
        }

        void CodeEditorMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuItem = sender as OleMenuCommand;
            if (menuItem != null) {

                menuItem.Visible = !_codeConversion.GetSelectionInCurrentView(CodeConversion.IsVBFileName)?.StreamSelectionSpan.IsEmpty ?? false;
            }
        }

        void ProjectItemMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuItem = sender as OleMenuCommand;
            if (menuItem != null) {
                menuItem.Visible = false;
                menuItem.Enabled = false;

                string itemPath = VisualStudioInteraction.GetSingleSelectedItemOrDefault()?.ItemPath;
                if (itemPath == null || !CodeConversion.IsVBFileName(itemPath))
                    return;

                menuItem.Visible = true;
                menuItem.Enabled = true;
            }
        }

        private void SolutionOrProjectMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuItem = sender as OleMenuCommand;
            if (menuItem != null) {
                menuItem.Visible = menuItem.Enabled = VisualStudioInteraction.GetSelectedProjects(ProjectExtension).Any();
            }
        }

        async void CodeEditorMenuItemCallback(object sender, EventArgs e)
        {
            var span = _codeConversion.GetSelectionInCurrentView(CodeConversion.IsVBFileName).SelectedSpans.First().Span;
            await ConvertDocument(_codeConversion.GetCurrentViewHost(CodeConversion.IsVBFileName).GetTextDocument().FilePath, span);
        }
        
        async void ProjectItemMenuItemCallback(object sender, EventArgs e)
        {
            string itemPath = VisualStudioInteraction.GetSingleSelectedItemOrDefault()?.ItemPath;
            await ConvertDocument(itemPath, new Span(0, 0));
        }

        private async void SolutionOrProjectMenuItemCallback(object sender, EventArgs e)
        {
            try {
                var projects = VisualStudioInteraction.GetSelectedProjects(ProjectExtension);
                await _codeConversion.PerformProjectConversion<VBToCSConversion>(projects);
            } catch (Exception ex) {
                VisualStudioInteraction.ShowException(ServiceProvider, CodeConversion.ConverterTitle, ex);
            }
        }

        private async Task ConvertDocument(string documentPath, Span selected)
        {
            if (documentPath == null || !CodeConversion.IsVBFileName(documentPath))
                return;

            try {
                await _codeConversion.PerformDocumentConversion<VBToCSConversion>(documentPath, selected);
            }
            catch (Exception ex) {
                VisualStudioInteraction.ShowException(ServiceProvider, CodeConversion.ConverterTitle, ex);
            }
        }
    }
}
