using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Marker interface
/// </summary>
internal interface IHoistedNode
{
}

internal record PostIfTrueBlock(ExpressionSyntax Condition, StatementSyntax Statement) : IHoistedNode
{
    public StatementSyntax CreateIfTrueBreakStatement() => SyntaxFactory.IfStatement(Condition, SyntaxFactory.Block(Statement));
}