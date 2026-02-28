using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Common;

/// <summary>
/// Provides reference assemblies for use in Roslyn compilations during tests.
/// Uses Basic.Reference.Assemblies packages to avoid platform-specific assembly loading.
/// </summary>
public static class DefaultReferences
{
    public static IReadOnlyCollection<PortableExecutableReference> NetStandard2 { get; } =
        Basic.Reference.Assemblies.NetStandard20.References.All;

    private static readonly IReadOnlyCollection<PortableExecutableReference> _net80All =
        Basic.Reference.Assemblies.Net80.References.All
            .Concat(Basic.Reference.Assemblies.Net80Windows.References.All)
            .ToArray();

    public static IReadOnlyCollection<PortableExecutableReference> With(params Assembly[] assemblies) =>
        _net80All
            .Concat(RefsFromAssemblies(assemblies))
            .ToArray();

    private static IEnumerable<PortableExecutableReference> RefsFromAssemblies(Assembly[] assemblies) =>
        assemblies
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && File.Exists(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));
}
