using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// Marker interface
/// </summary>
internal interface IHoistedNode
{
}
    
internal record IfTrueBreak(ExpressionSyntax Condition) : IHoistedNode
{
    public StatementSyntax CreateIfTrueBreakStatement() => SyntaxFactory.IfStatement(Condition, SyntaxFactory.Block(SyntaxFactory.BreakStatement()));
}