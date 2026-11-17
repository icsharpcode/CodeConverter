using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp.ByRefArguments;

/// <summary>
/// A ByRef argument that VB copies back is used twice: read before the call, and written after it.
/// VB evaluates the target's receiver and index arguments once, and the write reuses their values
/// (Roslyn's UseTwiceRewriter.vb, called from RewriteByRefArgumentWithCopyBack in LocalRewriter_Call.vb).
/// The C# read and write-back are separate expressions, so a part that could have a different value the second time is hoisted into a temporary.
/// </summary>
/// <remarks>
/// VB copies every part except constants and Me. A (non-Static) local or ByVal parameter gives the same value again unless it's written while the
/// enclosing expression runs, so it's only copied when that expression, or a lambda the call could run, writes it.
/// A field is also copied when the call could change it (<see cref="IFieldMutationOracle"/>).
/// An intrinsic operator on such parts gives the same value again too.
/// </remarks>
internal class ByRefTargetTemporaries
{
    private readonly SemanticModel _semanticModel;
    private readonly ITypeContext _typeContext;
    private readonly IFieldMutationOracle _fieldMutations;
    private readonly VBSyntax.ExpressionSyntax _vbTarget;
    private readonly Lazy<HashSet<ISymbol>> _writtenSymbols;

    private ByRefTargetTemporaries(SemanticModel semanticModel, ITypeContext typeContext, IFieldMutationOracle fieldMutations, VBSyntax.ExpressionSyntax vbTarget)
    {
        _semanticModel = semanticModel;
        _typeContext = typeContext;
        _fieldMutations = fieldMutations;
        _vbTarget = vbTarget;
        _writtenSymbols = new Lazy<HashSet<ISymbol>>(GetSymbolsWrittenInEnclosingExpression);
    }

