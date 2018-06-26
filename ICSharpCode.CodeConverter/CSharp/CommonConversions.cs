using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommonConversions
    {
        private readonly SemanticModel _semanticModel;
        private readonly VisualBasicSyntaxVisitor<CSharpSyntaxNode> _nodesVisitor;

        public CommonConversions(SemanticModel semanticModel, VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor)
        {
            _semanticModel = semanticModel;
            _nodesVisitor = nodesVisitor;
        }

        public Dictionary<string, VariableDeclarationSyntax> SplitVariableDeclarations(
            VariableDeclaratorSyntax declarator)
        {
            var rawType = ConvertDeclaratorType(declarator);
            var initializer = ConvertInitializer(declarator);

            var newDecls = new Dictionary<string, VariableDeclarationSyntax>();

            foreach (var name in declarator.Names) {
                var (type, adjustedInitializer) = AdjustFromName(rawType, name, initializer);
                var equalsValueClauseSyntax = adjustedInitializer == null ? null : SyntaxFactory.EqualsValueClause(adjustedInitializer);
                var v = SyntaxFactory.VariableDeclarator(ConvertIdentifier(name.Identifier), null, equalsValueClauseSyntax);
                string k = type.ToString();
                if (newDecls.TryGetValue(k, out var decl))
                    newDecls[k] = decl.AddVariables(v);
                else
                    newDecls[k] = SyntaxFactory.VariableDeclaration(type, SyntaxFactory.SingletonSeparatedList(v));
            }

            return newDecls;
        }

        private TypeSyntax ConvertDeclaratorType(VariableDeclaratorSyntax declarator)
        {
            return (TypeSyntax) declarator.AsClause?.TypeSwitch(
                       (SimpleAsClauseSyntax c) => c.Type,
                       (AsNewClauseSyntax c) => c.NewExpression.Type(),
                       _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
                   )?.Accept(_nodesVisitor) ?? SyntaxFactory.ParseTypeName("var");
        }

        private ExpressionSyntax ConvertInitializer(VariableDeclaratorSyntax declarator)
        {
            return (ExpressionSyntax)declarator.AsClause?.TypeSwitch(
                       (SimpleAsClauseSyntax _) => declarator.Initializer?.Value,
                       (AsNewClauseSyntax c) => c.NewExpression
                   )?.Accept(_nodesVisitor) ?? (ExpressionSyntax)declarator.Initializer?.Value.Accept(_nodesVisitor);
        }

        private (TypeSyntax, ExpressionSyntax) AdjustFromName(TypeSyntax rawType,
            ModifiedIdentifierSyntax name, ExpressionSyntax initializer)
        {
            var type = rawType;
            if (!SyntaxTokenExtensions.IsKind(name.Nullable, SyntaxKind.None))
            {
                if (type is ArrayTypeSyntax)
                {
                    type = ((ArrayTypeSyntax) type).WithElementType(
                        SyntaxFactory.NullableType(((ArrayTypeSyntax) type).ElementType));
                    initializer = null;
                }
                else
                    type = SyntaxFactory.NullableType(type);
            }

            var rankSpecifiers = ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds, false);
            if (rankSpecifiers.Count > 0)
            {
                var rankSpecifiersWithSizes = ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers,
                    name.ArrayBounds);
                if (!rankSpecifiersWithSizes.SelectMany(ars => ars.Sizes).OfType<OmittedArraySizeExpressionSyntax>().Any())
                {
                    initializer =
                        SyntaxFactory.ArrayCreationExpression(
                            SyntaxFactory.ArrayType(type, rankSpecifiersWithSizes));
                }

                type = SyntaxFactory.ArrayType(type, rankSpecifiers);
            }

            return (type, initializer);
        }

        public ExpressionSyntax Literal(object o, string valueText = null) => GetLiteralExpression(o, valueText);

        internal ExpressionSyntax GetLiteralExpression(object value, string valueText = null)
        {
            if (value is string s) {
                valueText = GetStringValueText(s, valueText);
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(valueText, s));
            }

            if (value == null)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NullLiteralExpression);
            if (value is bool)
                return SyntaxFactory.LiteralExpression((bool)value ? Microsoft.CodeAnalysis.CSharp.SyntaxKind.TrueLiteralExpression : Microsoft.CodeAnalysis.CSharp.SyntaxKind.FalseLiteralExpression);

            valueText = valueText != null ? ConvertNumericLiteralValueText(valueText, value) : value.ToString();

            if (value is byte)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (byte)value));
            if (value is sbyte)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (sbyte)value));
            if (value is short)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (short)value));
            if (value is ushort)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (ushort)value));
            if (value is int)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (int)value));
            if (value is uint)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (uint)value));
            if (value is long)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (long)value));
            if (value is ulong)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (ulong)value));

            if (value is float)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (float)value));
            if (value is double) {
                // Important to use value text, otherwise "10.0" gets coerced to and integer literal of 10 which can change semantics
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (double)value));
            }
            if (value is decimal) {
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (decimal)value));
            }

            if (value is char)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));


            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }

        internal string GetStringValueText(string s1, string valueText)
        {
            var worthBeingAVerbatimString = IsWorthBeingAVerbatimString(s1);
            if (worthBeingAVerbatimString)
            {
                var valueWithReplacements = s1.Replace("\"", "\"\"");
                return $"@\"{valueWithReplacements}\"";
            } 

            return "\"" + valueText.Substring(1, valueText.Length - 2).Replace("\"\"", "\\\"") + "\"";
        }

        public bool IsWorthBeingAVerbatimString(string s1)
        {
            return s1.IndexOfAny(new[] {'\r', '\n', '\\'}) > -1;
        }

        /// <summary>
        ///  https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/data-types/type-characters
        //   https://stackoverflow.com/a/166762/1128762
        /// </summary>
        private string ConvertNumericLiteralValueText(string valueText, object value)
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
            var longestMatchingReplacement = replacements.Where(t => valueText.EndsWith(t.Key, StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.Key.Length).OrderByDescending(g => g.Key).FirstOrDefault()?.SingleOrDefault();

            if (longestMatchingReplacement != null) {
                valueText = valueText.ReplaceEnd(longestMatchingReplacement.Value);
            }

            if (valueText.Length <= 2 || !valueText.StartsWith("&")) return valueText;

            if (valueText.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
            {
                return "0x" + valueText.Substring(2).Replace("M", "D"); // Undo any accidental replacements that assumed this was a decimal
            }

            if (valueText.StartsWith("&B", StringComparison.OrdinalIgnoreCase))
            {
                return "0b" + valueText.Substring(2);
            }

            // Octal or something unknown that can't be represented with C# literals
            return value.ToString();
        }

        public SyntaxToken ConvertIdentifier(SyntaxToken id, bool isAttribute = false)
        {
            string text = id.ValueText;
            var keywordKind = SyntaxFacts.GetKeywordKind(text);
            if (keywordKind != Microsoft.CodeAnalysis.CSharp.SyntaxKind.None)
                return SyntaxFactory.Identifier("@" + text);
            if (id.SyntaxTree == _semanticModel.SyntaxTree) {
                var symbol = _semanticModel.GetSymbolInfo(id.Parent).Symbol;
                if (symbol != null && !String.IsNullOrWhiteSpace(symbol.Name)) {
                    if (symbol.IsConstructor() && isAttribute) {
                        text = symbol.ContainingType.Name;
                        if (text.EndsWith("Attribute", StringComparison.Ordinal))
                            text = text.Remove(text.Length - "Attribute".Length);
                    } else if (symbol.IsKind(SymbolKind.Parameter) && symbol.Name == "Value") {
                        text = "value";
                    } else if (text.StartsWith("_", StringComparison.Ordinal) && symbol is IFieldSymbol fieldSymbol && fieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Property) == true) {
                        text = fieldSymbol.AssociatedSymbol.Name;
                    }
                }
            }
            return SyntaxFactory.Identifier(text);
        }

        public SyntaxTokenList ConvertModifiers(IEnumerable<SyntaxToken> modifiers, TokenContext context = TokenContext.Global, bool isVariableOrConst = false)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context, isVariableOrConst).Where(t => CSharpExtensions.Kind(t) != Microsoft.CodeAnalysis.CSharp.SyntaxKind.None));
        }

        private SyntaxToken? ConvertModifier(SyntaxToken m, TokenContext context = TokenContext.Global)
        {
            SyntaxKind vbSyntaxKind = VisualBasicExtensions.Kind(m);
            switch (vbSyntaxKind) {
                case SyntaxKind.DateKeyword:
                    return SyntaxFactory.Identifier("DateTime");
            }
            var token = vbSyntaxKind.ConvertToken(context);
            return token == Microsoft.CodeAnalysis.CSharp.SyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        private IEnumerable<SyntaxToken> ConvertModifiersCore(IEnumerable<SyntaxToken> modifiers, TokenContext context, bool isVariableOrConst = false)
        {
            var contextsWithIdenticalDefaults = new[] {TokenContext.Global, TokenContext.Local, TokenContext.InterfaceOrModule, TokenContext.MemberInInterface };
            if (!contextsWithIdenticalDefaults.Contains(context)) {
                bool visibility = false;
                foreach (var token in modifiers) {
                    if (SyntaxTokenExtensions.IsVbVisibility(token, isVariableOrConst)) {
                        visibility = true;
                        break;
                    }
                }
                if (!visibility)
                    yield return VisualBasicDefaultVisibility(context, isVariableOrConst);
            }
            foreach (var token in modifiers.Where(m => !IgnoreInContext(m, context)).OrderBy(m => SyntaxTokenExtensions.IsKind(m, SyntaxKind.PartialKeyword))) {
                var m = ConvertModifier(token, context);
                if (m.HasValue) yield return m.Value;
            }
            if (context == TokenContext.MemberInModule &&
                    !modifiers.Any(a => VisualBasicExtensions.Kind(a) == SyntaxKind.ConstKeyword ))
                yield return SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword);
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
            bool isConvOp= token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExplicitKeyword, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ImplicitKeyword)
                           ||token.IsKind(SyntaxKind.NarrowingKeyword, SyntaxKind.WideningKeyword);
            return isConvOp;
        }

        private SyntaxToken VisualBasicDefaultVisibility(TokenContext context, bool isVariableOrConst)
        {
            switch (context) {
                case TokenContext.Global:
                case TokenContext.InterfaceOrModule:
                    return SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword);
                case TokenContext.MemberInModule:
                case TokenContext.MemberInClass:
                case TokenContext.MemberInInterface:
                    return SyntaxFactory.Token(isVariableOrConst
                        ? Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword
                        : Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword);
                case TokenContext.MemberInStruct:
                    return SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword);
                case TokenContext.Local:
                    return SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword);
            }
            throw new ArgumentOutOfRangeException(nameof(context), context, "Specified argument was out of the range of valid values.");
        }

        internal SyntaxList<ArrayRankSpecifierSyntax> ConvertArrayRankSpecifierSyntaxes(
            SyntaxList<Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayRankSpecifierSyntax> arrayRankSpecifierSyntaxs,
            ArgumentListSyntax nodeArrayBounds, bool withSizes = true)
        {
            var bounds = SyntaxFactory.List(arrayRankSpecifierSyntaxs.Select(r => (ArrayRankSpecifierSyntax)r.Accept(_nodesVisitor)));

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
            return argumentListSyntax.Arguments.Select(a => IncreaseArrayUpperBoundExpression(((SimpleArgumentSyntax)a).Expression));
        }

        private ExpressionSyntax IncreaseArrayUpperBoundExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax expr)
        {
            var constant = _semanticModel.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is int)
                return SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)constant.Value + 1));

            return SyntaxFactory.BinaryExpression(
                Microsoft.CodeAnalysis.CSharp.SyntaxKind.SubtractExpression,
                (ExpressionSyntax)expr.Accept(_nodesVisitor), SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PlusToken), SyntaxFactory.LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        }

        public static AttributeArgumentListSyntax CreateAttributeArgumentList(params AttributeArgumentSyntax[] attributeArgumentSyntaxs)
        {
            return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(attributeArgumentSyntaxs));
        }
    }
}