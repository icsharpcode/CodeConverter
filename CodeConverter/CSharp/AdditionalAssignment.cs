using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp;

internal class AdditionalAssignment : IHoistedNode
{
    private readonly ExpressionSyntax _assignment;

    public AdditionalAssignment(ExpressionSyntax lhs, ExpressionSyntax rhs)
        : this(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            lhs ?? throw new ArgumentNullException(nameof(lhs)), rhs ?? throw new ArgumentNullException(nameof(rhs))))
    {
    }

    private AdditionalAssignment(ExpressionSyntax assignment) => _assignment = assignment;

    /// <summary>
    /// A parameterized property becomes a pair of methods in C#, so there's nothing to assign to: call its setter instead.
    /// </summary>
    public static AdditionalAssignment ViaSetter(InvocationExpressionSyntax setterCall) => new(setterCall);

    public static StatementSyntax CreateAssignment(AdditionalAssignment additionalAssignment) =>
        SyntaxFactory.ExpressionStatement(additionalAssignment._assignment);
}
