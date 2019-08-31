using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommonConversions
    {
        private static readonly Type ExtensionAttributeType = typeof(ExtensionAttribute);
        private static readonly Type OutAttributeType = typeof(OutAttribute);
        private readonly SemanticModel _semanticModel;
        private readonly SyntaxGenerator _csSyntaxGenerator;
        private readonly CSharpCompilation _csCompilation;
        public CommentConvertingVisitorWrapper<CSharpSyntaxNode> TriviaConvertingExpressionVisitor { get; set; }
        public TypeConversionAnalyzer TypeConversionAnalyzer { get; }

        public CommonConversions(SemanticModel semanticModel,
            TypeConversionAnalyzer typeConversionAnalyzer, SyntaxGenerator csSyntaxGenerator,
            CSharpCompilation csCompilation)
        {
            TypeConversionAnalyzer = typeConversionAnalyzer;
            _semanticModel = semanticModel;
            _csSyntaxGenerator = csSyntaxGenerator;
            _csCompilation = csCompilation;
        }

        public Dictionary<string, VariableDeclarationSyntax> SplitVariableDeclarations(
            VariableDeclaratorSyntax declarator, bool preferExplicitType = false)
        {
            var useExplicitType = declarator.AsClause != null || declarator.Initializer == null || preferExplicitType;
            var initializer = ConvertInitializer(declarator);

            var newDecls = new Dictionary<string, VariableDeclarationSyntax>();

            foreach (var name in declarator.Names) {
                var typeSymbol = _semanticModel.GetDeclaredSymbol(name).GetSymbolType();
                var csTypeSyntax = (TypeSyntax)_csSyntaxGenerator.TypeExpression(typeSymbol);
                var adjustedInitializer = GetInitializerFromNameAndType(typeSymbol, name, initializer);

                bool isField = declarator.Parent.IsKind(SyntaxKind.FieldDeclaration);
                EqualsValueClauseSyntax equalsValueClauseSyntax;
                if (adjustedInitializer != null) {
                    var vbInitializer = declarator.Initializer?.Value;
                    // Explicit conversions are never needed for AsClause, since the type is inferred from the RHS
                    var convertedInitializer = vbInitializer == null ? adjustedInitializer : TypeConversionAnalyzer.AddExplicitConversion(vbInitializer, adjustedInitializer);
                    equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(convertedInitializer);
                } else if (isField || _semanticModel.IsDefinitelyAssignedBeforeRead(declarator, name)) {
                    equalsValueClauseSyntax = null;
                } else {
                    // VB initializes variables to their default
                    equalsValueClauseSyntax = SyntaxFactory.EqualsValueClause(SyntaxFactory.DefaultExpression(csTypeSyntax));
                }

                var v = SyntaxFactory.VariableDeclarator(ConvertIdentifier(name.Identifier), null, equalsValueClauseSyntax);
                string k = typeSymbol.GetFullMetadataName();
                if (newDecls.TryGetValue(k, out var decl))
                    newDecls[k] = decl.AddVariables(v);
                else {
                    var csTypeOrVar = GetTypeSyntax(typeSymbol, !useExplicitType);
                    newDecls[k] = SyntaxFactory.VariableDeclaration(csTypeOrVar, SyntaxFactory.SingletonSeparatedList(v));
                }
            }

            return newDecls;
        }

        private TypeSyntax GetTypeSyntax(VariableDeclaratorSyntax declarator, bool useImplicitType)
        {
            if (useImplicitType) return CreateVarTypeName();

            var typeInf = _semanticModel.GetTypeInfo(declarator.Initializer.Value);
            if (typeInf.ConvertedType == null) return CreateVarTypeName();

            return GetTypeSyntax(typeInf.ConvertedType);
        }

        public TypeSyntax GetTypeSyntax(ITypeSymbol typeSymbol, bool useImplicitType = false)
        {
            if (useImplicitType || typeSymbol == null) return CreateVarTypeName();
            return (TypeSyntax) _csSyntaxGenerator.TypeExpression(typeSymbol);
        }

        private static TypeSyntax CreateVarTypeName()
        {
            return SyntaxFactory.ParseTypeName("var");
        }

        private ExpressionSyntax ConvertInitializer(VariableDeclaratorSyntax declarator)
        {
            return (ExpressionSyntax)declarator.AsClause?.TypeSwitch(
                       (SimpleAsClauseSyntax _) => declarator.Initializer?.Value,
                       (AsNewClauseSyntax c) => c.NewExpression
                   )?.Accept(TriviaConvertingExpressionVisitor) ?? (ExpressionSyntax)declarator.Initializer?.Value.Accept(TriviaConvertingExpressionVisitor);
        }

        private ExpressionSyntax GetInitializerFromNameAndType(ITypeSymbol typeSymbol,
            ModifiedIdentifierSyntax name, ExpressionSyntax initializer)
        {
            if (!SyntaxTokenExtensions.IsKind(name.Nullable, SyntaxKind.None))
            {
                if (typeSymbol.IsArrayType())
                {
                    initializer = null;
                }
            }

            var rankSpecifiers = ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds, false);
            if (rankSpecifiers.Count > 0)
            {
                var rankSpecifiersWithSizes = ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds);
                if (!rankSpecifiersWithSizes.SelectMany(ars => ars.Sizes).OfType<OmittedArraySizeExpressionSyntax>().Any())
                {
                    var arrayTypeSyntax = (ArrayTypeSyntax) _csSyntaxGenerator.TypeExpression(typeSymbol);
                    arrayTypeSyntax = arrayTypeSyntax.WithRankSpecifiers(rankSpecifiersWithSizes);
                    initializer = SyntaxFactory.ArrayCreationExpression(arrayTypeSyntax);
                }
            }

            return initializer;
        }

        public ExpressionSyntax Literal(object o, string textForUser = null) => GetLiteralExpression(o, textForUser);

        internal ExpressionSyntax GetLiteralExpression(object value, string textForUser = null)
        {
            if (value is string valueTextForCompiler) {
                textForUser = GetQuotedStringTextForUser(textForUser, valueTextForCompiler);
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(textForUser, valueTextForCompiler));
            }

            if (value == null)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NullLiteralExpression);
            if (value is bool)
                return SyntaxFactory.LiteralExpression((bool)value ? CSSyntaxKind.TrueLiteralExpression : CSSyntaxKind.FalseLiteralExpression);

            textForUser = textForUser != null ? ConvertNumericLiteralValueText(textForUser, value) : value.ToString();

            if (value is byte)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (byte)value));
            if (value is sbyte)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (sbyte)value));
            if (value is short)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (short)value));
            if (value is ushort)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (ushort)value));
            if (value is int)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (int)value));
            if (value is uint)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (uint)value));
            if (value is long)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (long)value));
            if (value is ulong)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (ulong)value));

            if (value is float)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (float)value));
            if (value is double) {
                // Important to use value text, otherwise "10.0" gets coerced to and integer literal of 10 which can change semantics
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (double)value));
            }
            if (value is decimal) {
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, (decimal)value));
            }

            if (value is char)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));

            if (value is DateTime dt) {
                var valueToOutput = dt.Date.Equals(dt) ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm:ss");
                return SyntaxFactory.ParseExpression("DateTime.Parse(\"" + valueToOutput + "\")");
            }


            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }

        internal string GetQuotedStringTextForUser(string textForUser, string valueTextForCompiler)
        {
            var sourceUnquotedTextForUser = Unquote(textForUser);
            var worthBeingAVerbatimString = IsWorthBeingAVerbatimString(valueTextForCompiler);
            var destQuotedTextForUser =
                $"\"{EscapeQuotes(sourceUnquotedTextForUser, valueTextForCompiler, worthBeingAVerbatimString)}\"";
            
            return worthBeingAVerbatimString ? "@" + destQuotedTextForUser : destQuotedTextForUser;

        }

        internal string EscapeQuotes(string unquotedTextForUser, string valueTextForCompiler, bool isVerbatimString)
        {
            if (isVerbatimString) {
                return valueTextForCompiler.Replace("\"", "\"\"");
            } else {
                return unquotedTextForUser.Replace("\"\"", "\\\"");
            }
        }

        private static string Unquote(string quotedText)
        {
            int firstQuoteIndex = quotedText.IndexOf("\"");
            int lastQuoteIndex = quotedText.LastIndexOf("\"");
            var unquoted = quotedText.Substring(firstQuoteIndex + 1, lastQuoteIndex - firstQuoteIndex - 1);
            return unquoted;
        }

        public bool IsWorthBeingAVerbatimString(string s1)
        {
            return s1.IndexOfAny(new[] {'\r', '\n', '\\'}) > -1;
        }

        /// <summary>
        ///  https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/data-types/type-characters
        ///  https://stackoverflow.com/a/166762/1128762
        /// </summary>
        private string ConvertNumericLiteralValueText(string textForUser, object value)
        {
            var replacements = new Dictionary<string, string> {
                {"C", ""},
                {"I", ""},
                {"%", ""},
                {"UI", "U"},
                {"S", ""},
                {"US", ""},
                {"UL", "UL"},
                {"D", "M"},
                {"@", "M"},
                {"R", "D"},
                {"#", "D"},
                {"F", "F"}, // Normalizes casing
                {"!", "F"},
                {"L", "L"}, // Normalizes casing
                {"&", "L"},
            };
            // Be careful not to replace only the "S" in "US" for example
            var longestMatchingReplacement = replacements.Where(t => textForUser.EndsWith(t.Key, StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.Key.Length).OrderByDescending(g => g.Key).FirstOrDefault()?.SingleOrDefault();

            if (longestMatchingReplacement != null) {
                textForUser = textForUser.ReplaceEnd(longestMatchingReplacement.Value);
            }

            if (textForUser.Length <= 2 || !textForUser.StartsWith("&")) return textForUser;

            if (textForUser.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
            {
                return "0x" + textForUser.Substring(2).Replace("M", "D"); // Undo any accidental replacements that assumed this was a decimal
            }

            if (textForUser.StartsWith("&B", StringComparison.OrdinalIgnoreCase))
            {
                return "0b" + textForUser.Substring(2);
            }

            // Octal or something unknown that can't be represented with C# literals
            return value.ToString();
        }

        public SyntaxToken ConvertIdentifier(SyntaxToken id, bool isAttribute = false)
        {
            string text = id.ValueText;
            
            if (id.SyntaxTree == _semanticModel.SyntaxTree) {
                var symbol = _semanticModel.GetSymbolInfo(id.Parent).Symbol;
                if (symbol != null && !String.IsNullOrWhiteSpace(symbol.Name)) {
                    if (text.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase)) {
                        text = symbol.Name;
                    }

                    if (symbol.IsConstructor() && isAttribute) {
                        text = symbol.ContainingType.Name;
                        if (text.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
                            text = text.Remove(text.Length - "Attribute".Length);
                    } else if (symbol.IsKind(SymbolKind.Parameter) && symbol.ContainingSymbol.IsAccessorPropertySet() && ((symbol.IsImplicitlyDeclared && symbol.Name == "Value") || symbol.ContainingSymbol.GetParameters().FirstOrDefault(x => !x.IsImplicitlyDeclared) == symbol)) {
                        // The case above is basically that if the symbol is a parameter, and the corresponding definition is a property set definition 
                        // AND the first explicitly declared parameter is this symbol, we need to replace it with value.
                        text = "value";
                    } else if (text.StartsWith("_", StringComparison.OrdinalIgnoreCase) && symbol is IFieldSymbol propertyFieldSymbol && propertyFieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Property) == true) {
                        text = propertyFieldSymbol.AssociatedSymbol.Name;
                    } else if (text.EndsWith("Event", StringComparison.OrdinalIgnoreCase) && symbol is IFieldSymbol eventFieldSymbol && eventFieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Event) == true) {
                        text = eventFieldSymbol.AssociatedSymbol.Name;
                    } else if (MustInlinePropertyWithEventsAccess(id.Parent, symbol)) {
                        // For C# Winforms designer, we need to use direct field access (and inline any event handlers)
                        text = "_" + text;
                    }
                }
            }

            return CsEscapedIdentifier(text);
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
            TokenContext context = TokenContext.Global, bool isVariableOrConst = false, bool isConstructor = false)
        {
            ISymbol declaredSymbol = _semanticModel.GetDeclaredSymbol(node);
            var declaredAccessibility = declaredSymbol.DeclaredAccessibility;

            var contextsWithIdenticalDefaults = new[] { TokenContext.Global, TokenContext.Local, TokenContext.InterfaceOrModule, TokenContext.MemberInInterface };
            bool isPartial = declaredSymbol.IsPartialClassDefinition() || declaredSymbol.IsPartialMethodDefinition() || declaredSymbol.IsPartialMethodImplementation();
            bool implicitVisibility = contextsWithIdenticalDefaults.Contains(context) || isVariableOrConst || isConstructor;
            if (implicitVisibility && !isPartial) declaredAccessibility = Accessibility.NotApplicable;
            return SyntaxFactory.TokenList(ConvertModifiersCore(declaredAccessibility, modifiers, context).Where(t => CSharpExtensions.Kind(t) != CSSyntaxKind.None));
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

            foreach (var token in remainingModifiers.Where(m => !IgnoreInContext(m, context)).OrderBy(m => SyntaxTokenExtensions.IsKind(m, SyntaxKind.PartialKeyword))) {
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

        internal SyntaxList<ArrayRankSpecifierSyntax> ConvertArrayRankSpecifierSyntaxes(
            SyntaxList<VBSyntax.ArrayRankSpecifierSyntax> arrayRankSpecifierSyntaxs,
            ArgumentListSyntax nodeArrayBounds, bool withSizes = true)
        {
            var bounds = SyntaxFactory.List(arrayRankSpecifierSyntaxs.Select(r => (ArrayRankSpecifierSyntax)r.Accept(TriviaConvertingExpressionVisitor)));

            if (nodeArrayBounds != null) {
                var sizesSpecified = nodeArrayBounds.Arguments.Any(a => !a.IsOmitted);
                var rank = nodeArrayBounds.Arguments.Count;
                if (!sizesSpecified) rank += 1;

                var convertedArrayBounds = withSizes && sizesSpecified ? ConvertArrayBounds(nodeArrayBounds)
                    : Enumerable.Repeat(SyntaxFactory.OmittedArraySizeExpression(), rank);
                var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SeparatedList(
                        convertedArrayBounds));
                bounds = bounds.Insert(0, arrayRankSpecifierSyntax);
            }

            return bounds;
        }

        public IEnumerable<ExpressionSyntax> ConvertArrayBounds(ArgumentListSyntax argumentListSyntax)
        {
            return argumentListSyntax.Arguments.Select(a => {
                VBSyntax.ExpressionSyntax upperBoundExpression = a is SimpleArgumentSyntax sas ? sas.Expression
                    : a is RangeArgumentSyntax ras ? ras.UpperBound
                    : throw new ArgumentOutOfRangeException(nameof(a), a, null);

                return IncreaseArrayUpperBoundExpression(upperBoundExpression);
            });
        }

        private ExpressionSyntax IncreaseArrayUpperBoundExpression(VBSyntax.ExpressionSyntax expr)
        {
            var constant = _semanticModel.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is int)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)constant.Value + 1));

            return SyntaxFactory.BinaryExpression(
                CSSyntaxKind.SubtractExpression,
                (ExpressionSyntax)expr.Accept(TriviaConvertingExpressionVisitor), SyntaxFactory.Token(CSSyntaxKind.PlusToken), SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
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

        public string GetParameterizedPropertyAccessMethod(IOperation operation, out ExpressionSyntax extraArg)
        {
            if (operation is IPropertyReferenceOperation pro && pro.Arguments.Any() &&
                !pro.Property.IsDefault()) {
                var isSetter = pro.Parent.Kind == OperationKind.SimpleAssignment && pro.Parent.Children.First() == pro;
                extraArg = isSetter
                    ? (ExpressionSyntax)TriviaConvertingExpressionVisitor.Visit(operation.Parent.Syntax.ChildNodes().ElementAt(1))
                    : null;
                return isSetter ? pro.Property.SetMethod.Name : pro.Property.GetMethod.Name;
            }

            extraArg = null;
            return null;
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