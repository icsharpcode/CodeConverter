using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp;

/// <summary>
/// A ByRef argument that VB copies back is used twice: read before the call, and written after it.
/// VB evaluates the target's receiver and index arguments once, and the write reuses their values
/// (Roslyn's UseTwiceRewriter.vb, called from RewriteByRefArgumentWithCopyBack in LocalRewriter_Call.vb).
/// The C# read and write-back are separate expressions, so a part that could have a different value the second time is hoisted into a temporary.
/// </summary>
/// <remarks>
/// VB copies every part except constants and Me. A local, parameter or field gives the same value again unless it's written while the
/// enclosing expression runs, so it's only copied when that expression writes it (as a ByRef argument, or by assignment in a lambda).
/// A field that the called method itself assigns is still read again: we can't see that cheaply, and it's rare.
/// </remarks>
internal class ByRefTargetTemporaries
{
    private readonly SemanticModel _semanticModel;
    private readonly ITypeContext _typeContext;
    private readonly VBSyntax.ExpressionSyntax _vbTarget;
    private readonly Lazy<HashSet<ISymbol>> _writtenSymbols;

    private ByRefTargetTemporaries(SemanticModel semanticModel, ITypeContext typeContext, VBSyntax.ExpressionSyntax vbTarget)
    {
        _semanticModel = semanticModel;
        _typeContext = typeContext;
        _vbTarget = vbTarget;
        _writtenSymbols = new Lazy<HashSet<ISymbol>>(GetSymbolsWrittenInEnclosingExpression);
    }

    /// <returns>The C# target with each part VB evaluates once replaced by a hoisted temporary, where its value could otherwise differ.</returns>
    public static CSSyntax.ExpressionSyntax Hoist(SemanticModel semanticModel, ITypeContext typeContext, VBSyntax.ExpressionSyntax vbTarget, CSSyntax.ExpressionSyntax csTarget) =>
        new ByRefTargetTemporaries(semanticModel, typeContext, vbTarget).Hoist(csTarget);

