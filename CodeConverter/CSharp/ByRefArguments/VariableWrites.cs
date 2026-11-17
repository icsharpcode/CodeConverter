using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// Every VB construct that stores to a variable (local, parameter or field), so analyses that ask "can this change?" agree on what a write is.
/// </summary>
internal static class VariableWrites
{
    /// <param name="reference">A local, parameter or field reference.</param>
    public static bool IsWrite(IOperation reference) => IsWrittenBySyntax(reference.Syntax) || reference.Parent switch {
        IAssignmentOperation a => a.Target == reference,
        // Without a known parameter (in code that doesn't compile), assume it could be ByRef
        IArgumentOperation a => a.Parameter is null || a.Parameter.RefKind != RefKind.None,
        IForToLoopOperation l => l.LoopControlVariable == reference,
        IForEachLoopOperation l => l.LoopControlVariable == reference,
        IReDimClauseOperation => true,
        // Late-bound arguments are copied back when the method found at runtime takes them ByRef
        IDynamicInvocationOperation or IDynamicIndexerAccessOperation or IDynamicObjectCreationOperation => true,
        _ => false
    };

    /// <summary>
    /// Statements whose operation tree doesn't show the variable as written.
    /// </summary>
    private static bool IsWrittenBySyntax(SyntaxNode reference) => reference.Parent switch {
        VBSyntax.EraseStatementSyntax => true,
        VBSyntax.SimpleArgumentSyntax { Parent.Parent: VBSyntax.MidExpressionSyntax mid } arg => mid.ArgumentList.Arguments.First() == arg,
        _ => false
    };
}