    /// <returns>The C# target with each part VB evaluates once replaced by a hoisted temporary, where its value could otherwise differ.</returns>
    public static CSSyntax.ExpressionSyntax Hoist(SemanticModel semanticModel, ITypeContext typeContext, IFieldMutationOracle fieldMutations,
        VBSyntax.ExpressionSyntax vbTarget, CSSyntax.ExpressionSyntax csTarget) =>
        new ByRefTargetTemporaries(semanticModel, typeContext, fieldMutations, vbTarget).Hoist(csTarget);

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
        if (memberExpression is not CSSyntax.MemberAccessExpressionSyntax maes || pro.Property.IsStatic) {
            return memberExpression;
        }
        return maes.WithExpression(HoistInstance(pro.Instance, maes.Expression));
    }

    /// <summary>
    /// A reference can be copied, but writing a structure's property through a copy would lose the write.
    /// So, like VB, a structure variable stays in place and only the parts that locate it are copied.
    /// </summary>
    private CSSyntax.ExpressionSyntax HoistInstance(IOperation instance, CSSyntax.ExpressionSyntax csInstance)
    {
        if (IsReusable(instance)) return csInstance;
        if (instance.Type?.IsReferenceType == true) return HoistValue(csInstance);
        return (instance, csInstance) switch {
            (ILocalReferenceOperation or IParameterReferenceOperation, _) => csInstance,
            (IFieldReferenceOperation { Instance: { } fieldInstance }, CSSyntax.MemberAccessExpressionSyntax maes) =>
                maes.WithExpression(HoistInstance(fieldInstance, maes.Expression)),
            (IFieldReferenceOperation, _) => csInstance,
            (IArrayElementReferenceOperation element, CSSyntax.ElementAccessExpressionSyntax elementAccess) =>
                elementAccess.WithExpression(HoistInstance(element.ArrayReference, elementAccess.Expression))
                    .WithArgumentList(elementAccess.ArgumentList.WithArguments(HoistIndices(elementAccess.ArgumentList.Arguments, element.Indices))),
            _ => HoistValue(csInstance)
        };
    }

    private SeparatedSyntaxList<CSSyntax.ArgumentSyntax> HoistArguments(SeparatedSyntaxList<CSSyntax.ArgumentSyntax> csArgs, IPropertyReferenceOperation pro)
    {
        var parameters = pro.Property.Parameters;
        return CS.SyntaxFactory.SeparatedList(csArgs.Select((csArg, i) => {
            var parameter = csArg.NameColon is { } nameColon
                ? parameters.FirstOrDefault(p => string.Equals(p.Name, nameColon.Name.Identifier.ValueText, StringComparison.OrdinalIgnoreCase))
                : parameters.ElementAtOrDefault(i);
            var vbArg = pro.Arguments.FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.Parameter, parameter));
            return IsReusable(vbArg) ? csArg : csArg.WithExpression(HoistValue(csArg.Expression));
        }));
    }

    private SeparatedSyntaxList<CSSyntax.ArgumentSyntax> HoistIndices(SeparatedSyntaxList<CSSyntax.ArgumentSyntax> csArgs, ImmutableArray<IOperation> indices)
    {
        return CS.SyntaxFactory.SeparatedList(csArgs.Select((csArg, i) =>
            IsReusable(indices.ElementAtOrDefault(i)) ? csArg : csArg.WithExpression(HoistValue(csArg.Expression))));
    }

    /// <remarks>A ParamArray's elements don't map one to one onto C# arguments, so they're each copied.</remarks>
    private bool IsReusable(IArgumentOperation vbArg) => vbArg switch {
        // The converter fills in an omitted argument with a constant or a temporary of its own
        { ArgumentKind: ArgumentKind.DefaultValue } => true,
        { ArgumentKind: ArgumentKind.Explicit } => IsReusable(vbArg.Value),
        _ => false
    };

    private bool IsReusable(IOperation vbValue) => vbValue switch {
        null => true,
        { ConstantValue.HasValue: true } => true,
        IInstanceReferenceOperation => true,
        IParenthesizedOperation p => IsReusable(p.Operand),
        IConversionOperation { OperatorMethod: null } c when IsRepeatable(c) => IsReusable(c.Operand),
        IUnaryOperation { OperatorMethod: null } u when IsCultureFree(u.Type) && IsCultureFree(u.Operand.Type) => IsReusable(u.Operand),
        IBinaryOperation { OperatorMethod: null } b when IsCultureFree(b.Type) && IsCultureFree(b.LeftOperand.Type) && IsCultureFree(b.RightOperand.Type) =>
            IsReusable(b.LeftOperand) && IsReusable(b.RightOperand),
        // A Static local lives as long as its method's object (or type), so another call of the method can change it
        ILocalReferenceOperation l => !l.Local.IsStatic && !_writtenSymbols.Value.Contains(l.Local),
        // A ByRef parameter aliases a variable of the caller's, which the call could reach some other way
        IParameterReferenceOperation p => p.Parameter.RefKind == RefKind.None && !_writtenSymbols.Value.Contains(p.Parameter),
        IFieldReferenceOperation f => !_writtenSymbols.Value.Contains(f.Field) && !_fieldMutations.MayChangeDuringCall(f.Field, GetCall()) && IsReusable(f.Instance),
        _ => false
    };

    /// <returns>The call (or object creation, or event raise) that the ByRef target is an argument of.</returns>
    private IOperation GetCall() => _semanticModel.GetOperation(_vbTarget)?.Parent is IArgumentOperation { Parent: var call } ? call : null;

    /// <summary>
    /// An intrinsic operator or conversion between these types gives the same result (or exception) from the same operands.
    /// One involving String or Date can read the current culture, which the call could change, and one on Object is late bound, which can run user code.
    /// </summary>
    private static bool IsCultureFree(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable) type = nullable.TypeArguments[0];
        return type is { TypeKind: TypeKind.Enum } or { SpecialType: SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte
            or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
            or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal };
    }

    /// <summary>
    /// Besides conversions between culture-free types, a widening reference or boxing conversion only changes the static type.
    /// </summary>
    private static bool IsRepeatable(IConversionOperation conversion) =>
        conversion.Conversion.IsIdentity ||
        conversion.Conversion.IsImplicit && (conversion.Conversion.IsReference || conversion.Operand.Type is { IsValueType: true } && conversion.Type is { IsReferenceType: true }) ||
        IsCultureFree(conversion.Operand.Type) && IsCultureFree(conversion.Type);

    private CSSyntax.ExpressionSyntax HoistValue(CSSyntax.ExpressionSyntax value) =>
        _typeContext.PerScopeState.Hoist(new AdditionalDeclaration("tmp", value, ValidSyntaxFactory.VarType)).IdentifierName;

    /// <summary>
    /// The temporaries and write-back are placed around the enclosing expression, so that's what could change a local variable in between.
    /// So could any lambda in the same method, since the call could run it.
    /// </summary>
    private HashSet<ISymbol> GetSymbolsWrittenInEnclosingExpression()
    {
        SyntaxNode enclosing = _vbTarget;
        while (enclosing.Parent is VBSyntax.ExpressionSyntax or VBSyntax.ArgumentSyntax or VBSyntax.ArgumentListSyntax) {
            enclosing = enclosing.Parent;
        }
        var lambdasInMember = _vbTarget.Ancestors().LastOrDefault(a => a is VBSyntax.MethodBlockBaseSyntax)?.DescendantNodes().OfType<VBSyntax.LambdaExpressionSyntax>()
                              ?? Enumerable.Empty<VBSyntax.LambdaExpressionSyntax>();

        var written = lambdasInMember.Prepend<SyntaxNode>(enclosing)
            .SelectMany(GetVariablesWrittenIn);
        return new HashSet<ISymbol>(written, SymbolEqualityComparer.Default);
    }

    private IEnumerable<ISymbol> GetVariablesWrittenIn(SyntaxNode node) =>
        _semanticModel.GetOperation(node)?.DescendantsAndSelf()
            .Select(o => (Operation: o, Variable: GetReferencedVariable(o)))
            .Where(r => r.Variable != null && VariableWrites.IsWrite(r.Operation))
            .Select(r => r.Variable) ?? Enumerable.Empty<ISymbol>();

    private static ISymbol GetReferencedVariable(IOperation operation) => operation switch {
        ILocalReferenceOperation l => l.Local,
        IParameterReferenceOperation p => p.Parameter,
        IFieldReferenceOperation f => f.Field,
        _ => null
    };
}
