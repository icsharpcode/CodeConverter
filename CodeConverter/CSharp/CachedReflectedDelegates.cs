using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Use and add to this class with great care. There is great potential for issues in different VS versions if something private/internal got renamed.
    /// </summary>
    /// <remarks>
    /// Lots of cases need a method and a backing field in a way that's awkward to wrap into a single object, so I've put them adjacent for easy reference.
    /// I've opted for using the minimal amount of hardcoded internal names (instead getting the runtime type of the argument) in the hope of being resilient against a simple rename.
    /// However, if extra subclasses are created and there's no common base type containing the property, or the property itself is renamed, this will obviously break.
    /// It'd be great to run a CI build configuration that updates all nuget packages to the latest prerelease and runs all the tests (this would also catch other breaking API changes before they hit).
    /// </remarks>
    internal static class CachedReflectedDelegates
    {
        public static bool IsMyGroupCollectionProperty(this IPropertySymbol declaredSymbol) =>
            GetCachedReflectedPropertyDelegate(declaredSymbol, "IsMyGroupCollectionProperty", ref _isMyGroupCollectionProperty);
        private static Func<ISymbol, bool> _isMyGroupCollectionProperty;

        public static ISymbol GetAssociatedField(this IPropertySymbol declaredSymbol) =>
            GetCachedReflectedPropertyDelegate(declaredSymbol, "AssociatedField", ref _associatedField);
        private static Func<ISymbol, ISymbol> _associatedField;

        public static SyntaxTree GetEmbeddedSyntaxTree(this Location loc) =>
            GetCachedReflectedPropertyDelegate(loc, "PossiblyEmbeddedOrMySourceTree", ref _possiblyEmbeddedOrMySourceTree);
        private static Func<Location, SyntaxTree> _possiblyEmbeddedOrMySourceTree;

        /// <remarks>Unfortunately the roslyn UnassignedVariablesWalker and all useful collections created from it are internal only
        /// Other attempts using DataFlowsIn on each reference showed that "DataFlowsIn" even from an uninitialized variable (at least in the case of ints)
        /// https://github.com/dotnet/roslyn/blob/007022c37c6d21ee100728954bd75113e0dfe4bd/src/Compilers/VisualBasic/Portable/Analysis/FlowAnalysis/UnassignedVariablesWalker.vb#L15
        /// It'd be possible to see the result of the diagnostic analysis, but that would miss out value types, which don't cause a warning in VB
        /// PERF: Assume we'll only be passed one type of data flow analysis (VisualBasicDataFlowAnalysis)
        /// </remarks>
        public static IEnumerable<ISymbol> GetVbUnassignedVariables(this DataFlowAnalysis methodFlow) =>
            GetCachedReflectedPropertyDelegate(methodFlow, "UnassignedVariables", ref _vbUnassignedVariables).Cast<ISymbol>();
        private static Func<DataFlowAnalysis, IEnumerable> _vbUnassignedVariables;

        private static TDesiredTarget GetCachedReflectedPropertyDelegate<TDesiredArg, TDesiredTarget>(TDesiredArg instance, string propertyToAccess,
            ref Func<TDesiredArg, TDesiredTarget> cachedDelegate)
        {
            if (cachedDelegate != null) return cachedDelegate(instance);

            var getDelegate = instance.ReflectedPropertyGetter(propertyToAccess)
                ?.CreateOpenInstanceDelegateForcingType<TDesiredArg, TDesiredTarget>();
            if (getDelegate == null) {
                Debug.Fail($"Delegate not found for {instance.GetType()}");
                return default;
            }

            cachedDelegate = getDelegate;
            return cachedDelegate(instance);
        }
    }
}
