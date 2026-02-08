using System.Collections;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Use and add to this class with great care. There is great potential for issues in different VS versions if something private/internal got renamed.
/// </summary>
/// <remarks>
/// Each extension block groups extension methods by type, with the cached delegates stored in the containing class.
/// I've opted for using the minimal amount of hardcoded internal names (instead getting the runtime type of the argument) in the hope of being resilient against a simple rename.
/// However, if extra subclasses are created and there's no common base type containing the property, or the property itself is renamed, this will obviously break.
/// It'd be great to run a CI build configuration that updates all nuget packages to the latest prerelease and runs all the tests (this would also catch other breaking API changes before they hit).
/// </remarks>
internal static class CachedReflectedDelegates
{
    private static CachedReflectedDelegate<ISymbol, bool> IsMyGroupCollectionPropertyDelegate { get; } =
        new CachedReflectedDelegate<ISymbol, bool>("IsMyGroupCollectionProperty");

    private static CachedReflectedDelegate<ISymbol, ISymbol> AssociatedFieldDelegate { get; } =
        new CachedReflectedDelegate<ISymbol, ISymbol>("AssociatedField");

    extension(IPropertySymbol declaredSymbol)
    {
        public bool IsMyGroupCollectionProperty => IsMyGroupCollectionPropertyDelegate.GetValue(declaredSymbol);

        public ISymbol AssociatedField => AssociatedFieldDelegate.GetValue(declaredSymbol);
    }

    private static CachedReflectedDelegate<Location, SyntaxTree> PossiblyEmbeddedOrMySourceTreeDelegate { get; } =
        new CachedReflectedDelegate<Location, SyntaxTree>("PossiblyEmbeddedOrMySourceTree");

    extension(Location loc)
    {
        public SyntaxTree EmbeddedSyntaxTree => PossiblyEmbeddedOrMySourceTreeDelegate.GetValue(loc);
    }

    private static CachedReflectedDelegate<DataFlowAnalysis, IEnumerable<ISymbol>> VbUnassignedVariablesDelegate { get; } =
        new CachedReflectedDelegate<DataFlowAnalysis, IEnumerable<ISymbol>>("UnassignedVariables");

    extension(DataFlowAnalysis methodFlow)
    {
        /// <remarks>Unfortunately the roslyn UnassignedVariablesWalker and all useful collections created from it are internal only
        /// Other attempts using DataFlowsIn on each reference showed that "DataFlowsIn" even from an uninitialized variable (at least in the case of ints)
        /// https://github.com/dotnet/roslyn/blob/007022c37c6d21ee100728954bd75113e0dfe4bd/src/Compilers/VisualBasic/Portable/Analysis/FlowAnalysis/UnassignedVariablesWalker.vb#L15
        /// It'd be possible to see the result of the diagnostic analysis, but that would miss out value types, which don't cause a warning in VB
        /// PERF: Assume we'll only be passed one type of data flow analysis (VisualBasicDataFlowAnalysis)
        /// </remarks>
        public IEnumerable<ISymbol> VbUnassignedVariables =>
            VbUnassignedVariablesDelegate.GetValue(methodFlow);
    }
}
