using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
#pragma warning disable CEE0012 // TODO: I can't find any information on what this means

namespace ICSharpCode.CodeConverter.VsExtension;

/// <summary>
/// Implements the VS package exposed by this assembly.
/// 
/// This package will load when:
/// * Visual Studio has been configured not to support UIContextRules and has a solution with a csproj or vbproj
/// * Someone clicks one of the menu items
/// * Someone opens the options page (it doesn't need to load in this case, but it seems to anyway)
/// </summary>
/// <remarks>
/// Until the package is loaded, converting a multiple selection of projects won't work because there's no way to set a ProvideUIContextRule that covers that case
/// </remarks>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
[VisualStudioContribution]
public sealed class CodeConverterPackage : Extension
{
    private readonly VisualStudioExtensibility _extensibility;

    internal Cancellation PackageCancellation { get; } = new();

    /// <summary>
    /// Initializes a new instance of package class.
    /// </summary>
    public CodeConverterPackage(VisualStudioExtensibility extensibility)
    {
        _extensibility = extensibility;
        // Inside this method you can place any initialization code that does not require
        // any Visual Studio service because at this point the package object is created but
        // not sited yet inside Visual Studio environment. The place to do all the other
        // initialization is the Initialize method.
    }

    protected override void Dispose(bool disposing)
    {
        PackageCancellation.Dispose();
        base.Dispose(disposing);
    }

    public override ExtensionConfiguration ExtensionConfiguration {
        get {
            var activationConstraints = new[] {ProjectCapability.CSharp, ProjectCapability.VB}
                .Select(ActivationConstraint.ActiveProjectCapability);
            return new ExtensionConfiguration() {
                LoadedWhen = ActivationConstraint.Or(activationConstraints.ToArray()),
                RequiresInProcessHosting = false,
                Metadata = new ExtensionMetadata("ICSharpCode.CodeConverter", ExtensionAssemblyVersion, "ICSharpCode", "Code Converter", "Convert projects/files between VB.NET and C#")
            };

        }
    }
    [Experimental("VSEXTPREVIEW_SETTINGS")]
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
        this.InitializeSettingsAsync(_extensibility).Forget();
    }
    [Experimental("VSEXTPREVIEW_SETTINGS")]
    private async Task InitializeSettingsAsync(VisualStudioExtensibility extensibility)
    {
        await extensibility.Settings().SubscribeAsync(
            [ConverterSettings.ConverterSettingsCategory],
            CancellationToken.None,
            values => {
                // look up in dictionary the values
            });
    }
}