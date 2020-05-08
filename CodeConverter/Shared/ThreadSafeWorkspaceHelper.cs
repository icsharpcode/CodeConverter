using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util.FromRoslynSdk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace ICSharpCode.CodeConverter.Shared
{
    /// <summary>
    /// Known MEF bug means creating multiple workspaces outside VS context in parallel has race conditions: https://github.com/dotnet/roslyn/issues/24260
    /// https://github.com/icsharpcode/CodeConverter/issues/376#issuecomment-625887068
    /// </summary>
    public static class ThreadSafeWorkspaceHelper
    {
        /// <summary>
        /// Use this in all workspace creation
        /// </summary>
        public static HostServices HostServices {
            get {
                var exportProvider = ExportProviderFactory.Value.CreateExportProvider();
                return MefHostServices.Create(exportProvider.AsCompositionContext());
            }
        }

        /// <summary>
        /// Empty solution in an adhoc workspace
        /// </summary>
        public static Solution EmptyAdhocSolution => LazyAdhocSolution.Value;

        private static readonly Lazy<IExportProviderFactory> ExportProviderFactory = new Lazy<IExportProviderFactory>(CreateExportProviderFactory);

        private static IExportProviderFactory CreateExportProviderFactory()
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - Consider making a joinable task factory available so we can use AsyncLazy
            return Task.Run(async () => await CreateExportProviderFactoryAsync()).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        private static async Task<IExportProviderFactory> CreateExportProviderFactoryAsync()
        {
            var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
            var parts = await discovery.CreatePartsAsync(MefHostServices.DefaultAssemblies);
            var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);

            var configuration = CompositionConfiguration.Create(catalog);
            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            return runtimeComposition.CreateExportProviderFactory();
        }

        private static Lazy<Solution> LazyAdhocSolution = new Lazy<Solution>(() => {
            return new AdhocWorkspace(HostServices).CurrentSolution;
        });
        
    }
}