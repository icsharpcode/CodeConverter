using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// Sound without knowing what a call reaches: a field can only change during a call if something can write it after construction.
/// A field keeps its value while any call runs when it's <c>Const</c> or <c>ReadOnly</c>, or when only this compilation can write it
/// and nothing in it does, outside construction of the object (or type) that holds it.
/// </summary>
/// <remarks>
/// Construction is excluded because it has finished before a caller can read the field: a constructor's writes to its own instance's fields,
/// a shared constructor's writes to shared fields, and field initializers (which run as part of those). A lambda in a constructor can run later, so it isn't excluded.
/// Computed once per project, on first use. Each document is converted with a compilation of its own (after expanding its root),
/// so fields are matched by documentation ID rather than by symbol.
/// </remarks>
internal class FieldWriteAnalysis : IFieldMutationOracle
{
    private readonly Compilation _compilation;
    private readonly Lazy<HashSet<string>> _fieldsWrittenAfterConstruction;
    private readonly Lazy<bool> _hasFriendAssemblies;

    public FieldWriteAnalysis(Compilation compilation)
    {
        _compilation = compilation;
        _fieldsWrittenAfterConstruction = new Lazy<HashSet<string>>(FindFieldsWrittenAfterConstruction);
        _hasFriendAssemblies = new Lazy<bool>(() => compilation.Assembly.GetAttributes()
            .Any(a => a.AttributeClass?.Name == nameof(InternalsVisibleToAttribute)));
    }

    /// <remarks>Ignores <paramref name="call"/>: a call graph could narrow the answer to the fields the call can transitively write.</remarks>
    public bool MayChangeDuringCall(IFieldSymbol field, IOperation call)
    {
        field = field.OriginalDefinition;
        if (field.IsConst || field.IsReadOnly) return false;
        // An auto-property's backing field (or a WithEvents variable's) is written through the property, which this doesn't follow
        if (field.AssociatedSymbol is IPropertySymbol) return true;
        if (CanBeWrittenOutsideCompilation(field) || CanBeMutatedThroughMembers(field)) return true;
        return _fieldsWrittenAfterConstruction.Value.Contains(field.GetDocumentationCommentId());
    }

    private bool CanBeWrittenOutsideCompilation(IFieldSymbol field)
    {
        if (field.ContainingAssembly?.Name != _compilation.AssemblyName) return true;
        for (ISymbol symbol = field; symbol is not null and not INamespaceSymbol; symbol = symbol.ContainingSymbol) {
            switch (symbol.DeclaredAccessibility) {
                case Accessibility.Private:
                    return false;
                case Accessibility.Internal or Accessibility.ProtectedAndInternal:
                    return _hasFriendAssemblies.Value;
            }
        }
        return true;
    }

    /// <summary>
    /// A structure field can be changed by calling a method on it or assigning one of its members, not just by assigning the field.
    /// </summary>
    private static bool CanBeMutatedThroughMembers(IFieldSymbol field) =>
        field.Type.IsValueType && field.Type.SpecialType == SpecialType.None && field.Type.TypeKind != TypeKind.Enum;

    private HashSet<string> FindFieldsWrittenAfterConstruction()
    {
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tree in _compilation.SyntaxTrees) {
            var semanticModel = _compilation.GetSemanticModel(tree, true);
            foreach (var name in tree.GetRoot().DescendantNodes().OfType<VBSyntax.IdentifierNameSyntax>()) {
                if (semanticModel.GetSymbolInfo(name).Symbol is not IFieldSymbol { IsConst: false, IsReadOnly: false } field) continue;
                var reference = name.Parent is VBSyntax.MemberAccessExpressionSyntax maes && maes.Name == name ? (SyntaxNode)maes : name;
                if (semanticModel.GetOperation(reference) is { } operation && VariableWrites.IsWrite(operation) &&
                    !IsDuringConstruction(semanticModel, reference, field)) {
                    written.Add(field.OriginalDefinition.GetDocumentationCommentId());
                }
            }
        }
        return written;
    }

    private static bool IsDuringConstruction(SemanticModel semanticModel, SyntaxNode reference, IFieldSymbol field)
    {
        if (!field.IsStatic && reference is VBSyntax.MemberAccessExpressionSyntax { Expression: not (VBSyntax.MeExpressionSyntax or VBSyntax.MyClassExpressionSyntax) }) {
            return false;
        }
        foreach (var ancestor in reference.Ancestors()) {
            switch (ancestor) {
                case VBSyntax.LambdaExpressionSyntax:
                    return false;
                case VBSyntax.ConstructorBlockSyntax ctor:
                    return IsConstructing(semanticModel.GetDeclaredSymbol(ctor.SubNewStatement), field);
                case VBSyntax.FieldDeclarationSyntax or VBSyntax.PropertyStatementSyntax:
                    var containingType = semanticModel.GetDeclaredSymbol(ancestor.FirstAncestorOrSelf<VBSyntax.TypeBlockSyntax>()?.BlockStatement);
                    return SymbolEqualityComparer.Default.Equals(containingType, field.ContainingType) && IsSharedDeclaration(ancestor) == field.IsStatic;
                case VBSyntax.MethodBlockBaseSyntax or VBSyntax.TypeBlockSyntax:
                    return false;
            }
        }
        return false;
    }

    private static bool IsConstructing(IMethodSymbol constructor, IFieldSymbol field) =>
        SymbolEqualityComparer.Default.Equals(constructor?.ContainingType, field.ContainingType) && constructor.IsStatic == field.IsStatic;

    /// <param name="declaration">A field declaration or property statement.</param>
    private static bool IsSharedDeclaration(SyntaxNode declaration)
    {
        var modifiers = declaration is VBSyntax.FieldDeclarationSyntax f ? f.Modifiers : ((VBSyntax.PropertyStatementSyntax)declaration).Modifiers;
        return modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.SharedKeyword)) ||
               declaration.FirstAncestorOrSelf<VBSyntax.TypeBlockSyntax>() is VBSyntax.ModuleBlockSyntax;
    }
}
