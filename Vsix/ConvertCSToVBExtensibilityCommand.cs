using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace ICSharpCode.CodeConverter.VsExtension;

[VisualStudioContribution]
public class ConvertCSToVBExtensibilityCommand : Command
{
    private readonly CodeConverterPackage _package;

    public ConvertCSToVBExtensibilityCommand(CodeConverterPackage package)
    {
        _package = package;
    }

    public override CommandConfiguration CommandConfiguration => new("%a3378a21-e939-40c9-9e4b-eb0cec7b7854%");

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        // This will be wired up to the existing logic in Phase 2
        await Task.CompletedTask;
    }
}
