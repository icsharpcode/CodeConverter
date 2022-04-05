﻿using System.Collections.Immutable;
using System.Reflection;
using ICSharpCode.CodeConverter.Util.FromRoslynSdk;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;

namespace ICSharpCode.CodeConverter.Common;

/// <summary>
/// Known MEF bug means creating multiple workspaces outside VS context in parallel has race conditions: https://github.com/dotnet/roslyn/issues/24260
/// https://github.com/icsharpcode/CodeConverter/issues/376#issuecomment-625887068
/// </summary>
public static class ThreadSafeWorkspaceHelper
{
    /// <summary>
    /// Create an empty adhoc workspace
    /// </summary>
    public static AsyncLazy<AdhocWorkspace> CreateAdhocWorkspace { get; } = new(async () =>
    {
        var hostServices = await CreateHostServicesAsync(MefHostServices.DefaultAssemblies);
        return new AdhocWorkspace(hostServices);
    }, JoinableTaskFactorySingleton.Instance);

    /// <summary>
    /// Empty solution in an adhoc workspace
    /// </summary>
    public static AsyncLazy<Solution> EmptyAdhocSolution { get; } = new(async () =>
    {
        var adhocWorkspace = await CreateAdhocWorkspace.GetValueAsync();
        return adhocWorkspace.CurrentSolution;
    }, JoinableTaskFactorySingleton.Instance);

    /// <summary>
    /// Use this in all workspace creation
    /// </summary>
    public static async Task<HostServices> CreateHostServicesAsync(ImmutableArray<Assembly> assemblies)
    {
        var exportProvider = await CreateExportProviderFactoryAsync(assemblies);
        return MefHostServices.Create(exportProvider.CreateExportProvider().AsCompositionContext());
    }

    private static async Task<IExportProviderFactory> CreateExportProviderFactoryAsync(ImmutableArray<Assembly> assemblies)
    {
        var discovery = new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true);
        var parts = await discovery.CreatePartsAsync(assemblies);
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);

        var configuration = CompositionConfiguration.Create(catalog);
        var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
        return runtimeComposition.CreateExportProviderFactory();
    }
}