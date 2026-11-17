using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// A syntactic, conservative check of whether control can run off the end of converted statements, so no statements are added after code that always leaves.
/// </summary>
/// <remarks>
/// VB's flow analysis can't tell, as it doesn't know that <c>Resume Next</c> and <c>Resume</c> don't continue, but they are converted to a <c>return</c>.
/// Whatever isn't recognised as always leaving is treated as reachable, which at worst adds an unreachable statement the C# compiler warns about.
/// </remarks>
internal static class StatementReachability
{
    public static bool EndIsReachable(IEnumerable<StatementSyntax> statements) => statements.LastOrDefault() is not { } last || EndIsReachable(last);

    private static bool EndIsReachable(StatementSyntax statement) => statement switch {
        ReturnStatementSyntax or ThrowStatementSyntax or GotoStatementSyntax => false,
        BlockSyntax block => EndIsReachable(block.Statements),
        LabeledStatementSyntax labeled => EndIsReachable(labeled.Statement),
        IfStatementSyntax { Else: { } @else } ifStatement => EndIsReachable(ifStatement.Statement) || EndIsReachable(@else.Statement),
        SwitchStatementSyntax switchStatement => EndOfSwitchIsReachable(switchStatement),
        TryStatementSyntax tryStatement => EndIsReachable(tryStatement.Block) || tryStatement.Catches.Any(c => EndIsReachable(c.Block)),
        _ => true
    };

    /// <summary>It can only be left by running off its end if some section ends reachably, jumps out with <c>break</c>, or there's no <c>default</c> to catch every value.</summary>
    private static bool EndOfSwitchIsReachable(SwitchStatementSyntax switchStatement)
    {
        var hasDefault = switchStatement.Sections.SelectMany(s => s.Labels).Any(l => l is DefaultSwitchLabelSyntax ||
            l is CasePatternSwitchLabelSyntax { WhenClause: null, Pattern: DeclarationPatternSyntax { Designation: DiscardDesignationSyntax or SingleVariableDesignationSyntax } });
        return !hasDefault || switchStatement.Sections.Any(s => EndIsReachable(s.Statements) || BreaksOutOfTheSwitch(s));
    }

    private static bool BreaksOutOfTheSwitch(SwitchSectionSyntax section) =>
        section.Statements.SelectMany(s => s.DescendantNodesAndSelf(n => n is not (SwitchStatementSyntax or WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)))
            .OfType<BreakStatementSyntax>().Any();
}
