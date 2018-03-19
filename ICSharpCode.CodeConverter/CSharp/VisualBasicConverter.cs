using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using ArrayRankSpecifierSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayRankSpecifierSyntax;
using ArrayTypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ArrayTypeSyntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using static ICSharpCode.CodeConverter.CSharp.SyntaxKindExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    public partial class VisualBasicConverter
    {
        public static CSharpSyntaxNode ConvertCompilationTree(VBasic.VisualBasicCompilation compilation, VBasic.VisualBasicSyntaxTree tree)
        {
            var visualBasicSyntaxVisitor = new VisualBasicConverter.NodesVisitor(compilation.GetSemanticModel(tree, true));
            return tree.GetRoot().Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }

        static Dictionary<string, VariableDeclarationSyntax> SplitVariableDeclarations(VBSyntax.VariableDeclaratorSyntax declarator, VBasic.VisualBasicSyntaxVisitor<CSharpSyntaxNode> nodesVisitor, SemanticModel semanticModel)
        {
            var rawType = (TypeSyntax)declarator.AsClause?.TypeSwitch(
                (VBSyntax.SimpleAsClauseSyntax c) => c.Type,
                (VBSyntax.AsNewClauseSyntax c) => VBasic.SyntaxExtensions.Type(c.NewExpression),
                _ => { throw new NotImplementedException($"{_.GetType().FullName} not implemented!"); }
            )?.Accept(nodesVisitor) ?? SyntaxFactory.ParseTypeName("var");

            var initializer = (ExpressionSyntax)declarator.AsClause?.TypeSwitch(
                (VBSyntax.SimpleAsClauseSyntax _) => declarator.Initializer?.Value,
                (VBSyntax.AsNewClauseSyntax c) => c.NewExpression
            )?.Accept(nodesVisitor) ?? (ExpressionSyntax)declarator.Initializer?.Value.Accept(nodesVisitor);

            var newDecls = new Dictionary<string, VariableDeclarationSyntax>();

            foreach (var name in declarator.Names) {
                var type = rawType;
                if (!SyntaxTokenExtensions.IsKind(name.Nullable, VBasic.SyntaxKind.None)) {
                    if (type is ArrayTypeSyntax) {
                        type = ((ArrayTypeSyntax)type).WithElementType(
                            SyntaxFactory.NullableType(((ArrayTypeSyntax)type).ElementType));
                        initializer = null;
                    } else
                        type = SyntaxFactory.NullableType(type);
                }

                var rankSpecifiers = NodesVisitor.ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds, nodesVisitor, semanticModel, false);
                if (rankSpecifiers.Count > 0) {
                    var rankSpecifiersWithSizes = NodesVisitor.ConvertArrayRankSpecifierSyntaxes(name.ArrayRankSpecifiers, name.ArrayBounds, nodesVisitor, semanticModel);
                    if (!rankSpecifiersWithSizes.SelectMany(ars => ars.Sizes).OfType<OmittedArraySizeExpressionSyntax>().Any())
                    {
                        initializer =
                            SyntaxFactory.ArrayCreationExpression(
                                SyntaxFactory.ArrayType(type, rankSpecifiersWithSizes));
                    }
                    type = SyntaxFactory.ArrayType(type, rankSpecifiers);
                }

                VariableDeclarationSyntax decl;
                var v = SyntaxFactory.VariableDeclarator(ConvertIdentifier(name.Identifier, semanticModel), null, initializer == null ? null : SyntaxFactory.EqualsValueClause(initializer));
                string k = type.ToString();
                if (newDecls.TryGetValue(k, out decl))
                    newDecls[k] = decl.AddVariables(v);
                else
                    newDecls[k] = SyntaxFactory.VariableDeclaration(type, SyntaxFactory.SingletonSeparatedList(v));
            }

            return newDecls;
        }

        static ExpressionSyntax Literal(object o, string valueText = null) => GetLiteralExpression(valueText ?? o.ToString(), o);

        internal static ExpressionSyntax GetLiteralExpression(string valueText, object value)
        {
            if (value is bool)
                return SyntaxFactory.LiteralExpression((bool)value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
            if (value is byte)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((byte)value));
            if (value is sbyte)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((sbyte)value));
            if (value is short)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((short)value));
            if (value is ushort)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ushort)value));
            if (value is int)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)value));
            if (value is uint)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((uint)value));
            if (value is long)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((long)value));
            if (value is ulong)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ulong)value));

            if (value is float)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((float)value));
            if (value is double) {
                // Important to use value text, otherwise "10.0" gets coerced to and integer literal of 10 which can change semantics
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(valueText, (double)value));
            }
            if (value is decimal) {
                // Don't use value text - it has a "D" in VB, but an "M" in C#
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((decimal)value));
            }

            if (value is char)
                return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));

            if (value is string)
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal((string)value));

            if (value == null)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }


        static SyntaxToken ConvertIdentifier(SyntaxToken id, SemanticModel semanticModel, bool isAttribute = false)
        {
            string text = id.ValueText;
            var keywordKind = SyntaxFacts.GetKeywordKind(text);
            if (keywordKind != SyntaxKind.None)
                return SyntaxFactory.Identifier("@" + text);
            if (id.SyntaxTree == semanticModel.SyntaxTree) {
                var symbol = semanticModel.GetSymbolInfo(id.Parent).Symbol;
                if (symbol != null && !String.IsNullOrWhiteSpace(symbol.Name)) {
                    if (symbol.IsConstructor() && isAttribute) {
                        text = symbol.ContainingType.Name;
                        if (text.EndsWith("Attribute", StringComparison.Ordinal))
                            text = text.Remove(text.Length - "Attribute".Length);
                    } else if (text.StartsWith("_", StringComparison.Ordinal) && symbol is IFieldSymbol fieldSymbol && fieldSymbol.AssociatedSymbol?.IsKind(SymbolKind.Property) == true) {

                        text = fieldSymbol.AssociatedSymbol.Name;
                    } else
                        text = symbol.Name;
                }
            }
            return SyntaxFactory.Identifier(text);
        }

        static SyntaxTokenList ConvertModifiers(IEnumerable<SyntaxToken> modifiers, TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context));
        }

        static SyntaxTokenList ConvertModifiers(SyntaxTokenList modifiers, TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context).Where(t => CSharpExtensions.Kind(t) != SyntaxKind.None));
        }

        static SyntaxToken? ConvertModifier(SyntaxToken m, TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            VBasic.SyntaxKind vbSyntaxKind = VBasic.VisualBasicExtensions.Kind(m);
            switch (vbSyntaxKind) {
                case VBasic.SyntaxKind.DateKeyword:
                    return SyntaxFactory.Identifier("System.DateTime");
            }
            var token = SyntaxKindExtensions.ConvertToken(vbSyntaxKind, context);
            return token == SyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        static IEnumerable<SyntaxToken> ConvertModifiersCore(IEnumerable<SyntaxToken> modifiers, TokenContext context)
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
            foreach (var token in modifiers.Where(m => !IgnoreInContext(m, context)).OrderBy(m => SyntaxTokenExtensions.IsKind(m, VBasic.SyntaxKind.PartialKeyword))) {
                var m = ConvertModifier(token, context);
                if (m.HasValue) yield return m.Value;
            }
            if (context == SyntaxKindExtensions.TokenContext.MemberInModule)
                yield return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
        }

        static bool IgnoreInContext(SyntaxToken m, TokenContext context)
        {
            switch (VBasic.VisualBasicExtensions.Kind(m)) {
                case VBasic.SyntaxKind.OptionalKeyword:
                case VBasic.SyntaxKind.ByValKeyword:
                case VBasic.SyntaxKind.IteratorKeyword:
                case VBasic.SyntaxKind.DimKeyword:
                    return true;
                case VBasic.SyntaxKind.ReadOnlyKeyword:
                case VBasic.SyntaxKind.WriteOnlyKeyword:
                    return context == SyntaxKindExtensions.TokenContext.Member;
                default:
                    return false;
            }
        }

        static bool IsConversionOperator(SyntaxToken token)
        {
            bool isConvOp= token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExplicitKeyword, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ImplicitKeyword)
                    ||token.IsKind(VBasic.SyntaxKind.NarrowingKeyword, VBasic.SyntaxKind.WideningKeyword);
            return isConvOp;
        }

        static bool IsVisibility(SyntaxToken token, TokenContext context)
        {
            return token.IsKind(VBasic.SyntaxKind.PublicKeyword, VBasic.SyntaxKind.FriendKeyword, VBasic.SyntaxKind.ProtectedKeyword, VBasic.SyntaxKind.PrivateKeyword)
                || (context == SyntaxKindExtensions.TokenContext.VariableOrConst && SyntaxTokenExtensions.IsKind(token, VBasic.SyntaxKind.ConstKeyword));
        }

        static SyntaxToken VisualBasicDefaultVisibility(TokenContext context)
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
    }
}
