using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// A local function can't use a ref local from its enclosing method (CS8175), e.g. the <c>ref var withBlock</c> of a <c>With</c> block over a structure field.
/// Each reference to a ref local is marked with the local's type, so that a local function can find the ones in its body and take them as <c>ref</c> parameters.
/// </summary>
internal static class RefLocalReferences
{
    private const string AnnotationKind = "CodeConverter.RefLocalReference";

    /// <summary>An annotation's data can only be a string, but the annotation itself is kept on every copy of the node, so it can key the type.</summary>
    private static readonly ConditionalWeakTable<SyntaxAnnotation, TypeSyntax> RefLocalTypes = new();

    public static IdentifierNameSyntax Mark(IdentifierNameSyntax reference, TypeSyntax refLocalType)
    {
        var annotation = new SyntaxAnnotation(AnnotationKind);
        RefLocalTypes.Add(annotation, refLocalType);
        return reference.WithAdditionalAnnotations(annotation);
    }

    /// <returns>One marked reference per ref local that <paramref name="node"/> uses from outside it, and the local's type.
    /// A ref local declared inside, e.g. by a <c>With</c> block in a lambda argument, is in scope already.
    /// Generated names are unique within a method, so a name declared inside can't also refer to one outside.</returns>
    public static IEnumerable<(IdentifierNameSyntax Reference, TypeSyntax Type)> FindIn(SyntaxNode node)
    {
        var declaredInside = new HashSet<string>(node.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Identifier.ValueText));
        return node.GetAnnotatedNodes(AnnotationKind).OfType<IdentifierNameSyntax>()
            .Where(n => !declaredInside.Contains(n.Identifier.ValueText))
            .GroupBy(n => n.Identifier.ValueText)
            .Select(g => g.First())
            .Select(n => (n, GetRefLocalType(n)));
    }

    private static TypeSyntax GetRefLocalType(IdentifierNameSyntax reference) =>
        RefLocalTypes.TryGetValue(reference.GetAnnotations(AnnotationKind).First(), out var type)
            ? type
            : throw new InvalidOperationException($"Ref local reference {reference} was annotated without {nameof(Mark)}");
}
