using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// A local function can't use a ref local from its enclosing method (CS8175), e.g. the <c>ref var withBlock</c> of a <c>With</c> block over a structure field.
/// Each reference to a ref local is marked with the local's type, so that a local function can find the ones in its body and take them as <c>ref</c> parameters.
/// </summary>
internal static class RefLocalReferences
{
    private const string AnnotationKind = "CodeConverter.RefLocalReference";

    public static IdentifierNameSyntax Mark(IdentifierNameSyntax reference, TypeSyntax refLocalType) =>
        reference.WithAdditionalAnnotations(new SyntaxAnnotation(AnnotationKind, refLocalType.ToString()));

    /// <returns>One marked reference per ref local used in <paramref name="node"/>, and the local's type</returns>
    public static IEnumerable<(IdentifierNameSyntax Reference, TypeSyntax Type)> FindIn(SyntaxNode node) =>
        node.GetAnnotatedNodes(AnnotationKind).OfType<IdentifierNameSyntax>()
            .GroupBy(n => n.Identifier.ValueText)
            .Select(g => g.First())
            .Select(n => (n, SyntaxFactory.ParseTypeName(n.GetAnnotations(AnnotationKind).First().Data)));
}
