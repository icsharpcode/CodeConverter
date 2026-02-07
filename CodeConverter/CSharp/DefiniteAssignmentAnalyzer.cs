namespace ICSharpCode.CodeConverter.CSharp;

internal static class DefiniteAssignmentAnalyzer
{

    public static bool IsDefinitelyAssignedBeforeRead(ISymbol localSymbol, DataFlowAnalysis methodFlow)
    {
        if (!methodFlow.ReadInsideSafe().Contains(localSymbol)) return true;
        var unassignedVariables = methodFlow.VbUnassignedVariables;
        return unassignedVariables != null && !unassignedVariables.Contains(localSymbol, SymbolEqualityComparer.IncludeNullability);
    }
}