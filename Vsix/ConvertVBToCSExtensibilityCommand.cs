using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace ICSharpCode.CodeConverter.VsExtension;

[VisualStudioContribution]
public class ConvertVBToCSExtensibilityCommand : Command
{
    private readonly CodeConverterPackage _package;

    public ConvertVBToCSExtensibilityCommand(CodeConverterPackage package)
    {
        _package = package;
    }

    public override CommandConfiguration CommandConfiguration => new("%a3378a21-e939-40c9-9e4b-eb0cec7b7854%");

    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
    {
        var codeConversion = CodeConverterPackage.CodeConversionInstance;
        var serviceProvider = CodeConverterPackage.Instance;
        if (codeConversion == null || serviceProvider == null) return;

        var itemsPath = await VisualStudioInteraction.GetSelectedItemsPathAsync(CodeConversion.IsVBFileName);
        if (itemsPath.Count > 0)
        {
            await codeConversion.ConvertDocumentsAsync<ICSharpCode.CodeConverter.CSharp.VBToCSConversion>(itemsPath, cancellationToken);
            return;
        }

        var projects = await VisualStudioInteraction.GetSelectedProjectsAsync(".vbproj");
        if (projects.Count > 0)
        {
            await codeConversion.ConvertProjectsAsync<ICSharpCode.CodeConverter.CSharp.VBToCSConversion>(projects, cancellationToken);
            return;
        }

        (string filePath, var selection) = await VisualStudioInteraction.GetCurrentFilenameAndSelectionAsync(serviceProvider, CodeConversion.IsVBFileName, false);
        if (filePath != null && selection != null) {
            await codeConversion.ConvertDocumentAsync<ICSharpCode.CodeConverter.CSharp.VBToCSConversion>(filePath, selection.Value, cancellationToken);
        }
    }
}
