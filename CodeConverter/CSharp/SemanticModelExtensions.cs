using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ICSharpCode.CodeConverter.CSharp;

internal static class SemanticModelExtensions
{
    /// <summary>
    /// This check is entirely to avoid some unnecessary default initializations so the code looks less cluttered and more like the VB did.
    /// The caller should default to outputting an initializer which is always safe for equivalence/correctness.
    /// </summary>
    public static bool IsDefinitelyAssignedBeforeRead(this SemanticModel semanticModel,
        ISymbol locallyDeclaredSymbol, VBSyntax.ModifiedIdentifierSyntax syntaxForSymbol)
    {
        var methodBlockBaseSyntax = syntaxForSymbol.GetAncestor<VBSyntax.MethodBlockBaseSyntax>();
        var methodFlow = semanticModel.AnalyzeDataFlow(methodBlockBaseSyntax.Statements.First(), methodBlockBaseSyntax.Statements.Last());
        return DefiniteAssignmentAnalyzer.IsDefinitelyAssignedBeforeRead(locallyDeclaredSymbol, methodFlow);
    }

    public static IOperation GetExpressionOperation(this SemanticModel semanticModel, Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax expressionSyntax)
    {
        var op = semanticModel.GetOperation(expressionSyntax);
        while (true) {
            switch (op) {
                case IArgumentOperation argumentOperation:
                    op = argumentOperation.Value;
                    continue;
                case IConversionOperation conversionOperation:
                    op = conversionOperation.Operand;
                    continue;
                case IParenthesizedOperation parenthesizedOperation:
                    op = parenthesizedOperation.Operand;
                    continue;
                default:
                    return op;
            }
        }
    }

    /// <summary>
    /// Returns true only if expressions static (i.e. doesn't reference the containing instance)
    /// </summary>
    public static bool IsDefinitelyStatic(this SemanticModel semanticModel, VBSyntax.ModifiedIdentifierSyntax vbName, VBSyntax.ExpressionSyntax vbInitValue)
    {
        var arrayBoundExpressions = vbName.ArrayBounds != null ? vbName.ArrayBounds.Arguments.Select(a => a.GetExpression()) : Enumerable.Empty<VBSyntax.ExpressionSyntax>();
        var expressions = vbInitValue.Yield().Concat(arrayBoundExpressions).Where(x => x != null).ToArray();
        return expressions.All(e => semanticModel.IsDefinitelyStatic(e));
    }

    /// <summary>
    /// Returns true only if expression is static (i.e. doesn't reference the containing instance)
    /// </summary>
    private static bool IsDefinitelyStatic(this SemanticModel semanticModel, VBSyntax.ExpressionSyntax e)
    {
        var instanceReferenceOperations = semanticModel.GetOperation(e).DescendantsAndSelf().OfType<IInstanceReferenceOperation>().ToArray();
        return !instanceReferenceOperations.Any(x => x.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance);
    }

    /// <returns>The ISymbol if available in this document, otherwise null</returns>
    /// <remarks>It's possible to use semanticModel.GetSpeculativeSymbolInfo(...) if you know (or can approximate) the position where the symbol would have been in the original document.</remarks>
    public static TSymbol GetSymbolInfoInDocument<TSymbol>(this SemanticModel semanticModel, SyntaxNode node) where TSymbol : class, ISymbol
    {
        return semanticModel.SyntaxTree == node.SyntaxTree ? semanticModel.GetSymbolInfo(node).ExtractBestMatch<TSymbol>() : null;
    }

    public static RefConversion GetRefConversionType(this SemanticModel semanticModel, VBSyntax.ArgumentSyntax node, VBSyntax.ArgumentListSyntax argList, ImmutableArray<IParameterSymbol> parameters, out string argName, out RefKind refKind)
    {
        var parameter = node.IsNamed && node is VBSyntax.SimpleArgumentSyntax sas
            ? parameters.FirstOrDefault(p => p.Name.Equals(sas.NameColonEquals.Name.Identifier.Text, StringComparison.OrdinalIgnoreCase))
            : parameters.ElementAtOrDefault(argList.Arguments.IndexOf(node));
        if (parameter != null) {
            refKind = parameter.RefKind;
            argName = parameter.Name;
        } else {
            refKind = RefKind.None;
            argName = null;
        }
        return semanticModel.NeedsVariableForArgument(node, refKind);
    }

