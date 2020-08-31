using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.VB;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace ICSharpCode.CodeConverter.VsExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PasteAsVB
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int MainMenuCommandId = 0x0300;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a3378a21-e939-40c9-9e4b-eb0cec7b7854");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private readonly object _codeConversion;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteAsVB"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="codeConversion">Instance of the code converter, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PasteAsVB(CodeConverterPackage package, CodeConversion codeConversion, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            _codeConversion = codeConversion;
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, MainMenuCommandId);
            var menuItem = package.CreateCommand(CodeEditorMenuItemCallbackAsync, menuCommandID);
            menuItem.BeforeQueryStatus += MainEditMenuItem_BeforeQueryStatusAsync;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PasteAsVB Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider {
            get {
                return this.package;
            }
        }

 //       /// <summary>
//        /// Initializes the singleton instance of the command.
//        /// </summary>
//        /// <param name="package">Owner package, not null.</param>
        //public static async Task InitializeAsync(AsyncPackage package)
        //{
        //    // Switch to the main thread - the call to AddCommand in PasteAsVB's constructor requires
        //    // the UI thread.
        //    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

        //    OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        //    Instance = new PasteAsVB(package, commandService);
        //}
        /// <remarks>
        /// Must be called from UI thread
        /// </remarks>
        public static void Initialize(CodeConverterPackage package, OleMenuCommandService menuCommandService, CodeConversion codeConversion)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //Instance = new ConvertVBToCSCommand(package, codeConversion, menuCommandService);
            Instance = new PasteAsVB(package, codeConversion, menuCommandService);
        }
        private async Task MainEditMenuItem_BeforeQueryStatusAsync(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand menuItem) {
                var selectionInCurrentViewAsync = await VisualStudioInteraction.GetFirstSelectedSpanInCurrentViewAsync(ServiceProvider, CodeConversion.IsVBFileName, true);
                menuItem.Visible = selectionInCurrentViewAsync != null;
            }
        }
        private async Task CodeEditorMenuItemCallbackAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var text = Clipboard.GetText();
            var convertTextOnly = await ProjectConversion.ConvertTextAsync<CSToVBConversion>(text, conversionOptions: new TextConversionOptions(DefaultReferences.NetStandard2), cancellationToken: cancellationToken);
            await VisualStudioInteraction.WriteToCurrentWindowAsync(ServiceProvider, convertTextOnly.ConvertedCode);
        }

    }
}

