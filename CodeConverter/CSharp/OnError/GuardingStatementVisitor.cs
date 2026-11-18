using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Guards every statement the method body visitor converts, at any depth, so the visitor itself needn't know about resume points.
/// </summary>
internal sealed class GuardingStatementVisitor : VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>>
{
    private readonly VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> _wrappedVisitor;
    private readonly StatementGuards _guards;

    public GuardingStatementVisitor(VBasic.VisualBasicSyntaxVisitor<Task<SyntaxList<StatementSyntax>>> wrappedVisitor, StatementGuards guards)
    {
        _wrappedVisitor = wrappedVisitor;
        _guards = guards;
    }

    public override async Task<SyntaxList<StatementSyntax>> DefaultVisit(SyntaxNode node)
    {
        var converted = await _wrappedVisitor.Visit(node);
        return node is VBSyntax.StatementSyntax statement ? _guards.Guard(statement, converted) : converted;
    }
}