    public static RefConversion NeedsVariableForArgument(this SemanticModel semanticModel, VBasic.Syntax.ArgumentSyntax node, RefKind refKind)
    {
        if (refKind == RefKind.None) return RefConversion.Inline;
        if (!(node is VBSyntax.SimpleArgumentSyntax sas) || sas is { Expression: VBSyntax.ParenthesizedExpressionSyntax }) return RefConversion.PreAssigment;
        var expression = sas.Expression;

        return GetRefConversion(expression);

        RefConversion GetRefConversion(VBSyntax.ExpressionSyntax expression)
        {
            var symbolInfo = semanticModel.GetSymbolInfoInDocument<ISymbol>(expression);
            if (symbolInfo is IPropertySymbol { ReturnsByRef: false, ReturnsByRefReadonly: false } propertySymbol) {
                // a property in VB.NET code can be ReturnsByRef if it's defined in a C# assembly the VB.NET code references
                return propertySymbol.IsReadOnly ? RefConversion.PreAssigment : RefConversion.PreAndPostAssignment;
            } else if (symbolInfo is IFieldSymbol { IsConst: true } or ILocalSymbol { IsConst: true }) {
                return RefConversion.PreAssigment;
            } else if (symbolInfo is IMethodSymbol { ReturnsByRef: false, ReturnsByRefReadonly: false }) {
                // a method in VB.NET code can be ReturnsByRef if it's defined in a C# assembly the VB.NET code references
                return RefConversion.PreAssigment;
            }

            if (DeclaredInUsing(symbolInfo)) return RefConversion.PreAssigment;

            if (expression is VBasic.Syntax.IdentifierNameSyntax || expression is VBSyntax.MemberAccessExpressionSyntax ||
                IsRefArrayAcces(expression)) {

                var typeInfo = semanticModel.GetTypeInfo(expression);
                bool isTypeMismatch = typeInfo.Type == null || !typeInfo.Type.Equals(typeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability);

                if (isTypeMismatch) {
                    return RefConversion.PreAndPostAssignment;
                }

                return RefConversion.Inline;
            }

            return RefConversion.PreAssigment;
        }

        bool IsRefArrayAcces(VBSyntax.ExpressionSyntax expression)
        {
            if (!(expression is VBSyntax.InvocationExpressionSyntax ies)) return false;
            var op = semanticModel.GetOperation(ies);
            return (op.IsArrayElementAccess() || IsReturnsByRefPropertyElementAccess(op))
                && GetRefConversion(ies.Expression) == RefConversion.Inline;

            static bool IsReturnsByRefPropertyElementAccess(IOperation op)
            {
                return op.IsPropertyElementAccess()
                 && op is IPropertyReferenceOperation { Property: { } prop }
                 && (prop.ReturnsByRef || prop.ReturnsByRefReadonly);
            }
        }
    }

    private static bool DeclaredInUsing(ISymbol symbolInfo)
    {
        return symbolInfo?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.Parent?.Parent?.IsKind(VBasic.SyntaxKind.UsingStatement) == true;
    }

    /// <summary>
    /// https://github.com/icsharpcode/CodeConverter/issues/324
    /// https://github.com/icsharpcode/CodeConverter/issues/310
    /// </summary>
    public enum RefConversion
    {
        /// <summary>
        /// e.g. Normal field, parameter or local
        /// </summary>
        Inline,
        /// <summary>
        /// Needs assignment before and/or after
        /// e.g. Method/Property result
        /// </summary>
        PreAssigment,
        /// <summary>
        /// Needs assignment before and/or after
        /// i.e. Property
        /// </summary>
        PreAndPostAssignment
    }
}