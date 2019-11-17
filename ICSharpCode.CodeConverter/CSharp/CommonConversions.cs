using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ArgumentListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayRankSpecifierSyntax;
using ArrayTypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayTypeSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using ISymbolExtensions = ICSharpCode.CodeConverter.Util.ISymbolExtensions;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;
using VariableDeclaratorSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using ITypeSymbol = Microsoft.CodeAnalysis.ITypeSymbol;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommonConversions
    {
        private static readonly Type ExtensionAttributeType = typeof(ExtensionAttribute);
        private static readonly Type OutAttributeType = typeof(OutAttribute);
        public Document Document { get; }
        private readonly SemanticModel _semanticModel;
        public SyntaxGenerator CsSyntaxGenerator { get; }
        private readonly CSharpCompilation _csCompilation;
        public CommentConvertingVisitorWrapper<CSharpSyntaxNode> TriviaConvertingExpressionVisitor { get; set; }
        public TypeConversionAnalyzer TypeConversionAnalyzer { get; }

        public CommonConversions(Document document, SemanticModel semanticModel,
            TypeConversionAnalyzer typeConversionAnalyzer, SyntaxGenerator csSyntaxGenerator,
            CSharpCompilation csCompilation)
        {
            TypeConversionAnalyzer = typeConversionAnalyzer;
            Document = document;
            _semanticModel = semanticModel;
            CsSyntaxGenerator = csSyntaxGenerator;
            _csCompilation = csCompilation;
        }

        public async Task<(IReadOnlyCollection<VariableDeclarationSyntax> Variables, IReadOnlyCollection<CSharpSyntaxNode> Methods)> SplitVariableDeclarations(
            VariableDeclaratorSyntax declarator, bool preferExplicitType = false)
        {
            var vbInitValue = GetInitializerToConvert(declarator);
            var initializerOrMethodDecl = await vbInitValue.AcceptAsync(TriviaConvertingExpressionVisitor);
            var vbInitializerType = vbInitValue != null ? _semanticModel.GetTypeInfo(vbInitValue).Type : null;

            bool requireExplicitTypeForAll = declarator.Names.Count > 1;
            IMethodSymbol initSymbol = null;
            if (vbInitValue != null) {
                var vbInitConstantValue = _semanticModel.GetConstantValue(vbInitValue);
                var vbInitIsNothingLiteral = vbInitConstantValue.HasValue && vbInitConstantValue.Value == null;
                preferExplicitType |= vbInitializerType != null && vbInitializerType.HasCsKeyword();
                initSymbol = _semanticModel.GetSymbolInfo(vbInitValue).Symbol as IMethodSymbol;
                bool isAnonymousFunction = initSymbol?.IsAnonymousFunction() == true;
                requireExplicitTypeForAll |= vbInitIsNothingLiteral || isAnonymousFunction;
            }

            var csVars = new Dictionary<string, VariableDeclarationSyntax>();
            var csMethods = new List<CSharpSyntaxNode>();

            foreach (var name in declarator.Names) {

                var declaredSymbol = _semanticModel.GetDeclaredSymbol(name);
                var declaredSymbolType = declaredSymbol.GetSymbolType();
                var equalsValueClauseSyntax = await ConvertEqualsValueClauseSyntax(declarator, name, vbInitValue, declaredSymbolType, declaredSymbol, initializerOrMethodDecl);
                var v = SyntaxFactory.VariableDeclarator(ConvertIdentifier(name.Identifier), null, equalsValueClauseSyntax);
                string k = declaredSymbolType?.GetFullMetadataName() ?? name.ToString();//Use likely unique key if the type symbol isn't available

                if (csVars.TryGetValue(k, out var decl)) {
                    csVars[k] = decl.AddVariables(v);
                    continue;
                }

                if (initializerOrMethodDecl == null || initializerOrMethodDecl is ExpressionSyntax) {
                    var variableDeclaration = CreateVariableDeclaration(declarator, preferExplicitType,
                        requireExplicitTypeForAll, vbInitializerType, declaredSymbolType, equalsValueClauseSyntax,
                        initSymbol, v);
                    csVars[k] = variableDeclaration;
                } else {
                    csMethods.Add(initializerOrMethodDecl);
                }
            }

            return (csVars.Values, csMethods);
        }

        private async Task<EqualsValueClauseSyntax> ConvertEqualsValueClauseSyntax(
            VariableDeclaratorSyntax vbDeclarator, ModifiedIdentifierSyntax vbName,
            VBSyntax.ExpressionSyntax vbInitValue,
            ITypeSymbol declaredSymbolType,
            ISymbol declaredSymbol, CSharpSyntaxNode initializerOrMethodDecl)
        {
            var csTypeSyntax = GetTypeSyntax(declaredSymbolType);

            bool isField = vbDeclarator.Parent.IsKind(SyntaxKind.FieldDeclaration);
            bool isConst = declaredSymbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst ||
                           declaredSymbol is ILocalSymbol localSymbol && localSymbol.IsConst;

            EqualsValueClauseSyntax equalsValueClauseSyntax;
            if (await GetInitializerFromNameAndType(declaredSymbolType, vbName, initializerOrMethodDecl) is ExpressionSyntax
                adjustedInitializerExpr)
            {
                var convertedInitializer = vbInitValue != null
                    ? TypeConversionAnalyzer.AddExplicitConversion(vbInitValue, adjustedInitializerExpr, isConst: isConst)
                    : adjustedInitializerExpr;
                equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(convertedInitializer);
            }
            else if (isField || declaredSymbol != null && _semanticModel.IsDefinitelyAssignedBeforeRead(declaredSymbol, vbName))
            {
                equalsValueClauseSyntax = null;
            }
            else
            {
                // VB initializes variables to their default
                equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(csTypeSyntax));
            }

            return equalsValueClauseSyntax;
        }

        private VariableDeclarationSyntax CreateVariableDeclaration(VariableDeclaratorSyntax vbDeclarator, bool preferExplicitType,
            bool requireExplicitTypeForAll, ITypeSymbol vbInitializerType, ITypeSymbol declaredSymbolType,
            EqualsValueClauseSyntax equalsValueClauseSyntax, IMethodSymbol initSymbol, CSSyntax.VariableDeclaratorSyntax v)
        {
            var requireExplicitType = requireExplicitTypeForAll ||
                                      vbInitializerType != null && !Equals(declaredSymbolType, vbInitializerType);
            bool useVar = equalsValueClauseSyntax != null && !preferExplicitType && !requireExplicitType;
            var typeSyntax = initSymbol == null || !initSymbol.IsAnonymousFunction()
                ? GetTypeSyntax(declaredSymbolType, useVar)
                : GetFuncTypeSyntax(initSymbol);
            return SyntaxFactory.VariableDeclaration(typeSyntax, SyntaxFactory.SingletonSeparatedList(v));
        }

        private TypeSyntax GetFuncTypeSyntax(IMethodSymbol method)
        {
            var parameters = method.Parameters.Select(p => p.Type).ToArray();
            if (method.ReturnsVoid) {
                return parameters.Any() ? (TypeSyntax)CsSyntaxGenerator.GenericName(nameof(Action), parameters)
                    : SyntaxFactory.ParseTypeName("Action");
            }

            parameters = parameters.Concat(new[] {method.ReturnType}).ToArray();
            return (TypeSyntax)CsSyntaxGenerator.GenericName(nameof(Func<object>), parameters);
        }

        public TypeSyntax GetTypeSyntax(ITypeSymbol typeSymbol, bool useImplicitType = false)
        {
            if (useImplicitType || typeSymbol == null) return CreateVarTypeName();
            var syntax = (TypeSyntax)CsSyntaxGenerator.TypeExpression(typeSymbol);

            return WithDeclarationCasing(syntax, typeSymbol);
        }

        private static TypeSyntax WithDeclarationCasing(TypeSyntax syntax, ITypeSymbol typeSymbol)
        {
            var vbType = SyntaxFactory.ParseTypeName(typeSymbol.ToDisplayString());
            var originalNames = vbType.DescendantNodes().OfType<CSSyntax.IdentifierNameSyntax>()
                .Select(i => i.ToString()).ToList();

            return syntax.ReplaceNodes(syntax.DescendantNodes().OfType<CSSyntax.IdentifierNameSyntax>(), (oldNode, n) =>
            {
                var originalName = originalNames.FirstOrDefault(on => string.Equals(@on, oldNode.ToString(), StringComparison.OrdinalIgnoreCase));
                return originalName != null ? SyntaxFactory.IdentifierName(originalName) : oldNode;
            });
        }

        private static TypeSyntax CreateVarTypeName()
        {
            return SyntaxFactory.ParseTypeName("var");
        }

        private static VBSyntax.ExpressionSyntax GetInitializerToConvert(VariableDeclaratorSyntax declarator)
        {
            return declarator.AsClause?.TypeSwitch(
                       (SimpleAsClauseSyntax _) => declarator.Initializer?.Value,
                       (AsNewClauseSyntax c) => c.NewExpression
                   ) ?? declarator.Initializer?.Value;
        }

        private async Task<CSharpSyntaxNode> GetInitializerFromNameAndType(ITypeSymbol typeSymbol,
            ModifiedIdentifierSyntax name, CSharpSyntaxNode initializer)
        {
            if (!SyntaxTokenExtensions.IsKind(name.Nullable, SyntaxKind.None))
            {
                if (typeSymbol.IsArrayType())
                {
                    initializer = null;
                }
            }

            var rankSpecifiers = await ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds, false);
            if (rankSpecifiers.Count > 0)
            {
                var rankSpecifiersWithSizes = await ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds);
                if (rankSpecifiersWithSizes.SelectMany(ars => ars.Sizes).Any(e => !e.IsKind(CSSyntaxKind.OmittedArraySizeExpression)))
                {
                    var arrayTypeSyntax = (ArrayTypeSyntax) GetTypeSyntax(typeSymbol);
                    arrayTypeSyntax = arrayTypeSyntax.WithRankSpecifiers(rankSpecifiersWithSizes);
                    initializer = SyntaxFactory.ArrayCreationExpression(arrayTypeSyntax);
                }
            }

            return initializer;
        }

        public ExpressionSyntax Literal(object o, string textForUser = null) => LiteralConversions.GetLiteralExpression(o, textForUser);

        public SyntaxToken ConvertIdentifier(SyntaxToken id, bool isAttribute = false)
        {
            string text = id.ValueText;

            if (id.SyntaxTree == _semanticModel.SyntaxTree) {
                var idSymbol = _semanticModel.GetSymbolInfo(id.Parent).Symbol ?? _semanticModel.GetDeclaredSymbol(id.Parent);
                var baseSymbol = idSymbol.IsKind(SymbolKind.Method) || idSymbol.IsKind(SymbolKind.Property) ? idSymbol.FollowProperty(s => s.OverriddenMember()).Last() : idSymbol;
                if (baseSymbol != null && !String.IsNullOrWhiteSpace(baseSymbol.Name)) {
                    text = WithDeclarationCasing(id, baseSymbol, text);

                    if (baseSymbol.IsConstructor() && isAttribute) {
                        text = baseSymbol.ContainingType.Name;
                        if (text.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
                            text = text.Remove(text.Length - "Attribute".Length);
                    } else if (baseSymbol.IsKind(SymbolKind.Parameter) && baseSymbol.ContainingSymbol.IsAccessorPropertySet() && ((baseSymbol.IsImplicitlyDeclared && baseSymbol.Name == "Value") || baseSymbol.ContainingSymbol.GetParameters().FirstOrDefault(x => !x.IsImplicitlyDeclared).Equals(baseSymbol))) {
                        // The case above is basically that if the symbol is a parameter, and the corresponding definition is a property set definition
                        // AND the first explicitly declared parameter is this symbol, we need to replace it with value.
                        text = "value";
                    } else if (text.StartsWith("_", StringComparison.OrdinalIgnoreCase) && idSymbol is IFieldSymbol propertyFieldSymbol && propertyFieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Property) == true) {
                        text = propertyFieldSymbol.AssociatedSymbol.Name;
                    } else if (text.EndsWith("Event", StringComparison.OrdinalIgnoreCase) && idSymbol is IFieldSymbol eventFieldSymbol && eventFieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Event) == true) {
                        text = eventFieldSymbol.AssociatedSymbol.Name;
                    } else if (MustInlinePropertyWithEventsAccess(id.Parent, baseSymbol)) {
                        // For C# Winforms designer, we need to use direct field access (and inline any event handlers)
                        text = "_" + text;
                    }
                }
            }

            return CsEscapedIdentifier(text);
        }

        private static string WithDeclarationCasing(SyntaxToken id, ISymbol symbol, string text)
        {
            bool isDeclaration = symbol.Locations.Any(l => l.SourceSpan == id.Span);
            bool isPartial = symbol.IsPartialClassDefinition() || symbol.IsPartialMethodDefinition() ||
                             symbol.IsPartialMethodImplementation();
            if (isPartial || (!isDeclaration && text.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase)))
            {
                text = symbol.Name;
            }

            return text;
        }

        public static bool MustInlinePropertyWithEventsAccess(SyntaxNode anyNodePossiblyWithinMethod, ISymbol potentialPropertySymbol)
        {
            return InMethodCalledInitializeComponent(anyNodePossiblyWithinMethod) && potentialPropertySymbol is IPropertySymbol prop && prop.IsWithEvents;
        }

        public static bool InMethodCalledInitializeComponent(SyntaxNode anyNodePossiblyWithinMethod)
        {
            return anyNodePossiblyWithinMethod.GetAncestor<MethodBlockSyntax>()?.SubOrFunctionStatement.Identifier.Text == "InitializeComponent";
        }

        private static SyntaxToken CsEscapedIdentifier(string text)
        {
            if (SyntaxFacts.GetKeywordKind(text) != CSSyntaxKind.None) text = "@" + text;
            return SyntaxFactory.Identifier(text);
        }

        public SyntaxTokenList ConvertModifiers(SyntaxNode node, IEnumerable<SyntaxToken> modifiers,
            TokenContext context = TokenContext.Global, bool isVariableOrConst = false, params CSSyntaxKind[] extraCsModifierKinds)
        {
            ISymbol declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
            var declaredAccessibility = declaredSymbol?.DeclaredAccessibility ?? Accessibility.NotApplicable;

            var contextsWithIdenticalDefaults = new[] { TokenContext.Global, TokenContext.Local, TokenContext.InterfaceOrModule, TokenContext.MemberInInterface };
            bool isPartial = declaredSymbol.IsPartialClassDefinition() || declaredSymbol.IsPartialMethodDefinition() || declaredSymbol.IsPartialMethodImplementation();
            bool implicitVisibility = contextsWithIdenticalDefaults.Contains(context) || isVariableOrConst || declaredSymbol.IsStaticConstructor();
            if (implicitVisibility && !isPartial) declaredAccessibility = Accessibility.NotApplicable;
            var modifierSyntaxs = ConvertModifiersCore(declaredAccessibility, modifiers, context)
                .Concat(extraCsModifierKinds.Select(SyntaxFactory.Token))
                .Where(t => CSharpExtensions.Kind(t) != CSSyntaxKind.None)
                .OrderBy(m => SyntaxTokenExtensions.IsKind(m, CSSyntaxKind.PartialKeyword));
            return SyntaxFactory.TokenList(modifierSyntaxs);
        }

        private SyntaxToken? ConvertModifier(SyntaxToken m, TokenContext context = TokenContext.Global)
        {
            SyntaxKind vbSyntaxKind = VisualBasicExtensions.Kind(m);
            switch (vbSyntaxKind) {
                case SyntaxKind.DateKeyword:
                    return SyntaxFactory.Identifier("DateTime");
            }
            var token = vbSyntaxKind.ConvertToken(context);
            return token == CSSyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        private IEnumerable<SyntaxToken> ConvertModifiersCore(Accessibility declaredAccessibility,
            IEnumerable<SyntaxToken> modifiers, TokenContext context)
        {
            var remainingModifiers = modifiers.ToList();
            if (declaredAccessibility != Accessibility.NotApplicable) {
                remainingModifiers = remainingModifiers.Where(m => !m.IsVbVisibility(false, false)).ToList();
                foreach (var visibilitySyntaxKind in CsSyntaxAccessibilityKindForContext(declaredAccessibility, context)) {
                    yield return SyntaxFactory.Token(visibilitySyntaxKind);
                }
            }

            foreach (var token in remainingModifiers.Where(m => !IgnoreInContext(m, context))) {
                var m = ConvertModifier(token, context);
                if (m.HasValue) yield return m.Value;
            }
            if (context == TokenContext.MemberInModule &&
                    !remainingModifiers.Any(a => VisualBasicExtensions.Kind(a) == SyntaxKind.ConstKeyword ))
                yield return SyntaxFactory.Token(CSSyntaxKind.StaticKeyword);
        }

        private IEnumerable<CSSyntaxKind> CsSyntaxAccessibilityKindForContext(Accessibility declaredAccessibility,
            TokenContext context)
        {
            return CsSyntaxAccessibilityKind(declaredAccessibility);

        }

        private IEnumerable<CSSyntaxKind> CsSyntaxAccessibilityKind(Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility) {
                case Accessibility.Private:
                    return new[] { CSSyntaxKind.PrivateKeyword };
                case Accessibility.Protected:
                    return new[] { CSSyntaxKind.ProtectedKeyword };
                case Accessibility.Internal:
                    return new[] { CSSyntaxKind.InternalKeyword };
                case Accessibility.ProtectedOrInternal:
                    return new[] { CSSyntaxKind.ProtectedKeyword, CSSyntaxKind.InternalKeyword };
                case Accessibility.Public:
                    return new[] { CSSyntaxKind.PublicKeyword };
                case Accessibility.ProtectedAndInternal:
                case Accessibility.NotApplicable:
                default:
                    throw new ArgumentOutOfRangeException(nameof(declaredAccessibility), declaredAccessibility, null);
            }
        }

        private bool IgnoreInContext(SyntaxToken m, TokenContext context)
        {
            switch (VisualBasicExtensions.Kind(m)) {
                case SyntaxKind.OptionalKeyword:
                case SyntaxKind.ByValKeyword:
                case SyntaxKind.IteratorKeyword:
                case SyntaxKind.DimKeyword:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsConversionOperator(SyntaxToken token)
        {
            bool isConvOp= token.IsKind(CSSyntaxKind.ExplicitKeyword, CSSyntaxKind.ImplicitKeyword)
                           ||token.IsKind(SyntaxKind.NarrowingKeyword, SyntaxKind.WideningKeyword);
            return isConvOp;
        }

        internal async Task<SyntaxList<ArrayRankSpecifierSyntax>> ConvertArrayRankSpecifierSyntaxes(
            SyntaxList<VBSyntax.ArrayRankSpecifierSyntax> arrayRankSpecifierSyntaxs,
            ArgumentListSyntax nodeArrayBounds, bool withSizes = true)
        {
            var bounds = SyntaxFactory.List(await arrayRankSpecifierSyntaxs.SelectAsync(async r => (ArrayRankSpecifierSyntax) await r.AcceptAsync(TriviaConvertingExpressionVisitor)));

            if (nodeArrayBounds != null) {
                var sizesSpecified = nodeArrayBounds.Arguments.Any(a => !a.IsOmitted);
                var rank = nodeArrayBounds.Arguments.Count;
                if (!sizesSpecified) rank += 1;

                var convertedArrayBounds = withSizes && sizesSpecified ? await ConvertArrayBounds(nodeArrayBounds)
                    : Enumerable.Repeat(SyntaxFactory.OmittedArraySizeExpression(), rank);
                var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SeparatedList(
                        convertedArrayBounds));
                bounds = bounds.Insert(0, arrayRankSpecifierSyntax);
            }

            return bounds;
        }

        public async Task<IEnumerable<ExpressionSyntax>> ConvertArrayBounds(ArgumentListSyntax argumentListSyntax)
        {
            return await argumentListSyntax.Arguments.SelectAsync(a => {
                VBSyntax.ExpressionSyntax upperBoundExpression = a is SimpleArgumentSyntax sas ? sas.Expression
                    : a is RangeArgumentSyntax ras ? ras.UpperBound
                    : throw new ArgumentOutOfRangeException(nameof(a), a, null);

                return IncreaseArrayUpperBoundExpression(upperBoundExpression);
            });
        }

        private async Task<ExpressionSyntax> IncreaseArrayUpperBoundExpression(VBSyntax.ExpressionSyntax expr)
        {
            var op = _semanticModel.GetOperation(expr);
            var constant = op.ConstantValue;
            if (constant.HasValue && constant.Value is int)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)constant.Value + 1));

            var convertedExpression = (ExpressionSyntax)await expr.AcceptAsync(TriviaConvertingExpressionVisitor);

            if (op is IBinaryOperation bOp && bOp.OperatorKind == BinaryOperatorKind.Subtract &&
                bOp.RightOperand.ConstantValue.HasValue && bOp.RightOperand.ConstantValue.Value is int subtractedVal && subtractedVal == 1
                && convertedExpression is CSSyntax.BinaryExpressionSyntax bExp && bExp.IsKind(CSSyntaxKind.SubtractExpression))
                return bExp.Left;

            return SyntaxFactory.BinaryExpression(
                CSSyntaxKind.SubtractExpression,
                convertedExpression, SyntaxFactory.Token(CSSyntaxKind.PlusToken), SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        }

        public static AttributeArgumentListSyntax CreateAttributeArgumentList(params AttributeArgumentSyntax[] attributeArgumentSyntaxs)
        {
            return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(attributeArgumentSyntaxs));
        }

        public static VariableDeclarationSyntax CreateVariableDeclarationAndAssignment(string variableName,
            ExpressionSyntax initValue, TypeSyntax explicitType = null)
        {
            CSSyntax.VariableDeclaratorSyntax variableDeclaratorSyntax = CreateVariableDeclarator(variableName, initValue);
            var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(
                explicitType ?? SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.SingletonSeparatedList(variableDeclaratorSyntax));
            return variableDeclarationSyntax;
        }

        public static CSSyntax.VariableDeclaratorSyntax CreateVariableDeclarator(string variableName, ExpressionSyntax initValue)
        {
            var variableDeclaratorSyntax = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.Identifier(variableName), null,
                SyntaxFactory.EqualsValueClause(initValue));
            return variableDeclaratorSyntax;
        }

        public async Task<(string, ExpressionSyntax extraArg)> GetParameterizedPropertyAccessMethod(IOperation operation)
        {
            if (operation is IPropertyReferenceOperation pro && pro.Arguments.Any() &&
                !pro.Property.IsDefault()) {
                var isSetter = pro.Parent.Kind == OperationKind.SimpleAssignment && pro.Parent.Children.First() == pro;
                var extraArg = isSetter
                    ? (ExpressionSyntax) await TriviaConvertingExpressionVisitor.Visit(operation.Parent.Syntax.ChildNodes().ElementAt(1))
                    : null;
                return (isSetter ? pro.Property.SetMethod.Name : pro.Property.GetMethod.Name, extraArg);
            }

            return (null, null);
        }

        public static bool IsDefaultIndexer(SyntaxNode node)
        {
            return node is PropertyStatementSyntax pss && pss.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DefaultKeyword));
        }


        public bool HasExtensionAttribute(VBSyntax.AttributeListSyntax a)
        {
            return a.Attributes.Any(IsExtensionAttribute);
        }

        public bool HasOutAttribute(VBSyntax.AttributeListSyntax a)
        {
            return a.Attributes.Any(IsOutAttribute);
        }

        public bool IsExtensionAttribute(VBSyntax.AttributeSyntax a)
        {
            return _semanticModel.GetTypeInfo(a).ConvertedType?.GetFullMetadataName()
                       ?.Equals(ExtensionAttributeType.FullName) == true;
        }

        public bool IsOutAttribute(VBSyntax.AttributeSyntax a)
        {
            return _semanticModel.GetTypeInfo(a).ConvertedType?.GetFullMetadataName()
                       ?.Equals(OutAttributeType.FullName) == true;
        }

        public ISymbol GetDeclaredCsOriginalSymbolOrNull(VisualBasicSyntaxNode node)
        {
            var declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
            return declaredSymbol != null ? GetCsOriginalSymbolOrNull(declaredSymbol) : null;
        }

        public ISymbol GetCsOriginalSymbolOrNull(ISymbol symbol)
        {
            symbol = symbol.OriginalDefinition;
            // Construct throws an exception if ConstructedFrom differs from it, so let's use ConstructedFrom directly
            var symbolToFind = symbol is IMethodSymbol m ? m.ConstructedFrom : symbol;
            var similarSymbol = SymbolFinder.FindSimilarSymbols(symbolToFind, _csCompilation).FirstOrDefault();
            return similarSymbol;
        }
    }
}