using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.OnError;

/// <summary>
/// What an emitter needs from the method body conversion.
/// </summary>
internal sealed record OnErrorConversionContext(VBasic.VisualBasicSyntaxNode MethodNode, SemanticModel SemanticModel, CommonConversions CommonConversions,
    MethodBodyExecutableStatementVisitor Visitor, HashSet<string> ExtraUsingDirectives)
{
    public async Task<List<StatementSyntax>> ConvertAsync(IEnumerable<VBSyntax.StatementSyntax> statements) =>
        (await statements.SelectManyAsync(async s => (IEnumerable<StatementSyntax>)await s.Accept(Visitor.CommentConvertingVisitor))).ToList();

    /// <summary>
    /// Converts the statements of a handler, keeping the comments of those that convert to nothing with the next statement (see <see cref="ElidedStatementTrivia"/>).
    /// </summary>
    /// <param name="elidedBefore">Statements before these that aren't converted by themselves (the handler label), whose comments are kept with the first statement.</param>
    public async Task<List<StatementSyntax>> ConvertHandlerAsync(IEnumerable<VBSyntax.StatementSyntax> statements, IEnumerable<VBSyntax.StatementSyntax> elidedBefore)
    {
        var converted = new List<StatementSyntax>();
        var elided = elidedBefore.ToList();
        foreach (var statement in statements) {
            var csStatements = (await statement.Accept(Visitor.CommentConvertingVisitor)).ToList();
            if (!csStatements.Any()) {
                elided.Add(statement);
                continue;
            }
            if (elided.Any()) csStatements[0] = ElidedStatementTrivia.PrependTo(csStatements[0], statement, elided);
            elided.Clear();
            converted.AddRange(csStatements);
        }
        return converted;
    }

    /// <summary>The type of the member being converted: the symbol enclosing the end of the method block is the member itself.</summary>
    public INamedTypeSymbol ContainingType =>
        SemanticModel.GetEnclosingSymbol(MethodNode.Span.End - 1) is { } symbol ? symbol as INamedTypeSymbol ?? symbol.ContainingType : null;

    /// <summary>
    /// Whether the shapes that declare local functions (a handler function, or a loop condition or <c>Case</c> expression moved out of the statement) can be used.
    /// A local function in a struct can't use <c>this</c> (CS1673), so an instance member of a struct can't have one.
    /// </summary>
    public bool CanDeclareLocalFunctions =>
        SemanticModel.GetEnclosingSymbol(MethodNode.Span.End - 1) is not { IsStatic: false, ContainingType.TypeKind: TypeKind.Struct };
}
