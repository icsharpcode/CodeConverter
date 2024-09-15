using ICSharpCode.CodeConverter.CSharp;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace ICSharpCode.CodeConverter.VsExtension;

internal class Guids
{
    internal static Guid ContextMenu => new("{d309f791-903f-11d0-9efc-00a0c911004f}");
}

[VisualStudioContribution]
internal class SolutionContextMenuCommandHandler : ConvertCommandBase
{
    public SolutionContextMenuCommandHandler(CodeConverterPackage package, CodeConversion codeConversion)
        : base(package, codeConversion) { }

    public override CommandConfiguration CommandConfiguration => new("%ICSharpCode.CodeConverter.VsExtension.SolutionContextMenuCommandHandler.DisplayName%") {
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu, CommandPlacement.VsctParent(Guids.ContextMenu, id: 537, priority: 0)]
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var projects = await VisualStudioInteraction.GetSelectedProjectsAsync(ProjectExtension);
        await _codeConversion.ConvertProjectsAsync<VBToCSConversion>(projects, cancellationToken);
    }
}