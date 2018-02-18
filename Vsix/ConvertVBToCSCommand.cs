using System;
using System.ComponentModel.Design;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

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

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a3378a21-e939-40c9-9e4b-eb0cec7b7854");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        readonly REConverterPackage package;

        private CodeConversion codeConversion;

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
                return this.package;
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
            if (package == null) {
                throw new ArgumentNullException(nameof(package));
            }

            this.package = package;
            codeConversion = new CodeConversion(package, package.VsWorkspace);

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null) {
                // Command in main menu
                var menuCommandID = new CommandID(CommandSet, MainMenuCommandId);
                var menuItem = new OleMenuCommand(CodeEditorMenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += CodeEditorMenuItem_BeforeQueryStatus;
                commandService.AddCommand(menuItem);

                // Command in code editor's context menu
                var ctxMenuCommandID = new CommandID(CommandSet, CtxMenuCommandId);
                var ctxMenuItem = new OleMenuCommand(CodeEditorMenuItemCallback, ctxMenuCommandID);
                ctxMenuItem.BeforeQueryStatus += CodeEditorMenuItem_BeforeQueryStatus;
                commandService.AddCommand(ctxMenuItem);

                // Command in project item context menu
                var projectItemCtxMenuCommandID = new CommandID(CommandSet, ProjectItemCtxMenuCommandId);
                var projectItemCtxMenuItem = new OleMenuCommand(ProjectItemMenuItemCallback, projectItemCtxMenuCommandID);
                projectItemCtxMenuItem.BeforeQueryStatus += ProjectItemMenuItem_BeforeQueryStatus;
                commandService.AddCommand(projectItemCtxMenuItem);
            }
        }

        void CodeEditorMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuItem = sender as OleMenuCommand;
            if (menuItem != null) {
                
                menuItem.Visible = !codeConversion.GetVBSelectionInCurrentView()?.StreamSelectionSpan.IsEmpty ?? false;
            }
        }

        void ProjectItemMenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuItem = sender as OleMenuCommand;
            if (menuItem != null) {
                menuItem.Visible = false;
                menuItem.Enabled = false;

                string itemPath = VisualStudioInteraction.GetSingleSelectedItemOrDefault()?.ItemPath;
                var fileInfo = new FileInfo(itemPath);
                if (!CodeConversion.IsVBFileName(fileInfo.Name))
                    return;

                menuItem.Visible = true;
                menuItem.Enabled = true;
            }
        }

        async void CodeEditorMenuItemCallback(object sender, EventArgs e)
        {
            string selectedText = codeConversion.GetVBSelectionInCurrentView().StreamSelectionSpan.GetText();
            await ConvertVbDocument(codeConversion.GetCurrentVBViewHost().GetTextDocument().FilePath, selectedText);
        }

        async void ProjectItemMenuItemCallback(object sender, EventArgs e)
        {
            string itemPath = VisualStudioInteraction.GetSingleSelectedItemOrDefault()?.ItemPath;
            await ConvertVbDocument(itemPath);
        }

        private async Task ConvertVbDocument(string documentPath, string selectionText = "")
        {
            var fileInfo = new FileInfo(documentPath);
            if (!CodeConversion.IsVBFileName(fileInfo.Name))
                return;

            try {
                await codeConversion.PerformVBToCSConversion(documentPath, selectionText);
            }
            catch (Exception ex) {
                VisualStudioInteraction.ShowException(ServiceProvider, CodeConversion.VBToCSConversionTitle, ex);
            }
        }
    }
}
