using ICSharpCode.CodeConverter.CSharp;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace ICSharpCode.CodeConverter.VsExtension;

[VisualStudioContribution]
internal class ProjectItemContextMenuCommandHandler : ConvertCommandBase
{
    public ProjectItemContextMenuCommandHandler(CodeConverterPackage package, CodeConversion codeConversion)
        : base(package, codeConversion) { }

    public override CommandConfiguration CommandConfiguration => new("%ICSharpCode.CodeConverter.VsExtension.ProjectItemContextMenuCommandHandler.DisplayName%") {
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu]
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var itemsPath = await VisualStudioInteraction.GetSelectedItemsPathAsync(CodeConversion.IsVBFileName);
        await ConvertDocumentsAsync(itemsPath, cancellationToken);
    }

    private async Task ConvertDocumentsAsync(IReadOnlyCollection<string> documentsPath, CancellationToken cancellationToken)
    {
        if (documentsPath.Count == 0) {
            await VisualStudioInteraction.ShowMessageBoxAsync("Unable to find any files valid for conversion.");
            return;
        }

        try {
            await _codeConversion.ConvertDocumentsAsync<VBToCSConversion>(documentsPath, cancellationToken);
        } catch (Exception ex) {
            await ShowExceptionAsync(ex);
        }
    }
}