    private CSSyntax.ExpressionSyntax Hoist(CSSyntax.ExpressionSyntax csTarget)
    {
        return (_semanticModel.GetOperation(_vbTarget), csTarget) switch {
            (IPropertyReferenceOperation pro, CSSyntax.InvocationExpressionSyntax getterCall) =>
                getterCall.WithExpression(HoistReceiver(getterCall.Expression, pro))
                    .WithArgumentList(getterCall.ArgumentList.WithArguments(HoistArguments(getterCall.ArgumentList.Arguments, pro))),
            (IPropertyReferenceOperation pro, CSSyntax.ElementAccessExpressionSyntax indexer) =>
                indexer.WithArgumentList(indexer.ArgumentList.WithArguments(HoistArguments(indexer.ArgumentList.Arguments, pro))),
            (IArrayElementReferenceOperation element, CSSyntax.ElementAccessExpressionSyntax elementAccess) =>
                elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(HoistIndices(elementAccess.ArgumentList.Arguments, element.Indices))),
            (IPropertyReferenceOperation pro, CSSyntax.MemberAccessExpressionSyntax) => HoistReceiver(csTarget, pro),
            _ => csTarget
        };
    }

    /// <remarks>The container of an element access is already hoisted by the caller.</remarks>
    private CSSyntax.ExpressionSyntax HoistReceiver(CSSyntax.ExpressionSyntax memberExpression, IPropertyReferenceOperation pro)
    {
        if (memberExpression is not CSSyntax.MemberAccessExpressionSyntax maes || pro.Property.IsStatic || IsReusable(pro.Instance)) {
            return memberExpression;
        }
        return maes.WithExpression(HoistValue(maes.Expression));
    }

    private SeparatedSyntaxList<CSSyntax.ArgumentSyntax> HoistArguments(SeparatedSyntaxList<CSSyntax.ArgumentSyntax> csArgs, IPropertyReferenceOperation pro)
    {
        var parameters = pro.Property.Parameters;
        return CS.SyntaxFactory.SeparatedList(csArgs.Select((csArg, i) => {
            var parameter = csArg.NameColon is { } nameColon
                ? parameters.FirstOrDefault(p => string.Equals(p.Name, nameColon.Name.Identifier.ValueText, StringComparison.OrdinalIgnoreCase))
                : parameters.ElementAtOrDefault(Math.Min(i, parameters.Length - 1));
            var vbArg = pro.Arguments.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Parameter, parameter));
            return IsReusable(vbArg, i - parameters.IndexOf(parameter)) ? csArg : csArg.WithExpression(HoistValue(csArg.Expression));
        }));
    }

    private SeparatedSyntaxList<CSSyntax.ArgumentSyntax> HoistIndices(SeparatedSyntaxList<CSSyntax.ArgumentSyntax> csArgs, ImmutableArray<IOperation> indices)
    {
        return CS.SyntaxFactory.SeparatedList(csArgs.Select((csArg, i) =>
            IsReusable(indices.ElementAtOrDefault(i)) ? csArg : csArg.WithExpression(HoistValue(csArg.Expression))));
    }

    /// <param name="paramArrayIndex">For a ParamArray parameter, which of its elements the C# argument passes.</param>
    private bool IsReusable(IArgumentOperation vbArg, int paramArrayIndex) => vbArg switch {
        // The converter fills in an omitted argument with a constant or a temporary of its own
        { ArgumentKind: ArgumentKind.DefaultValue } => true,
        { ArgumentKind: ArgumentKind.ParamArray, Value: IArrayCreationOperation { Initializer.ElementValues: var elements } } =>
            paramArrayIndex >= 0 && paramArrayIndex < elements.Length && IsReusable(elements[paramArrayIndex]),
        { ArgumentKind: ArgumentKind.Explicit } => IsReusable(vbArg.Value),
        _ => false
    };

    private bool IsReusable(IOperation vbValue) => vbValue switch {
        null => true,
        { ConstantValue.HasValue: true } => true,
        IInstanceReferenceOperation => true,
        IParenthesizedOperation p => IsReusable(p.Operand),
        IConversionOperation { OperatorMethod: null } c => IsReusable(c.Operand),
        ILocalReferenceOperation l => !_writtenSymbols.Value.Contains(l.Local),
        IParameterReferenceOperation p => !_writtenSymbols.Value.Contains(p.Parameter),
        IFieldReferenceOperation f => !_writtenSymbols.Value.Contains(f.Field) && IsReusable(f.Instance),
        _ => false
    };

    private CSSyntax.ExpressionSyntax HoistValue(CSSyntax.ExpressionSyntax value) =>
        _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("tmp", value, ValidSyntaxFactory.VarType)).IdentifierName;

    /// <summary>
    /// The temporaries and write-back are placed around the enclosing expression, so that's what could change a variable in between.
    /// </summary>
    private HashSet<ISymbol> GetSymbolsWrittenInEnclosingExpression()
    {
        SyntaxNode enclosing = _vbTarget;
        while (enclosing.Parent is VBSyntax.ExpressionSyntax or VBSyntax.ArgumentSyntax or VBSyntax.ArgumentListSyntax) {
            enclosing = enclosing.Parent;
        }

        var written = _semanticModel.GetOperation(enclosing)?.DescendantsAndSelf().Select(o => o switch {
            IArgumentOperation { Parameter.RefKind: not RefKind.None } a => a.Value,
            IAssignmentOperation a => a.Target,
            _ => null
        }).Select(GetReferencedVariable).Where(s => s != null);
        return new HashSet<ISymbol>(written ?? Enumerable.Empty<ISymbol>(), SymbolEqualityComparer.Default);
    }

    private static ISymbol GetReferencedVariable(IOperation operation) => operation switch {
        IParenthesizedOperation p => GetReferencedVariable(p.Operand),
        IConversionOperation c => GetReferencedVariable(c.Operand),
        ILocalReferenceOperation l => l.Local,
        IParameterReferenceOperation p => p.Parameter,
        IFieldReferenceOperation f => f.Field,
        _ => null
    };
}
