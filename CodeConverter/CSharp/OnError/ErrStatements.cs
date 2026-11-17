using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// The calls VB's lowering makes to keep <c>Err</c> up to date, or nothing when <see cref="ErrUsage"/> says nobody reads it.
/// </summary>
internal sealed class ErrStatements
{
    private const string CompilerServicesNamespace = "Microsoft.VisualBasic.CompilerServices";
    private readonly HashSet<string> _extraUsingDirectives;

    public ErrStatements(bool isMaintained, HashSet<string> extraUsingDirectives)
    {
        IsMaintained = isMaintained;
        _extraUsingDirectives = extraUsingDirectives;
    }

    public bool IsMaintained { get; }

    /// <summary>Where VB clears <c>Err</c>: each <c>On Error</c> and <c>Resume</c> statement, and leaving the method while in a handler.</summary>
    public SyntaxList<StatementSyntax> Clear() =>
        IsMaintained ? SyntaxFactory.SingletonList(ProjectDataCall("ClearProjectError")) : default;

    /// <summary>A catch-all, as VB catches any exception, recording it in <c>Err</c> first as VB's catch does.</summary>
    public CatchClauseSyntax CatchClause(string exceptionVariableName, IEnumerable<StatementSyntax> statements)
    {
        if (!IsMaintained) return SyntaxFactory.CatchClause().WithBlock(SyntaxFactory.Block(statements));
        _extraUsingDirectives.Add("System");
        var declaration = SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName(nameof(Exception)), SyntaxFactory.Identifier(exceptionVariableName));
        var setError = ProjectDataCall("SetProjectError", SyntaxFactory.IdentifierName(exceptionVariableName));
        return SyntaxFactory.CatchClause(declaration, null, SyntaxFactory.Block(statements.Prepend(setError)));
    }

    private StatementSyntax ProjectDataCall(string methodName, params ExpressionSyntax[] arguments)
    {
        _extraUsingDirectives.Add(CompilerServicesNamespace);
        var method = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("ProjectData"), SyntaxFactory.IdentifierName(methodName));
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(method,
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument)))));
    }
}
