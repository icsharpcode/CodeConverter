using Microsoft.VisualStudio.Extensibility;

namespace ICSharpCode.CodeConverter.VsExtension;

[VisualStudioContribution]
public class CodeConverterExtension : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new() {
        RequiresInProcessHosting = true,
        Metadata = null
    };
}
