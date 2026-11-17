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
/// Computed once per compilation, on first use.
/// </remarks>
internal class FieldWriteAnalysis : IFieldMutationOracle
{
    private static readonly ConditionalWeakTable<Compilation, FieldWriteAnalysis> PerCompilation = new();

    private readonly Compilation _compilation;
    private readonly Lazy<HashSet<IFieldSymbol>> _fieldsWrittenAfterConstruction;
    private readonly Lazy<bool> _hasFriendAssemblies;

    private FieldWriteAnalysis(Compilation compilation)
    {
        _compilation = compilation;
        _fieldsWrittenAfterConstruction = new Lazy<HashSet<IFieldSymbol>>(FindFieldsWrittenAfterConstruction);
        _hasFriendAssemblies = new Lazy<bool>(() => compilation.Assembly.GetAttributes()
            .Any(a => a.AttributeClass?.Name == nameof(InternalsVisibleToAttribute)));
    }

    public static FieldWriteAnalysis For(Compilation compilation) => PerCompilation.GetValue(compilation, c => new FieldWriteAnalysis(c));

    /// <remarks>Ignores <paramref name="call"/>: a call graph could narrow the answer to the fields the call can transitively write.</remarks>
    public bool MayChangeDuringCall(IFieldSymbol field, IOperation call)
    {
        field = field.OriginalDefinition;
        if (field.IsConst || field.IsReadOnly) return false;
        if (CanBeWrittenOutsideCompilation(field) || CanBeMutatedThroughMembers(field)) return true;
        return _fieldsWrittenAfterConstruction.Value.Contains(field);
    }

    private bool CanBeWrittenOutsideCompilation(IFieldSymbol field)
    {
        if (!SymbolEqualityComparer.Default.Equals(field.ContainingAssembly, _compilation.Assembly)) return true;
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

    private HashSet<IFieldSymbol> FindFieldsWrittenAfterConstruction()
    {
        var written = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
        foreach (var tree in _compilation.SyntaxTrees) {
            var semanticModel = _compilation.GetSemanticModel(tree, true);
            foreach (var name in tree.GetRoot().DescendantNodes().OfType<VBSyntax.IdentifierNameSyntax>()) {
                if (semanticModel.GetSymbolInfo(name).Symbol is not IFieldSymbol { IsConst: false, IsReadOnly: false } field) continue;
                var reference = name.Parent is VBSyntax.MemberAccessExpressionSyntax maes && maes.Name == name ? (SyntaxNode)maes : name;
                if (IsWrite(semanticModel, reference) && !IsDuringConstruction(semanticModel, reference, field)) {
                    written.Add(field.OriginalDefinition);
                }
            }
        }
        return written;
    }

    private static bool IsWrite(SemanticModel semanticModel, SyntaxNode reference)
    {
        if (IsWrittenBySyntax(reference)) return true;
        var operation = semanticModel.GetOperation(reference);
        return operation?.Parent switch {
            IAssignmentOperation a => a.Target == operation,
            IArgumentOperation a => a.Parameter?.RefKind is not RefKind.None,
            IForToLoopOperation l => l.LoopControlVariable == operation,
            IForEachLoopOperation l => l.LoopControlVariable == operation,
            IReDimClauseOperation => true,
            // Late-bound arguments are copied back when the method found at runtime takes them ByRef
            IDynamicInvocationOperation or IDynamicIndexerAccessOperation or IDynamicObjectCreationOperation => true,
            _ => false
        };
    }

    /// <summary>
    /// Statements whose operation tree doesn't show the field as written.
    /// </summary>
    private static bool IsWrittenBySyntax(SyntaxNode reference) => reference.Parent switch {
        VBSyntax.EraseStatementSyntax => true,
        VBSyntax.SimpleArgumentSyntax { Parent.Parent: VBSyntax.MidExpressionSyntax mid } arg => mid.ArgumentList.Arguments.First() == arg,
        _ => false
    };

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

    private static bool IsSharedDeclaration(SyntaxNode declaration)
    {
        var modifiers = declaration switch {
            VBSyntax.FieldDeclarationSyntax f => f.Modifiers,
            VBSyntax.PropertyStatementSyntax p => p.Modifiers,
            _ => default
        };
        return modifiers.Any(m => m.IsKind(VBasic.SyntaxKind.SharedKeyword)) ||
               declaration.FirstAncestorOrSelf<VBSyntax.TypeBlockSyntax>() is VBSyntax.ModuleBlockSyntax;
    }
}
