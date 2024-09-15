using ICSharpCode.CodeConverter.CSharp;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace ICSharpCode.CodeConverter.VsExtension;

[VisualStudioContribution]
internal class ProjectContextMenuCommandHandler : ConvertCommandBase
{
    public ProjectContextMenuCommandHandler(CodeConverterPackage package, CodeConversion codeConversion)
        : base(package, codeConversion) { }

    public override CommandConfiguration CommandConfiguration => new("%ICSharpCode.CodeConverter.VsExtension.ProjectContextMenuCommandHandler.DisplayName%") {
        Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu]
    };

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var projects = await VisualStudioInteraction.GetSelectedProjectsAsync(ProjectExtension);
        await _codeConversion.ConvertProjectsAsync<VBToCSConversion>(projects, cancellationToken);
    }
}