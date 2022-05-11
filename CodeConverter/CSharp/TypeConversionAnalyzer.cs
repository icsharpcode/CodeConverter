using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using CastExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax;
using Conversion = Microsoft.CodeAnalysis.VisualBasic.Conversion;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using IdentifierNameSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax;
using InvocationExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax;
using MemberAccessExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;

namespace ICSharpCode.CodeConverter.CSharp;

/// <remarks>See https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions </remarks>
internal class TypeConversionAnalyzer
{
    private readonly CSharpCompilation _csCompilation;
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _extraUsingDirectives;
    private readonly SyntaxGenerator _csSyntaxGenerator;
    private readonly ExpressionEvaluator _expressionEvaluator;
    private readonly VisualBasicNullableExpressionsConverter _vbNullableExpressionsConverter;

    private static readonly VBasic.SyntaxKind[] Int32ArithmeticExpressionKinds = {
        VBasic.SyntaxKind.IntegerDivideExpression,
        VBasic.SyntaxKind.ModuloExpression,
        VBasic.SyntaxKind.AddExpression,
        VBasic.SyntaxKind.SubtractExpression,
        VBasic.SyntaxKind.MultiplyExpression,
        VBasic.SyntaxKind.DivideExpression
    };

    public TypeConversionAnalyzer(SemanticModel semanticModel, CSharpCompilation csCompilation,
        HashSet<string> extraUsingDirectives, SyntaxGenerator csSyntaxGenerator, ExpressionEvaluator expressionEvaluator, 
        VisualBasicNullableExpressionsConverter vbNullableExpressionsConverter)
    {
        _semanticModel = semanticModel;
        _csCompilation = csCompilation;
        _extraUsingDirectives = extraUsingDirectives;
        _csSyntaxGenerator = csSyntaxGenerator;
        _expressionEvaluator = expressionEvaluator;
        _vbNullableExpressionsConverter = vbNullableExpressionsConverter;
    }

    public ExpressionSyntax AddExplicitConversion(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, bool addParenthesisIfNeeded = true, bool defaultToCast = false, bool isConst = false, ITypeSymbol forceSourceType = null, ITypeSymbol forceTargetType = null)
    {
        if (csNode == null) return null;
        var conversionKind = AnalyzeConversion(vbNode, defaultToCast, isConst, forceSourceType, forceTargetType);
        csNode = addParenthesisIfNeeded && conversionKind is TypeConversionKind.DestructiveCast or TypeConversionKind.NonDestructiveCast
            ? vbNode.ParenthesizeIfPrecedenceCouldChange(csNode)
            : csNode;
        return AddExplicitConversion(vbNode, csNode, conversionKind, addParenthesisIfNeeded, isConst, forceSourceType: forceSourceType, forceTargetType: forceTargetType).Expr;
    }

    public (ExpressionSyntax Expr, bool IsConst) AddExplicitConversion(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeConversionKind conversionKind, bool addParenthesisIfNeeded = false, bool requiresConst = false, ITypeSymbol forceSourceType = null, ITypeSymbol forceTargetType = null)
    {
        var (vbType, vbConvertedType) = GetTypeInfo(vbNode, forceSourceType, forceTargetType);
        bool resultConst = false;

        if (requiresConst) {
            var (constExpression, isCorrectType) = _expressionEvaluator.GetConstantOrNull(vbNode, vbConvertedType, conversionKind, csNode);
            if (isCorrectType) {
                return (constExpression, true);
            }
            if (constExpression != null) {
                csNode = constExpression ?? csNode;
                resultConst = true;
            }
        }

        var typeConvertedResult = AddTypeConversion(vbNode, csNode, conversionKind, addParenthesisIfNeeded, vbType, vbConvertedType);
        return (typeConvertedResult, resultConst);
    }

