using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ICSharpCode.CodeConverter.CSharp;

internal static class VbSyntaxNodeExtensions
{
    public static CSSyntax.ExpressionSyntax ParenthesizeIfPrecedenceCouldChange(this VBasic.VisualBasicSyntaxNode node, CSSyntax.ExpressionSyntax expression)
    {
        return PrecedenceCouldChange(node) ? SyntaxFactory.ParenthesizedExpression(expression) : expression;
    }

    public static bool PrecedenceCouldChange(this VBasic.VisualBasicSyntaxNode node)
    {
        bool parentIsBinaryExpression = node is VBSyntax.BinaryExpressionSyntax;
        bool parentIsLambda = node.Parent is VBSyntax.LambdaExpressionSyntax;
        bool parentIsNonArgumentExpression = node.Parent is VBSyntax.ExpressionSyntax && node.Parent is not VBSyntax.ArgumentSyntax;
        bool parentIsParenthesis = node.Parent is VBSyntax.ParenthesizedExpressionSyntax;
        bool parentIsMemberAccessExpression = node.Parent is VBSyntax.MemberAccessExpressionSyntax;

        return parentIsMemberAccessExpression || parentIsNonArgumentExpression && !parentIsBinaryExpression && !parentIsLambda && !parentIsParenthesis;
    }

    public static bool AlwaysHasBooleanTypeInCSharp(this VBSyntax.ExpressionSyntax vbNode)
    {
        var parent = vbNode.SkipOutOfParens()?.Parent;

        return parent is VBSyntax.SingleLineIfStatementSyntax singleLine && singleLine.Condition == vbNode ||
               parent is VBSyntax.IfStatementSyntax ifStatement && ifStatement.Condition == vbNode ||
               parent is VBSyntax.ElseIfStatementSyntax elseIfStatement && elseIfStatement.Condition == vbNode ||
               parent is VBSyntax.TernaryConditionalExpressionSyntax ternary && ternary.Condition == vbNode ||
               parent is VBSyntax.WhereClauseSyntax;
    }

    public static TokenContext GetMemberContext(this VBSyntax.StatementSyntax member)
    {
        var parentType = member.GetAncestorOrThis<VBSyntax.TypeBlockSyntax>();
        var parentTypeKind = parentType?.Kind();
        switch (parentTypeKind) {
            case VBasic.SyntaxKind.ModuleBlock:
                return TokenContext.MemberInModule;
            case VBasic.SyntaxKind.ClassBlock:
                return TokenContext.MemberInClass;
            case VBasic.SyntaxKind.InterfaceBlock:
                return TokenContext.MemberInInterface;
            case VBasic.SyntaxKind.StructureBlock:
                return TokenContext.MemberInStruct;
            default:
                throw new ArgumentOutOfRangeException(nameof(member));
        }
    }
}