using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using ICSharpCode.CodeConverter.Util.FromRoslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using ArgumentListSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayRankSpecifierSyntax;
using ArrayTypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayTypeSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;
using VariableDeclaratorSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax;
using VisualBasicExtensions = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using ITypeSymbol = Microsoft.CodeAnalysis.ITypeSymbol;
using TypeInfo = Microsoft.CodeAnalysis.TypeInfo;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommonConversions
    {
        private static readonly Type ExtensionAttributeType = typeof(ExtensionAttribute);
        private static readonly Type OutAttributeType = typeof(OutAttribute);
        public Document Document { get; }
        private readonly SemanticModel _semanticModel;
        public SyntaxGenerator CsSyntaxGenerator { get; }
        public VisualBasicEqualityComparison VisualBasicEqualityComparison { get; }

        private readonly CSharpCompilation _csCompilation;
        private readonly ITypeContext _typeContext;

        public CommentConvertingVisitorWrapper TriviaConvertingExpressionVisitor { get; set; }
        public TypeConversionAnalyzer TypeConversionAnalyzer { get; }

        public CommonConversions(Document document, SemanticModel semanticModel,
            TypeConversionAnalyzer typeConversionAnalyzer, SyntaxGenerator csSyntaxGenerator,
            CSharpCompilation csCompilation, ITypeContext typeContext, VisualBasicEqualityComparison visualBasicEqualityComparison)
        {
            TypeConversionAnalyzer = typeConversionAnalyzer;
            Document = document;
            _semanticModel = semanticModel;
            CsSyntaxGenerator = csSyntaxGenerator;
            _csCompilation = csCompilation;
            _typeContext = typeContext;
            VisualBasicEqualityComparison = visualBasicEqualityComparison;
        }

        public async Task<(IReadOnlyCollection<(VariableDeclarationSyntax Decl, ITypeSymbol Type)> Variables, IReadOnlyCollection<CSharpSyntaxNode> Methods)> SplitVariableDeclarationsAsync(
            VariableDeclaratorSyntax declarator, HashSet<ILocalSymbol> symbolsToSkip = null, bool preferExplicitType = false)
        {
            var vbInitValue = GetInitializerToConvert(declarator);
            var initializerOrMethodDecl = await vbInitValue.AcceptAsync(TriviaConvertingExpressionVisitor);
            var vbInitializerTypeInfo = vbInitValue != null ? _semanticModel.GetTypeInfo(vbInitValue) : default(TypeInfo?);
            var vbInitializerType = vbInitValue != null ? vbInitializerTypeInfo.Value.Type : default(ITypeSymbol);

            bool requireExplicitTypeForAll = declarator.Names.Count > 1;
            IMethodSymbol initSymbol = null;
            if (vbInitValue != null) {
                TypeInfo expType = vbInitializerTypeInfo.Value;
                preferExplicitType |= ShouldPreferExplicitType(vbInitValue, expType.ConvertedType, out bool vbInitIsNothingLiteral);
                initSymbol = _semanticModel.GetSymbolInfo(vbInitValue).Symbol as IMethodSymbol;
                bool isAnonymousFunction = initSymbol?.IsAnonymousFunction() == true;
                requireExplicitTypeForAll |= vbInitIsNothingLiteral || isAnonymousFunction;
            }

            var csVars = new Dictionary<string, (VariableDeclarationSyntax Decl, ITypeSymbol Type)>();
            var csMethods = new List<CSharpSyntaxNode>();

            foreach (var name in declarator.Names) {

                var declaredSymbol = _semanticModel.GetDeclaredSymbol(name);
                if (symbolsToSkip?.Contains(declaredSymbol) == true) continue;
                var declaredSymbolType = declaredSymbol.GetSymbolType();
                var equalsValueClauseSyntax = await ConvertEqualsValueClauseSyntaxAsync(declarator, name, vbInitValue, declaredSymbolType, declaredSymbol, initializerOrMethodDecl);
                var v = SyntaxFactory.VariableDeclarator(ConvertIdentifier(name.Identifier), null, equalsValueClauseSyntax);
                string k = declaredSymbolType?.GetFullMetadataName() ?? name.ToString();//Use likely unique key if the type symbol isn't available

                if (csVars.TryGetValue(k, out var decl)) {
                    csVars[k] = (decl.Decl.AddVariables(v), decl.Type);
                    continue;
                }

                if (initializerOrMethodDecl == null || initializerOrMethodDecl is ExpressionSyntax) {
                    var variableDeclaration = CreateVariableDeclaration(declarator, preferExplicitType,
                        requireExplicitTypeForAll, vbInitializerType, declaredSymbolType, equalsValueClauseSyntax,
                        initSymbol, v);
                    csVars[k] = (variableDeclaration, declaredSymbolType);
                } else {
                    csMethods.Add(initializerOrMethodDecl);
                }
            }

            return (csVars.Values, csMethods);
        }

        public bool ShouldPreferExplicitType(VBSyntax.ExpressionSyntax exp,
            ITypeSymbol expConvertedType,
            out bool isNothingLiteral)
        {
            var op = _semanticModel.GetExpressionOperation(exp);
            exp = op.Syntax as VBSyntax.ExpressionSyntax;
            var vbInitConstantValue = _semanticModel.GetConstantValue(exp);
            isNothingLiteral = vbInitConstantValue.HasValue && vbInitConstantValue.Value == null || exp is VBSyntax.LiteralExpressionSyntax les && les.IsKind(SyntaxKind.NothingLiteralExpression);
            bool shouldPreferExplicitType = expConvertedType != null && (expConvertedType.HasCsKeyword() || !expConvertedType.Equals(op.Type));
            return shouldPreferExplicitType;
        }

        private async Task<EqualsValueClauseSyntax> ConvertEqualsValueClauseSyntaxAsync(
            VariableDeclaratorSyntax vbDeclarator, VBSyntax.ModifiedIdentifierSyntax vbName,
            VBSyntax.ExpressionSyntax vbInitValue,
            ITypeSymbol declaredSymbolType,
            ISymbol declaredSymbol, CSharpSyntaxNode initializerOrMethodDecl)
        {
            var csTypeSyntax = GetTypeSyntax(declaredSymbolType);

            bool isField = vbDeclarator.Parent.IsKind(SyntaxKind.FieldDeclaration);
            bool declaredConst = declaredSymbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst ||
                                 declaredSymbol is ILocalSymbol localSymbol && localSymbol.IsConst;

            EqualsValueClauseSyntax equalsValueClauseSyntax;
            if (await GetInitializerFromNameAndTypeAsync(declaredSymbolType, vbName, initializerOrMethodDecl) is ExpressionSyntax
                adjustedInitializerExpr)
            {
                var convertedInitializer = vbInitValue != null
                    ? TypeConversionAnalyzer.AddExplicitConversion(vbInitValue, adjustedInitializerExpr, isConst: declaredConst)
                    : adjustedInitializerExpr;

                if (isField && !declaredSymbol.IsStatic && !_semanticModel.IsDefinitelyStatic(vbName, vbInitValue)) {
                    if (_typeContext.Initializers.ShouldAddTypeWideInitToThisPart) {
                        var lhs = SyntaxFactory.IdentifierName(ConvertIdentifier(vbName.Identifier, sourceTriviaMapKind: SourceTriviaMapKind.None));
                        _typeContext.Initializers.AdditionalInstanceInitializers.Add((lhs, CSSyntaxKind.SimpleAssignmentExpression, adjustedInitializerExpr));
                        equalsValueClauseSyntax = null;
                    } else {
                        var returnBlock = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(adjustedInitializerExpr));
                        _typeContext.HoistedState.Hoist<HoistedParameterlessFunction>(new HoistedParameterlessFunction(GetInitialValueFunctionName(vbName), csTypeSyntax, returnBlock));
                        equalsValueClauseSyntax = null;
                    }
                } else {
                    equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(convertedInitializer);
                }
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

        /// <remarks>
        /// In CS we need to lift non-static initializers to the constructor. But for partial classes these can be in different files.
        /// Rather than re-architect to allow communication between files, we create an initializer function, and call it from the other part, and just hope the name doesn't clash.
        /// </remarks>
        public static string GetInitialValueFunctionName(VBSyntax.ModifiedIdentifierSyntax vbName)
        {
            return "initial" + vbName.Identifier.ValueText.ToPascalCase();
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
            if (useImplicitType || typeSymbol == null) return ValidSyntaxFactory.VarType;
            var syntax = (TypeSyntax)CsSyntaxGenerator.TypeExpression(typeSymbol);

            return WithDeclarationNameCasing(syntax, typeSymbol);
        }

        /// <summary>
        /// Semantic model merges the symbols, but the compiled form retains multiple namespaces, which (when referenced from C#) need to keep the correct casing.
        /// <seealso cref="DeclarationNodeVisitor.WithDeclarationNameCasingAsync(VBSyntax.NamespaceBlockSyntax, ISymbol)"/>
        /// <seealso cref="CommonConversions.WithDeclarationName(SyntaxToken, ISymbol, string)"/>
        /// </summary>
        private static TypeSyntax WithDeclarationNameCasing(TypeSyntax syntax, ITypeSymbol typeSymbol)
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

        private static VBSyntax.ExpressionSyntax GetInitializerToConvert(VariableDeclaratorSyntax declarator)
        {
            return declarator.AsClause?.TypeSwitch(
                       (VBSyntax.SimpleAsClauseSyntax _) => declarator.Initializer?.Value,
                       (VBSyntax.AsNewClauseSyntax c) => c.NewExpression
                   ) ?? declarator.Initializer?.Value;
        }

        private async Task<CSharpSyntaxNode> GetInitializerFromNameAndTypeAsync(ITypeSymbol typeSymbol,
            VBSyntax.ModifiedIdentifierSyntax name, CSharpSyntaxNode initializer)
        {
            if (!SyntaxTokenExtensions.IsKind(name.Nullable, SyntaxKind.None))
            {
                if (typeSymbol.IsArrayType())
                {
                    initializer = null;
                }
            }

            var rankSpecifiers = await ConvertArrayRankSpecifierSyntaxesAsync(name.ArrayRankSpecifiers, name.ArrayBounds, false);
            if (rankSpecifiers.Count > 0)
            {
                var rankSpecifiersWithSizes = await ConvertArrayRankSpecifierSyntaxesAsync(name.ArrayRankSpecifiers, name.ArrayBounds);
                var arrayTypeSyntax = ((ArrayTypeSyntax)GetTypeSyntax(typeSymbol)).WithRankSpecifiers(rankSpecifiersWithSizes);
                if (rankSpecifiersWithSizes.SelectMany(ars => ars.Sizes).Any(e => !e.IsKind(CSSyntaxKind.OmittedArraySizeExpression))) {
                    initializer = SyntaxFactory.ArrayCreationExpression(arrayTypeSyntax);
                } else if (initializer is ImplicitArrayCreationExpressionSyntax iaces && iaces.Initializer != null) {
                    initializer = SyntaxFactory.ArrayCreationExpression(arrayTypeSyntax, iaces.Initializer);
                }
            }

            return initializer;
        }

        public ExpressionSyntax Literal(object o, string textForUser = null, ITypeSymbol convertedType = null) => LiteralConversions.GetLiteralExpression(o, textForUser, convertedType);

        public SyntaxToken ConvertIdentifier(SyntaxToken id, bool isAttribute = false, SourceTriviaMapKind sourceTriviaMapKind = SourceTriviaMapKind.All)
        {
            string text = id.ValueText;

            if (id.SyntaxTree == _semanticModel.SyntaxTree) {
                var idSymbol = _semanticModel.GetSymbolInfo(id.Parent).Symbol ?? _semanticModel.GetDeclaredSymbol(id.Parent);
                if (idSymbol != null && !String.IsNullOrWhiteSpace(idSymbol.Name)) {
                    text = WithDeclarationName(id, idSymbol, text);
                    var normalizedText = text.WithHalfWidthLatinCharacters();
                    if (idSymbol.IsConstructor() && isAttribute) {
                        text = idSymbol.ContainingType.Name;
                        if (normalizedText.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
                            text = text.Remove(text.Length - "Attribute".Length);
                    } else if (idSymbol.IsKind(SymbolKind.Parameter) && idSymbol.ContainingSymbol.IsAccessorWithValueInCsharp() && ((idSymbol.IsImplicitlyDeclared && idSymbol.Name.WithHalfWidthLatinCharacters().Equals("value", StringComparison.OrdinalIgnoreCase)) || idSymbol.Equals(idSymbol.ContainingSymbol.GetParameters().FirstOrDefault(x => !x.IsImplicitlyDeclared)))) {
                        // The case above is basically that if the symbol is a parameter, and the corresponding definition is a property set definition
                        // AND the first explicitly declared parameter is this symbol, we need to replace it with value.
                        text = "value";
                    } else if (normalizedText.StartsWith("_", StringComparison.OrdinalIgnoreCase) && idSymbol is IFieldSymbol propertyFieldSymbol && propertyFieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Property) == true) {
                        text = propertyFieldSymbol.AssociatedSymbol.Name;
                    } else if (normalizedText.EndsWith("Event", StringComparison.OrdinalIgnoreCase) && idSymbol is IFieldSymbol eventFieldSymbol && eventFieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Event) == true) {
                        text = eventFieldSymbol.AssociatedSymbol.Name;
                    } else if (WinformsConversions.MustInlinePropertyWithEventsAccess(id.Parent, idSymbol)) {
                        // For C# Winforms designer, we need to use direct field access - see other usage of MustInlinePropertyWithEventsAccess
                        text = "_" + text;
                    }
                }
                var csId = CsEscapedIdentifier(text);
                return sourceTriviaMapKind != SourceTriviaMapKind.None ? csId : csId.WithSourceMappingFrom(id);
            } else {
                text = text.WithHalfWidthLatinCharacters();
            }

            return CsEscapedIdentifier(text);
        }

        /// <summary>
        /// Semantic model merges the symbols, but the compiled form retains multiple namespaces, which (when referenced from C#) need to keep the correct casing.
        /// <seealso cref="DeclarationNodeVisitor.WithDeclarationNameCasingAsync(VBSyntax.NamespaceBlockSyntax, ISymbol)"/>
        /// <seealso cref="CommonConversions.WithDeclarationNameCasing(TypeSyntax, ITypeSymbol)"/>
        /// </summary>
        private static string WithDeclarationName(SyntaxToken id, ISymbol idSymbol, string text)
        {
            //This also covers the case when the name is different (in VB you can have method X implements IFoo.Y), but doesn't resolve any resulting name clashes
            var baseSymbol = idSymbol.IsKind(SymbolKind.Method) || idSymbol.IsKind(SymbolKind.Property) ? idSymbol.FollowProperty(s => s.BaseMember()).Last() : idSymbol;
            bool isDeclaration = baseSymbol.Locations.Any(l => l.SourceSpan == id.Span);
            bool isPartial = baseSymbol.IsPartialClassDefinition() || baseSymbol.IsPartialMethodDefinition() ||
                             baseSymbol.IsPartialMethodImplementation();
            if (isPartial || !isDeclaration)
            {
                text = baseSymbol.Name;
            }

            return text;
        }

        public static SyntaxToken CsEscapedIdentifier(string text)
        {
            if (SyntaxFacts.GetKeywordKind(text) != CSSyntaxKind.None) text = "@" + text;
            return SyntaxFactory.Identifier(text);
        }

        public SyntaxTokenList ConvertModifiers(SyntaxNode node, IReadOnlyCollection<SyntaxToken> modifiers,
            TokenContext context = TokenContext.Global, bool isVariableOrConst = false, params CSSyntaxKind[] extraCsModifierKinds)
        {
            ISymbol declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
            var declaredAccessibility = declaredSymbol?.DeclaredAccessibility ?? Accessibility.NotApplicable;
            modifiers = modifiers.Where(m =>
                !m.IsKind(SyntaxKind.OverloadsKeyword) || RequiresNewKeyword(declaredSymbol) != false).ToList();
            var contextsWithIdenticalDefaults = new[] { TokenContext.Global, TokenContext.Local, TokenContext.InterfaceOrModule, TokenContext.MemberInInterface };
            bool isPartial = declaredSymbol.IsPartialClassDefinition() || declaredSymbol.IsPartialMethodDefinition() || declaredSymbol.IsPartialMethodImplementation();
            bool implicitVisibility = ContextHasIdenticalDefaults(context, contextsWithIdenticalDefaults, declaredSymbol)
                                      || isVariableOrConst || declaredSymbol.IsStaticConstructor();
            if (implicitVisibility && !isPartial) declaredAccessibility = Accessibility.NotApplicable;
            var modifierSyntaxs = ConvertModifiersCore(declaredAccessibility, modifiers, context)
                .Concat(extraCsModifierKinds.Select(SyntaxFactory.Token))
                .Where(t => CSharpExtensions.Kind(t) != CSSyntaxKind.None)
                .OrderBy(m => SyntaxTokenExtensions.IsKind(m, CSSyntaxKind.PartialKeyword));
            return SyntaxFactory.TokenList(modifierSyntaxs);
        }

        private static bool? RequiresNewKeyword(ISymbol declaredSymbol)
        {
            if (!(declaredSymbol is IMethodSymbol methodSymbol)) return null;
            if (declaredSymbol.IsOverride ) return false;
            var methodSignature = methodSymbol.GetUnqualifiedMethodSignature(true);
            return declaredSymbol.ContainingType.FollowProperty(s => s.BaseType).Skip(1).Any(t => t.GetMembers()
                .Any(s => s.Name == declaredSymbol.Name && s is IMethodSymbol m && m.GetUnqualifiedMethodSignature(true) == methodSignature));
        }

        private static bool ContextHasIdenticalDefaults(TokenContext context, TokenContext[] contextsWithIdenticalDefaults, ISymbol declaredSymbol)
        {
            if (!contextsWithIdenticalDefaults.Contains(context)) {
                return false;
            }

            return declaredSymbol == null || !declaredSymbol.IsType() || declaredSymbol.ContainingType == null;
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

        internal async Task<SyntaxList<ArrayRankSpecifierSyntax>> ConvertArrayRankSpecifierSyntaxesAsync(
            SyntaxList<VBSyntax.ArrayRankSpecifierSyntax> arrayRankSpecifierSyntaxs,
            ArgumentListSyntax nodeArrayBounds, bool withSizes = true)
        {
            var bounds = SyntaxFactory.List(await arrayRankSpecifierSyntaxs.SelectAsync(async r => (ArrayRankSpecifierSyntax) await r.AcceptAsync(TriviaConvertingExpressionVisitor)));

            if (nodeArrayBounds != null) {
                ArrayRankSpecifierSyntax arrayRankSpecifierSyntax = await ConvertArrayBoundsAsync(nodeArrayBounds, withSizes);
                bounds = bounds.Insert(0, arrayRankSpecifierSyntax);
            }

            return bounds;
        }

        public async Task<ArrayRankSpecifierSyntax> ConvertArrayBoundsAsync(ArgumentListSyntax nodeArrayBounds, bool withSizes = true)
        {
            SeparatedSyntaxList<VBSyntax.ArgumentSyntax> arguments = nodeArrayBounds.Arguments;
            var sizesSpecified = arguments.Any(a => !a.IsOmitted);
            var rank = arguments.Count;
            if (!sizesSpecified) rank += 1;

            var convertedArrayBounds = withSizes && sizesSpecified ? await ConvertArrayBoundsAsync(arguments)
                : Enumerable.Repeat(SyntaxFactory.OmittedArraySizeExpression(), rank);
            var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(
                SyntaxFactory.SeparatedList(
                    convertedArrayBounds));
            return arrayRankSpecifierSyntax;
        }

        private async Task<IEnumerable<ExpressionSyntax>> ConvertArrayBoundsAsync(SeparatedSyntaxList<VBSyntax.ArgumentSyntax> arguments)
        {
            return await arguments.SelectAsync(a => {
                VBSyntax.ExpressionSyntax upperBoundExpression = a is VBSyntax.SimpleArgumentSyntax sas ? sas.Expression
                    : a is VBSyntax.RangeArgumentSyntax ras ? ras.UpperBound
                    : throw new ArgumentOutOfRangeException(nameof(a), a, null);

                return IncreaseArrayUpperBoundExpressionAsync(upperBoundExpression);
            });
        }

        private async Task<ExpressionSyntax> IncreaseArrayUpperBoundExpressionAsync(VBSyntax.ExpressionSyntax expr)
        {
            var op = _semanticModel.GetOperation(expr);
            var constant = op.ConstantValue;
            if (constant.HasValue && constant.Value is int)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)constant.Value + 1));

            var convertedExpression = (ExpressionSyntax)await expr.AcceptAsync(TriviaConvertingExpressionVisitor);

            if (op is IBinaryOperation bOp && bOp.OperatorKind == BinaryOperatorKind.Subtract &&
                bOp.RightOperand.ConstantValue.HasValue && bOp.RightOperand.ConstantValue.Value is int subtractedVal && subtractedVal == 1
                && convertedExpression.SkipIntoParens() is CSSyntax.BinaryExpressionSyntax bExp && bExp.IsKind(CSSyntaxKind.SubtractExpression))
                return bExp.Left;

            return SyntaxFactory.BinaryExpression(
                CSSyntaxKind.SubtractExpression,
                convertedExpression, SyntaxFactory.Token(CSSyntaxKind.PlusToken), SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        }

        public async Task<SyntaxList<CSSyntax.AttributeListSyntax>> ConvertAttributesAsync(SyntaxList<VBSyntax.AttributeListSyntax> attributeListSyntaxs)
        {
            return SyntaxFactory.List(await attributeListSyntaxs.SelectManyAsync(ConvertAttributeAsync));
        }

        public async Task<IEnumerable<CSSyntax.AttributeListSyntax>> ConvertAttributeAsync(VBSyntax.AttributeListSyntax attributeList)
        {
            // These attributes' semantic effects are expressed differently in CSharp.
            return await attributeList.Attributes.Where(a => !IsExtensionAttribute(a) && !IsOutAttribute(a))
                .SelectAsync(async a => (CSSyntax.AttributeListSyntax)await a.AcceptAsync(TriviaConvertingExpressionVisitor));
        }

        public static AttributeArgumentListSyntax CreateAttributeArgumentList(params AttributeArgumentSyntax[] attributeArgumentSyntaxs)
        {
            return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(attributeArgumentSyntaxs));
        }

        public static CSSyntax.LocalDeclarationStatementSyntax CreateLocalVariableDeclarationAndAssignment(string variableName, ExpressionSyntax initValue)
        {
            return SyntaxFactory.LocalDeclarationStatement(CreateVariableDeclarationAndAssignment(variableName, initValue));
        }

        public static VariableDeclarationSyntax CreateVariableDeclarationAndAssignment(string variableName,
            ExpressionSyntax initValue, TypeSyntax explicitType = null)
        {
            CSSyntax.VariableDeclaratorSyntax variableDeclaratorSyntax = CreateVariableDeclarator(variableName, initValue);
            var variableDeclarationSyntax = SyntaxFactory.VariableDeclaration(
                explicitType ?? ValidSyntaxFactory.VarType,
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

        public async Task<(string, ExpressionSyntax extraArg)> GetParameterizedPropertyAccessMethodAsync(IOperation operation)
        {
            if (operation is IPropertyReferenceOperation pro && pro.Arguments.Any() &&
                !VBasic.VisualBasicExtensions.IsDefault(pro.Property)) {
                var isSetter = pro.Parent.Kind == OperationKind.SimpleAssignment && pro.Parent.Children.First() == pro;
                var extraArg = isSetter
                    ? (ExpressionSyntax) await operation.Parent.Syntax.ChildNodes().ElementAt(1).AcceptAsync(TriviaConvertingExpressionVisitor)
                    : null;
                return (isSetter ? pro.Property.SetMethod.Name : pro.Property.GetMethod.Name, extraArg);
            }

            return (null, null);
        }

        public CSSyntax.IdentifierNameSyntax GetRetVariableNameOrNull(VBSyntax.MethodBlockBaseSyntax node)
        {
            if (!node.MustReturn()) return null;
            if (_semanticModel.GetDeclaredSymbol(node) is IMethodSymbol ms && ms.ReturnsVoidOrAsyncTask()) {
                return null;
            }
            

            bool assignsToMethodNameVariable = false;

            if (!node.Statements.IsEmpty()) {
                string methodName = GetMethodBlockBaseIdentifierForImplicitReturn(node).ValueText ?? "";
                Func<ISymbol, bool> equalsMethodName = s => s.IsKind(SymbolKind.Local) && s.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase);
                var flow = _semanticModel.AnalyzeDataFlow(node.Statements.First(), node.Statements.Last());

                if (flow.Succeeded) {
                    assignsToMethodNameVariable = flow.ReadInside.Any(equalsMethodName) || flow.WrittenInside.Any(equalsMethodName);
                }
            }

            CSSyntax.IdentifierNameSyntax csReturnVariable = null;

            if (assignsToMethodNameVariable) {
                // In VB, assigning to the method name implicitly creates a variable that is returned when the method exits
                var csReturnVariableName =
                    ConvertIdentifier(GetMethodBlockBaseIdentifierForImplicitReturn(node)).ValueText + "Ret";
                csReturnVariable = SyntaxFactory.IdentifierName(csReturnVariableName);
            }

            return csReturnVariable;
        }

        public static SyntaxToken GetMethodBlockBaseIdentifierForImplicitReturn(SyntaxNode vbMethodBlock)
        {
            if (vbMethodBlock.Parent is VBSyntax.PropertyBlockSyntax pb) {
                return pb.PropertyStatement.Identifier;
            } else if (vbMethodBlock is VBSyntax.MethodBlockSyntax mb) {
                return mb.SubOrFunctionStatement.Identifier;
            } else {
                throw new NotImplementedException("MethodBlockBaseIdentifier " + VisualBasicExtensions.Kind(vbMethodBlock).ToString());
            }
        }

        public static bool IsDefaultIndexer(SyntaxNode node)
        {
            return node is VBSyntax.PropertyStatementSyntax pss && pss.Modifiers.Any(m => SyntaxTokenExtensions.IsKind(m, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DefaultKeyword));
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

        public ISymbol GetDeclaredCsOriginalSymbolOrNull(VBasic.VisualBasicSyntaxNode node)
        {
            var declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
            return declaredSymbol != null ? GetCsOriginalSymbolOrNull(declaredSymbol) : null;
        }

        public ISymbol GetCsOriginalSymbolOrNull(ISymbol symbol)
        {
            if (symbol == null) return null;
            symbol = symbol.OriginalDefinition;
            // Construct throws an exception if ConstructedFrom differs from it, so let's use ConstructedFrom directly
            var symbolToFind = symbol is IMethodSymbol m ? m.ConstructedFrom : symbol;
            var similarSymbol = SymbolFinder.FindSimilarSymbols(symbolToFind, _csCompilation).FirstOrDefault();
            return similarSymbol;
        }

        public static ExpressionSyntax ThrowawayParameters(ExpressionSyntax invocable, int paramCount)
        {
            var names = Enumerable.Range(1, paramCount).Select<int, string>(i =>
                                            new string(Enumerable.Repeat('_', i).ToArray())
                                        ).ToArray();
            var parameters = CreateParameterList(names.Select(n => SyntaxFactory.Parameter(SyntaxFactory.Identifier(n))));
            return SyntaxFactory.ParenthesizedLambdaExpression(parameters, SyntaxFactory.InvocationExpression(invocable));
        }

        public static CSSyntax.ParameterListSyntax CreateParameterList(IEnumerable<SyntaxNode> ps)
        {
            return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(ps));
        }

        public static CSSyntax.BinaryExpressionSyntax NotNothingComparison(ExpressionSyntax otherArgument, bool isReferenceType)
        {
            if (isReferenceType) {
                // When we upgrade the CodeAnalysis version, we'll be able to use a RecursivePattern of "{}" instead
                return SyntaxFactory.BinaryExpression(CSSyntaxKind.IsExpression, otherArgument, ValidSyntaxFactory.ObjectType);
            }
            return SyntaxFactory.BinaryExpression(CSSyntaxKind.NotEqualsExpression, otherArgument, ValidSyntaxFactory.DefaultExpression);
        }

        public static ExpressionSyntax NothingComparison(ExpressionSyntax otherArgument, bool isReferenceType)
        {
            if (isReferenceType) {
                return SyntaxFactory.IsPatternExpression(otherArgument,
                    SyntaxFactory.ConstantPattern(ValidSyntaxFactory.NullExpression));
            }

            return SyntaxFactory.BinaryExpression(CSSyntaxKind.EqualsExpression, otherArgument, ValidSyntaxFactory.DefaultExpression);
        }
    }
}