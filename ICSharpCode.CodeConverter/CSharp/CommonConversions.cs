using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CommonConversions
    {
        private readonly SemanticModel _semanticModel;

        public CommonConversions(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public Dictionary<string, VariableDeclarationSyntax> SplitVariableDeclarations(
            Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax declarator, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor,
            bool isWithEvents = false)
        {
            var rawType = ConvertDeclaratorType(nodesVisitor, declarator);
            var initializer = ConvertInitializer(nodesVisitor, declarator);

            var newDecls = new Dictionary<string, VariableDeclarationSyntax>();

            foreach (var name in declarator.Names) {
                var (type, adjustedInitializer) = AdjustFromName(nodesVisitor, rawType, name, initializer);
                var v = SyntaxFactory.VariableDeclarator(ConvertIdentifier(name.Identifier), null, adjustedInitializer == null ? null : SyntaxFactory.EqualsValueClause(adjustedInitializer));
                string k = type.ToString();
                if (newDecls.TryGetValue(k, out var decl))
                    newDecls[k] = decl.AddVariables(v);
                else
                    newDecls[k] = SyntaxFactory.VariableDeclaration(type, SyntaxFactory.SingletonSeparatedList(v));
            }

            return newDecls;
        }

        private TypeSyntax ConvertDeclaratorType(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor,
            Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax declarator)
        {
            return (TypeSyntax) declarator.AsClause?.TypeSwitch(
                       (Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax c) => c.Type,
                       (Microsoft.CodeAnalysis.VisualBasic.Syntax.AsNewClauseSyntax c) => Microsoft.CodeAnalysis.VisualBasic.SyntaxExtensions.Type(c.NewExpression),
                       _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
                   )?.Accept(nodesVisitor) ?? SyntaxFactory.ParseTypeName("var");
        }

        private ExpressionSyntax ConvertInitializer(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor,
            Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax declarator)
        {
            return (ExpressionSyntax)declarator.AsClause?.TypeSwitch(
                       (Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleAsClauseSyntax _) => declarator.Initializer?.Value,
                       (Microsoft.CodeAnalysis.VisualBasic.Syntax.AsNewClauseSyntax c) => c.NewExpression
                   )?.Accept(nodesVisitor) ?? (ExpressionSyntax)declarator.Initializer?.Value.Accept(nodesVisitor);
        }

        private (TypeSyntax, ExpressionSyntax) AdjustFromName(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor, TypeSyntax rawType,
            Microsoft.CodeAnalysis.VisualBasic.Syntax.ModifiedIdentifierSyntax name, ExpressionSyntax initializer)
        {
            var type = rawType;
            if (!SyntaxTokenExtensions.IsKind(name.Nullable, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.None))
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

            var rankSpecifiers = ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds,
                nodesVisitor, false);
            if (rankSpecifiers.Count > 0)
            {
                var rankSpecifiersWithSizes = ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers,
                    name.ArrayBounds, nodesVisitor);
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
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(valueText, s));
            }

            if (value == null)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
            if (value is bool)
                return SyntaxFactory.LiteralExpression((bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

            valueText = valueText != null ? ConvertNumericLiteralValueText(valueText, value) : value.ToString();

            if (value is byte)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (byte)value));
            if (value is sbyte)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (sbyte)value));
            if (value is short)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (short)value));
            if (value is ushort)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (ushort)value));
            if (value is int)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (int)value));
            if (value is uint)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (uint)value));
            if (value is long)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (long)value));
            if (value is ulong)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (ulong)value));

            if (value is float)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (float)value));
            if (value is double) {
                // Important to use value text, otherwise "10.0" gets coerced to and integer literal of 10 which can change semantics
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (double)value));
            }
            if (value is decimal) {
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (decimal)value));
            }

            if (value is char)
                return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));


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
                {"UI", "U"},
                {"S", ""},
                {"US", ""},
                {"UL", "UL"},
                {"D", "M"},
                {"R", "D"},
                {"F", "F"}, // Normalizes casing
                {"L", "L"}, // Normalizes casing
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

        public SyntaxToken ConvertIdentifier(SyntaxToken id, bool isAttribute = false, string prefix = "")
        {
            string text = prefix + id.ValueText;
            var keywordKind = SyntaxFacts.GetKeywordKind(text);
            if (keywordKind != SyntaxKind.None)
                return SyntaxFactory.Identifier("@" + text);
            if (id.SyntaxTree == _semanticModel.SyntaxTree) {
                var symbol = _semanticModel.GetSymbolInfo(id.Parent).Symbol;
                if (symbol != null && !String.IsNullOrWhiteSpace(symbol.Name)) {
                    if (symbol.IsConstructor() && isAttribute) {
                        text = symbol.ContainingType.Name;
                        if (text.EndsWith("Attribute", StringComparison.Ordinal))
                            text = text.Remove(text.Length - "Attribute".Length);
                    } else if (text.StartsWith("_", StringComparison.Ordinal) && symbol is IFieldSymbol fieldSymbol && fieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Property) == true) {

                        text = fieldSymbol.AssociatedSymbol.Name;
                    }
                }
            }
            return SyntaxFactory.Identifier(text);
        }

        public SyntaxTokenList ConvertModifiers(IEnumerable<SyntaxToken> modifiers, SyntaxKindExtensions.TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            return SyntaxFactory.TokenList((IEnumerable<SyntaxToken>) ConvertModifiersCore(modifiers, context));
        }

        public SyntaxTokenList ConvertModifiers(SyntaxTokenList modifiers, SyntaxKindExtensions.TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            return SyntaxFactory.TokenList(Enumerable.Where<SyntaxToken>(ConvertModifiersCore(modifiers, context), t => CSharpExtensions.Kind(t) != SyntaxKind.None));
        }

        private SyntaxToken? ConvertModifier(SyntaxToken m, SyntaxKindExtensions.TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            Microsoft.CodeAnalysis.VisualBasic.SyntaxKind vbSyntaxKind = Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind(m);
            switch (vbSyntaxKind) {
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DateKeyword:
                    return SyntaxFactory.Identifier("DateTime");
            }
            var token = SyntaxKindExtensions.ConvertToken(vbSyntaxKind, context);
            return token == SyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        private IEnumerable<SyntaxToken> ConvertModifiersCore(IEnumerable<SyntaxToken> modifiers, SyntaxKindExtensions.TokenContext context)
        {
            var contextsWithIdenticalDefaults = new[] {SyntaxKindExtensions.TokenContext.Global, SyntaxKindExtensions.TokenContext.Local, SyntaxKindExtensions.TokenContext.InterfaceOrModule, SyntaxKindExtensions.TokenContext.MemberInInterface };
            if (!contextsWithIdenticalDefaults.Contains(context)) {
                bool visibility = false;
                foreach (var token in modifiers) {
                    if (IsVisibility(token, context)) {
                        visibility = true;
                        break;
                    }
                }
                if (!visibility)
                    yield return VisualBasicDefaultVisibility(context);
            }
            foreach (var token in modifiers.Where(m => !IgnoreInContext(m, context)).OrderBy(m => SyntaxTokenExtensions.IsKind(m, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PartialKeyword))) {
                var m = ConvertModifier(token, context);
                if (m.HasValue) yield return m.Value;
            }
            if (context == SyntaxKindExtensions.TokenContext.MemberInModule)
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
        }

        private bool IgnoreInContext(SyntaxToken m, SyntaxKindExtensions.TokenContext context)
        {
            switch (Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind(m)) {
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.OptionalKeyword:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ByValKeyword:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IteratorKeyword:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DimKeyword:
                    return true;
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ReadOnlyKeyword:
                case Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.WriteOnlyKeyword:
                    return context == SyntaxKindExtensions.TokenContext.Member;
                default:
                    return false;
            }
        }

        public bool IsConversionOperator(SyntaxToken token)
        {
            bool isConvOp= token.IsKind(SyntaxKind.ExplicitKeyword, SyntaxKind.ImplicitKeyword)
                           ||token.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NarrowingKeyword, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.WideningKeyword);
            return isConvOp;
        }

        private bool IsVisibility(SyntaxToken token, SyntaxKindExtensions.TokenContext context)
        {
            return token.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PublicKeyword, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FriendKeyword, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ProtectedKeyword, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.PrivateKeyword)
                   || (context == SyntaxKindExtensions.TokenContext.VariableOrConst && SyntaxTokenExtensions.IsKind(token, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ConstKeyword));
        }

        private SyntaxToken VisualBasicDefaultVisibility(SyntaxKindExtensions.TokenContext context)
        {
            switch (context) {
                case SyntaxKindExtensions.TokenContext.Global:
                case SyntaxKindExtensions.TokenContext.InterfaceOrModule:
                    return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                case SyntaxKindExtensions.TokenContext.Member:
                case SyntaxKindExtensions.TokenContext.MemberInModule:
                case SyntaxKindExtensions.TokenContext.MemberInClass:
                case SyntaxKindExtensions.TokenContext.MemberInInterface:
                case SyntaxKindExtensions.TokenContext.MemberInStruct:
                    return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                case SyntaxKindExtensions.TokenContext.Local:
                case SyntaxKindExtensions.TokenContext.VariableOrConst:
                    return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
            }
            throw new ArgumentOutOfRangeException(nameof(context), context, "Specified argument was out of the range of valid values.");
        }

        internal SyntaxList<ArrayRankSpecifierSyntax> ConvertArrayRankSpecifierSyntaxes(
            SyntaxList<Microsoft.CodeAnalysis.VisualBasic.Syntax.ArrayRankSpecifierSyntax> arrayRankSpecifierSyntaxs,
            Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax nodeArrayBounds, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> commentConvertingNodesVisitor, bool withSizes = true)
        {
            var bounds = SyntaxFactory.List(arrayRankSpecifierSyntaxs.Select(r => (ArrayRankSpecifierSyntax)r.Accept(commentConvertingNodesVisitor)));

            if (nodeArrayBounds != null) {
                var sizesSpecified = nodeArrayBounds.Arguments.Any(a => !a.IsOmitted);
                var rank = nodeArrayBounds.Arguments.Count;
                if (!sizesSpecified) rank += 1;

                var convertedArrayBounds = withSizes && sizesSpecified ? ConvertArrayBounds(nodeArrayBounds, commentConvertingNodesVisitor)
                    : Enumerable.Repeat(SyntaxFactory.OmittedArraySizeExpression(), rank);
                var arrayRankSpecifierSyntax = SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.SeparatedList(
                        convertedArrayBounds));
                bounds = bounds.Insert(0, arrayRankSpecifierSyntax);
            }

            return bounds;
        }

        public IEnumerable<ExpressionSyntax> ConvertArrayBounds(Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax argumentListSyntax, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> commentConvertingNodesVisitor)
        {
            return argumentListSyntax.Arguments.Select(a => IncreaseArrayUpperBoundExpression(((Microsoft.CodeAnalysis.VisualBasic.Syntax.SimpleArgumentSyntax)a).Expression, commentConvertingNodesVisitor));
        }

        private ExpressionSyntax IncreaseArrayUpperBoundExpression(Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax expr, Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> commentConvertingNodesVisitor)
        {
            var constant = _semanticModel.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is int)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)constant.Value + 1));

            return SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                (ExpressionSyntax)expr.Accept(commentConvertingNodesVisitor), SyntaxFactory.Token(SyntaxKind.PlusToken), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        }
    }
}