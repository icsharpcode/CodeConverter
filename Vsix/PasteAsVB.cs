using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Windows;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.VB;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace ICSharpCode.CodeConverter.VsExtension
{
    /// <summary>
    ///     Command handler
    /// </summary>
    internal sealed class PasteAsVB
    {
        /// <summary>
        ///     Command ID.
        /// </summary>
        public const int MainMenuCommandId = 0x0300;

        /// <summary>
        ///     Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a3378a21-e939-40c9-9e4b-eb0cec7b7854");

        private readonly CodeConversion _codeConversion;

        /// <summary>
        ///     VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PasteAsVB" /> class.
        ///     Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="codeConversion">Instance of the code converter, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PasteAsVB(CodeConverterPackage package, CodeConversion codeConversion,
            OleMenuCommandService commandService)
        {
            this._package = package ?? throw new ArgumentNullException(nameof(package));
            _codeConversion = codeConversion;
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, MainMenuCommandId);
            var menuItem = package.CreateCommand(CodeEditorMenuItemCallbackAsync, menuCommandID);
            menuItem.BeforeQueryStatus += MainEditMenuItem_BeforeQueryStatusAsync;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        ///     Gets the instance of the command.
        /// </summary>
        public static PasteAsVB Instance { get; private set; }

        /// <summary>
        ///     Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => _package;

        /// <remarks>
        ///     Must be called from UI thread
        /// </remarks>
        public static void Initialize(CodeConverterPackage package, OleMenuCommandService menuCommandService,
            CodeConversion codeConversion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Instance = new PasteAsVB(package, codeConversion, menuCommandService);
        }

        private async Task MainEditMenuItem_BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuItem) {
                var selectionInCurrentViewAsync =
                    await VisualStudioInteraction.GetFirstSelectedSpanInCurrentViewAsync(ServiceProvider,
                        CodeConversion.IsVBFileName, true);
                menuItem.Visible = selectionInCurrentViewAsync != null;
            }
        }

        private async Task CodeEditorMenuItemCallbackAsync(CancellationToken cancellationToken)
        {
            try {
                await _codeConversion.PasteAsAsync<CSToVBConversion>(cancellationToken);
            } catch (Exception ex) {
                await _package.ShowExceptionAsync(ex);
            }
        }
    }
}