    private ExpressionSyntax AddTypeConversion(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, TypeConversionKind conversionKind, bool addParenthesisIfNeeded, ITypeSymbol vbType, ITypeSymbol vbConvertedType)
    {
        switch (conversionKind) {
            case TypeConversionKind.FractionalNumberRoundThenCast:
                csNode = vbType.IsNullable() && vbConvertedType.IsNullable() 
                    ? _vbNullableExpressionsConverter.InvokeConversionWhenNotNull(csNode, GetMathRoundMemberAccess(), GetTypeSyntax(vbConvertedType)) 
                    : AddRoundInvocation(vbType.IsNullable() ? csNode.NullableGetValueExpression() : csNode);

                return AddTypeConversion(vbNode, csNode, TypeConversionKind.NonDestructiveCast, addParenthesisIfNeeded, vbType, vbConvertedType);
            case TypeConversionKind.EnumConversionThenCast:
                vbConvertedType.IsNullable(out var convertedNullableType);
                var underlyingEnumType = ((INamedTypeSymbol)(convertedNullableType ?? vbConvertedType)).EnumUnderlyingType;
                csNode = vbType.IsNullable() && convertedNullableType != null
                    ? _vbNullableExpressionsConverter.InvokeConversionWhenNotNull(csNode, GetConversionsMemberAccess(underlyingEnumType), GetTypeSyntax(vbConvertedType))
                    : AddTypeConversion(vbNode, csNode, TypeConversionKind.Conversion, addParenthesisIfNeeded, vbType, underlyingEnumType);

                return AddTypeConversion(vbNode, csNode, TypeConversionKind.NonDestructiveCast, addParenthesisIfNeeded, vbType, vbConvertedType);
            case TypeConversionKind.EnumCastThenConversion:
                var enumUnderlyingType = ((INamedTypeSymbol) vbType).EnumUnderlyingType;
                csNode = AddTypeConversion(vbNode, csNode, TypeConversionKind.NonDestructiveCast, addParenthesisIfNeeded, vbType, enumUnderlyingType);
                return AddTypeConversion(vbNode, csNode, TypeConversionKind.Conversion, addParenthesisIfNeeded, enumUnderlyingType, vbConvertedType);
            case TypeConversionKind.Unknown:
            case TypeConversionKind.Identity:
                return addParenthesisIfNeeded ? vbNode.ParenthesizeIfPrecedenceCouldChange(csNode) : csNode;
            case TypeConversionKind.DestructiveCast:
            case TypeConversionKind.NonDestructiveCast:
                return CreateCast(csNode, vbConvertedType);
            case TypeConversionKind.Conversion:
                return AddExplicitConvertTo(vbNode, csNode, vbType, vbConvertedType);
            case TypeConversionKind.NullableBool:
                return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, csNode,
                    LiteralConversions.GetLiteralExpression(true));
            case TypeConversionKind.StringToCharArray:
                var memberAccessExpressionSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, csNode, SyntaxFactory.IdentifierName(nameof(string.ToCharArray)));
                return SyntaxFactory.InvocationExpression(memberAccessExpressionSyntax,
                    SyntaxFactory.ArgumentList());
            case TypeConversionKind.DelegateConstructor:
                return SyntaxFactory.ObjectCreationExpression(GetCommonDelegateTypeOrNull(vbNode, vbConvertedType)).WithArgumentList(new[]{csNode}.CreateCsArgList());
            default:
                throw new ArgumentOutOfRangeException(nameof(conversionKind), conversionKind, null);
        }
    }

    private TypeSyntax GetTypeSyntax(ITypeSymbol type) => (TypeSyntax)_csSyntaxGenerator.TypeExpression(type);

    private ExpressionSyntax CreateCast(ExpressionSyntax csNode, ITypeSymbol vbConvertedType)
    {
        var typeName = GetTypeSyntax(vbConvertedType);
        if (csNode is CastExpressionSyntax cast && cast.Type.IsEquivalentTo(typeName)) {
            return csNode;
        }

        return ValidSyntaxFactory.CastExpression(typeName, csNode);
    }

    public TypeConversionKind AnalyzeConversion(VBSyntax.ExpressionSyntax vbNode, bool alwaysExplicit = false, bool isConst = false, ITypeSymbol forceSourceType = null, ITypeSymbol forceTargetType = null)
    {
        var (vbType, vbConvertedType) = GetTypeInfo(vbNode, forceSourceType, forceTargetType);
            
        if (vbConvertedType is null)
        {
            return TypeConversionKind.Unknown;
        }

        if (vbType is null) {
            return GetCommonDelegateTypeOrNull(vbNode, vbConvertedType) is {} ? TypeConversionKind.DelegateConstructor : TypeConversionKind.Unknown;
        }

        if (vbType.IsEnumType()) {
            if (vbConvertedType.IsNumericType()) {
                return TypeConversionKind.NonDestructiveCast;
            }

            if (vbType.Equals(vbConvertedType, SymbolEqualityComparer.IncludeNullability) ||
                (vbConvertedType.IsNullable() && vbType.Equals(vbConvertedType.GetNullableUnderlyingType(), SymbolEqualityComparer.IncludeNullability)) ||
                vbConvertedType.SpecialType == SpecialType.System_Object) {
                return TypeConversionKind.Identity;
            }

            if (vbConvertedType.SpecialType == SpecialType.System_String) {
                return TypeConversionKind.EnumCastThenConversion;
            }
            return TypeConversionKind.Conversion;
        }

        vbType.IsNullable(out var underlyingType);
        var nullableVbType = underlyingType ?? vbType;
        if (vbConvertedType.IsEnumType() && !(nullableVbType.IsIntegralOrEnumType() || nullableVbType.IsFractionalNumericType())) {
            return TypeConversionKind.EnumConversionThenCast;
        }

        var vbCompilation = (VBasic.VisualBasicCompilation) _semanticModel.Compilation;
        var vbConversion = vbCompilation.ClassifyConversion(vbType, vbConvertedType);
        var csType = GetCSType(vbType, vbNode);
        var csConvertedType = GetCSType(vbConvertedType);

        if (csType != null && csConvertedType != null &&
            TryAnalyzeCsConversion(vbNode, csType, csConvertedType, vbConversion, vbConvertedType, vbType, isConst, forceSourceType != null, out TypeConversionKind analyzeConversion)) {
            return analyzeConversion;
        }

        return AnalyzeVbConversion(alwaysExplicit, vbType, vbConvertedType, vbConversion);
    }

    /// <summary>
    /// The VB operators are different to C#, e.g. VB: short * short = short, C#: short * short = int
    /// https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/operators-and-expressions/arithmetic-operators
    /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/arithmetic-operators#:~:text=The%20following%20operators%20perform%20arithmetic,%2C%20and%20%2D%20(subtraction)%20operators
    /// </summary>
    private (ITypeSymbol Type, ITypeSymbol ConvertedType) GetTypeInfo(VBSyntax.ExpressionSyntax vbNode,
        ITypeSymbol type = null, ITypeSymbol convertedType = null)
    {
        var typeInfo = _semanticModel.GetTypeInfo(vbNode);
        type ??= typeInfo.Type;
        convertedType ??= typeInfo.ConvertedType;
        if (type.IsNumericType()) {
            VBasic.VisualBasicExtensions.Kind(vbNode.SkipIntoParens());
            type = GetTypeOrNull(vbNode, type.SpecialType) ?? type;
        }
        return (type, convertedType);
    }

    private ITypeSymbol GetTypeOrNull(VBSyntax.ExpressionSyntax vbNode, SpecialType type)
    {
        var syntaxKind = VBasic.VisualBasicExtensions.Kind(vbNode.SkipIntoParens());
        if (Int32ArithmeticExpressionKinds.Contains(syntaxKind))
        {
            var mappedSpecialType = type switch {
                SpecialType.System_SByte => SpecialType.System_Int32,
                SpecialType.System_Byte => SpecialType.System_Int32,
                SpecialType.System_Int16 => SpecialType.System_Int32,
                SpecialType.System_UInt16 => SpecialType.System_Int32,
                var other => other
            };
            return _semanticModel.Compilation.GetSpecialType(mappedSpecialType);
        }

        return null;
    }

    private CSSyntax.NameSyntax GetCommonDelegateTypeOrNull(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol vbConvertedType)
    {
        var parentExceptParentheses = vbNode.Parent is VBSyntax.ExpressionSyntax parentExp ? parentExp.SkipOutOfParens() : vbNode.Parent;
        if (vbConvertedType.Name != nameof(Delegate) &&
            (parentExceptParentheses is VBSyntax.SimpleArgumentSyntax || parentExceptParentheses is VBSyntax.EqualsValueSyntax)) {
            return null;
        }

        if (vbNode.SkipIntoParens() is VBSyntax.LambdaExpressionSyntax vbLambda &&
            (vbConvertedType.TypeKind == TypeKind.Delegate || vbConvertedType.Name == nameof(Delegate) || vbConvertedType.SpecialType == SpecialType.System_Object) &&
            _semanticModel.GetSymbolInfo(vbLambda).Symbol is IMethodSymbol lambdaSymbol)
        {
            return CreateCommonDelegateTypeSyntax(lambdaSymbol);
        }

        return null;
    }

    private CSSyntax.NameSyntax CreateCommonDelegateTypeSyntax(IMethodSymbol vbLambda)
    {
        var parameters = vbLambda.Parameters
            .Select(p => _csSyntaxGenerator.TypeExpression(p.Type));
            
        if (vbLambda.ReturnType.IsSystemVoid()) {
            return CreateType("Action", parameters);
        }

        var typeExpression = _csSyntaxGenerator.TypeExpression(vbLambda.ReturnType);
        return CreateType("Func", parameters.Concat(typeExpression));
    }

    private static CSSyntax.NameSyntax CreateType(string baseTypeName, IEnumerable<SyntaxNode> parameters)
    {
        var parameterList = SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(parameters));
        if (!parameterList.Arguments.Any()) return SyntaxFactory.IdentifierName(baseTypeName);
        return SyntaxFactory.GenericName(SyntaxFactory.Identifier(baseTypeName), parameterList);
    }

    private ITypeSymbol GetCSType(ITypeSymbol vbType, VBSyntax.ExpressionSyntax vbNode = null)
    {
        // C# does not have literals for short/ushort, so the actual type here is integer
        if (vbNode is VBSyntax.LiteralExpressionSyntax literal &&
            literal.IsKind(VBasic.SyntaxKind.NumericLiteralExpression) &&
            literal.Token.Text.EndsWith("S", StringComparison.InvariantCulture)) {
            return _csCompilation.GetSpecialType(SpecialType.System_Int32);
        }

        var csType = SymbolFinder.FindSimilarSymbols(vbType, _csCompilation).FirstOrDefault() ?? _csCompilation.GetTypeByMetadataName(vbType.GetFullMetadataName());

        return csType;
    }

    private bool TryAnalyzeCsConversion(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol csType,
        ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, bool isConst, bool sourceForced,
        out TypeConversionKind typeConversionKind)
    {
        var csConversion = _csCompilation.ClassifyConversion(csType, csConvertedType);
        vbType.IsNullable(out var underlyingType);
        vbConvertedType.IsNullable(out var underlyingConvertedType);
        var nullableVbType = underlyingType ?? vbType;
        var nullableVbConvertedType = underlyingConvertedType ?? vbConvertedType;

        bool isConvertToString =
            (vbConversion.IsString || vbConversion.IsReference && vbConversion.IsNarrowing) && vbConvertedType.SpecialType == SpecialType.System_String;
        bool isConvertFractionalToInt =
            !csConversion.IsImplicit && nullableVbType.IsFractionalNumericType() && nullableVbConvertedType.IsIntegralOrEnumType();

        if (!csConversion.Exists || csConversion.IsUnboxing) {
            if (ConvertStringToCharLiteral(vbNode, vbConvertedType, out _)) {
                typeConversionKind =
                    TypeConversionKind.Identity; // Already handled elsewhere by other usage of method
                return true;
            }

            if (vbType.SpecialType == SpecialType.System_String && vbConvertedType.IsArrayOf(SpecialType.System_Char)) {
                typeConversionKind = TypeConversionKind.StringToCharArray;
                return true;
            }
            if (isConvertToString || vbConversion.IsNarrowing) {
                typeConversionKind = nullableVbConvertedType.IsEnumType() && !csConversion.Exists
                    ? TypeConversionKind.EnumConversionThenCast
                    : TypeConversionKind.Conversion;
                return true;
            }
        } else if (vbConversion.IsNarrowing && vbConversion.IsNullableValueType && isConvertFractionalToInt) {
            typeConversionKind = TypeConversionKind.FractionalNumberRoundThenCast;
            return true;
        } else if (vbConversion.IsNumeric && (csConversion.IsNumeric || nullableVbConvertedType.IsEnumType()) && isConvertFractionalToInt) {
            typeConversionKind = TypeConversionKind.FractionalNumberRoundThenCast;
            return true;
        } else if (csConversion.IsExplicit && csConversion.IsEnumeration || csConversion.IsBoxing) {
            typeConversionKind = TypeConversionKind.NonDestructiveCast;
            return true;
        } else if (vbConversion.IsNumeric && csConversion.IsNumeric) {
            // For widening, implicit, a cast is really only needed to help resolve the overload for the operator/method used.
            // e.g. When VB "&" changes to C# "+", there are lots more overloads available that implicit casts could match.
            // e.g. sbyte * ulong uses the decimal * operator in VB. In C# it's ambiguous - see ExpressionTests.vb "TestMul".
            typeConversionKind =
                isConst && IsImplicitConstantConversion(vbNode) || csConversion.IsIdentity || !sourceForced && IsExactTypeNumericLiteral(vbNode, vbConvertedType) ? TypeConversionKind.Identity :
                csConversion.IsImplicit || vbType.IsNumericType() ? TypeConversionKind.NonDestructiveCast
                : TypeConversionKind.Conversion;
            return true;
        } else if (isConvertToString && vbType.SpecialType == SpecialType.System_Object) {
            typeConversionKind = TypeConversionKind.Conversion;
            return true;
        } 
        else if (csConversion.IsNullable && csConvertedType.SpecialType == SpecialType.System_Boolean && vbNode.AlwaysHasBooleanTypeInCSharp() &&
                 (vbNode is not VBSyntax.BinaryExpressionSyntax and not VBSyntax.UnaryExpressionSyntax || vbNode.IsKind(VBasic.SyntaxKind.AndExpression, VBasic.SyntaxKind.OrExpression, VBasic.SyntaxKind.ExclusiveOrExpression))) {
            typeConversionKind = TypeConversionKind.NullableBool;
            return true;
        }
        else if (csConversion.IsExplicit) {
            typeConversionKind = TypeConversionKind.DestructiveCast;
            return true;
        }

        typeConversionKind = csConversion.IsIdentity ? TypeConversionKind.Identity : TypeConversionKind.Unknown;
        return false;
    }

    private static bool IsExactTypeNumericLiteral(VBSyntax.ExpressionSyntax vbNode, ITypeSymbol vbConvertedType) => 
        vbNode is VBSyntax.LiteralExpressionSyntax literal &&
        LiteralConversions.ConvertLiteralNumericValueOrNull(literal.Token.Value, vbConvertedType) is {};

    private bool IsImplicitConstantConversion(VBSyntax.ExpressionSyntax vbNode)
    {
        return _semanticModel.GetOperation(vbNode).Parent is IConversionOperation co && co.IsImplicit && co.Operand.ConstantValue.HasValue;
    }

    private static TypeConversionKind AnalyzeVbConversion(bool alwaysExplicit, ITypeSymbol vbType,
        ITypeSymbol vbConvertedType, Conversion vbConversion)
    {
        if (vbType.Equals(vbConvertedType, SymbolEqualityComparer.IncludeNullability) || vbConversion.IsIdentity) {
            return TypeConversionKind.Identity;
        }

        if (vbConversion.IsNumeric && (vbType.IsEnumType() || vbConvertedType.IsEnumType())) {
            return TypeConversionKind.NonDestructiveCast;
        }

        if (vbConversion.IsNarrowing && vbConversion.IsString && vbConversion.IsKind(VbConversionKind.InvolvesEnumTypeConversions)) {
            return TypeConversionKind.EnumConversionThenCast;
        }
        if (vbConversion.IsNarrowing) {
            return TypeConversionKind.DestructiveCast;
        }
        if (alwaysExplicit) {
            return TypeConversionKind.NonDestructiveCast;
        }

        return TypeConversionKind.Unknown;
    }

    private MemberAccessExpressionSyntax GetMathRoundMemberAccess()
    {
        _extraUsingDirectives.Add("System");
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Math"),  SyntaxFactory.IdentifierName("Round"));
    }

    private MemberAccessExpressionSyntax GetConversionsMemberAccess(ITypeSymbol type)
    {
        if (!ExpressionEvaluator.ConversionsTypeFullNames.TryGetValue(type.GetFullMetadataName(), out var methodId)) {
            throw new ArgumentException($"Unable to find conversion method for type {type}", nameof(type));
        }

        return GetConversionsMemberAccess(methodId.Name);
    }

    private MemberAccessExpressionSyntax GetConversionsMemberAccess(string methodId)
    {
        _extraUsingDirectives.Add("Microsoft.VisualBasic.CompilerServices");
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Conversions"), SyntaxFactory.IdentifierName(methodId));
    }

    private ExpressionSyntax AddRoundInvocation(ExpressionSyntax csNode)
    {
        var memberAccess = GetMathRoundMemberAccess();
        var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(csNode)));
        return SyntaxFactory.InvocationExpression(memberAccess, arguments);
    }

    private ExpressionSyntax AddExplicitConvertTo(VBSyntax.ExpressionSyntax vbNode, ExpressionSyntax csNode, ITypeSymbol currentType, ITypeSymbol targetType)
    {
        var displayType = targetType.ToMinimalDisplayString(_semanticModel, vbNode.SpanStart);
        if (csNode is InvocationExpressionSyntax invoke &&
            invoke.Expression is MemberAccessExpressionSyntax expr &&
            expr.Expression is IdentifierNameSyntax name && name.Identifier.ValueText == "Conversions" &&
            expr.Name.Identifier.ValueText == $"To{displayType}") {
            return csNode;
        }

        if (GetToStringConversionOrNull(csNode, currentType, targetType) is { } csNodeToString) {
            return csNodeToString;
        }

        currentType.IsNullable(out var nullableCurrentType);
        targetType.IsNullable(out var nullableTargetType);
        if (nullableCurrentType != null && nullableTargetType == null) {
            csNode = csNode.NullableGetValueExpression();
        }

        var typeNameForConversionMethod = nullableTargetType ?? targetType;
        if (!ExpressionEvaluator.ConversionsTypeFullNames.TryGetValue(typeNameForConversionMethod.GetFullMetadataName(), out var methodId)) {
            return CreateCast(csNode, targetType);
        }

        // Need to use Conversions rather than Convert to match what VB does, eg. Conversions.ToInteger(True) -> -1
        var memberAccess = GetConversionsMemberAccess(methodId.Name);

        if (nullableCurrentType != null && nullableTargetType != null) {
            return _vbNullableExpressionsConverter.InvokeConversionWhenNotNull(csNode, memberAccess, GetTypeSyntax(targetType));
        }

        var arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(csNode)));
        return SyntaxFactory.InvocationExpression(memberAccess, arguments);
    }

    /// <summary>
    /// For many types, Conversions.ToString is the same as ToString.
    /// I've done some checks on numeric types, could add more here in future. Any reference types will need a conditional to avoid nullref like Conversions does
    /// </summary>
    private static ExpressionSyntax GetToStringConversionOrNull(ExpressionSyntax csNode, ITypeSymbol currentType, ITypeSymbol targetType)
    {
        if (targetType.SpecialType != SpecialType.System_String) return null;

        const string toStringMethodName = "ToString";
        if (csNode is MemberAccessExpressionSyntax maes && maes.Name.Identifier.Text == toStringMethodName) {
            return csNode;
        }

        if (currentType.IsNumericType()) {
            var toString = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                csNode.AddParens(), SyntaxFactory.IdentifierName(toStringMethodName));
            return SyntaxFactory.InvocationExpression(toString, SyntaxFactory.ArgumentList());
        }
        return null;
    }

    public enum TypeConversionKind
    {
        Unknown,
        Identity,
        DestructiveCast,
        NonDestructiveCast,
        Conversion,
        EnumConversionThenCast,
        EnumCastThenConversion,
        NullableBool,
        StringToCharArray,
        DelegateConstructor,
        FractionalNumberRoundThenCast
    }

    public static bool ConvertStringToCharLiteral(VBSyntax.ExpressionSyntax node,
        ITypeSymbol convertedType,
        out char chr)
    {

        var preferChar = node.Parent is VBSyntax.PredefinedCastExpressionSyntax pces &&
                         pces.Keyword.IsKind(VBasic.SyntaxKind.CCharKeyword)
                         || convertedType?.SpecialType == SpecialType.System_Char;
        if (preferChar && node.SkipIntoParens() is VBSyntax.LiteralExpressionSyntax les &&
            les.Token.Value is string str &&
            str.Length == 1) {
            chr = str.Single();
            return true;
        }

        chr = default;
        return false;
    }

    public static SyntaxKind? GetNonCompoundOrNull(SyntaxKind kind) =>
        kind switch {
            SyntaxKind.DivideAssignmentExpression=> SyntaxKind.DivideExpression,
            SyntaxKind.MultiplyAssignmentExpression => SyntaxKind.MultiplyExpression,
            SyntaxKind.AddAssignmentExpression => SyntaxKind.AddExpression,
            SyntaxKind.SubtractAssignmentExpression=> SyntaxKind.SubtractExpression,
            SyntaxKind.ModuloAssignmentExpression=> SyntaxKind.ModuloExpression,
            SyntaxKind.LeftShiftAssignmentExpression=> SyntaxKind.LeftShiftExpression,
            SyntaxKind.RightShiftAssignmentExpression=> SyntaxKind.RightShiftExpression,
            _ => null
        };
}