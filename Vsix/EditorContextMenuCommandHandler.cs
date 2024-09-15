using ICSharpCode.CodeConverter.CSharp;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Text;

namespace ICSharpCode.CodeConverter.VsExtension;

[VisualStudioContribution]
internal class EditorContextMenuCommandHandler : ConvertCommandBase
{
    public EditorContextMenuCommandHandler(CodeConverterPackage package, CodeConversion codeConversion)
        : base(package, codeConversion) { }

    public override CommandConfiguration CommandConfiguration => new("%ICSharpCode.CodeConverter.VsExtension.EditorContextMenuCommandHandler.DisplayName%") {
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu]
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var (filePath, selection) = await VisualStudioInteraction.GetCurrentFilenameAndSelectionAsync(ServiceProvider, CodeConversion.IsVBFileName, false);
        if (filePath != null && selection != null) {
            await ConvertDocumentAsync(filePath, selection.Value, cancellationToken);
        }
    }

    private async Task ConvertDocumentAsync(string documentPath, Span selected, CancellationToken cancellationToken)
    {
        try {
            await _codeConversion.ConvertDocumentAsync<VBToCSConversion>(documentPath, selected, cancellationToken);
        } catch (Exception ex) {
            await ShowExceptionAsync(ex);
        }
    }
}