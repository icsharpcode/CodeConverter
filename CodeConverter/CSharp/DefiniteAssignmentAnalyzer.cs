namespace ICSharpCode.CodeConverter.CSharp;

internal static class DefiniteAssignmentAnalyzer
{

    public static bool IsDefinitelyAssignedBeforeRead(ISymbol localSymbol, DataFlowAnalysis methodFlow)
    {
        if (!methodFlow.ReadInside.Contains(localSymbol)) return true;
        var unassignedVariables = methodFlow.GetVbUnassignedVariables();
        return unassignedVariables != null && !unassignedVariables.Contains(localSymbol, SymbolEqualityComparer.IncludeNullability);
    }
}