using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// Gives locals declared without an initializer the <c>= default</c> that VB's zeroed locals have.
/// </summary>
/// <remarks>
/// The converter leaves out an initializer that VB's flow analysis says isn't needed, but that analysis doesn't know a handler can run after any statement,
/// or that an error can skip the assignment. VB locals start zeroed, so <c>= default</c> is exact.
/// </remarks>
internal static class DefaultInitializers
{
    public static IEnumerable<StatementSyntax> Add(IEnumerable<StatementSyntax> statements) =>
        statements.Select(s => s is LocalDeclarationStatementSyntax { IsConst: false } declaration
            ? declaration.WithDeclaration(declaration.Declaration.WithVariables(SyntaxFactory.SeparatedList(declaration.Declaration.Variables.Select(v =>
                v.Initializer != null ? v : v.WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)))))))
            : s);
}
