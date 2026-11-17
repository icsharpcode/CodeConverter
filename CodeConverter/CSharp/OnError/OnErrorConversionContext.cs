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

    /// <summary>The type of the member being converted: the symbol enclosing the end of the method block is the member itself.</summary>
    public INamedTypeSymbol ContainingType =>
        SemanticModel.GetEnclosingSymbol(MethodNode.Span.End - 1) is { } symbol ? symbol as INamedTypeSymbol ?? symbol.ContainingType : null;
}
