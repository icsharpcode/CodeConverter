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

namespace ICSharpCode.CodeConverter.CSharp
{
    public partial class VisualBasicConverter
    {
        public static CSharpSyntaxNode ConvertCompilationTree(VBasic.VisualBasicCompilation compilation, VBasic.VisualBasicSyntaxTree tree)
        {
            var visualBasicSyntaxVisitor = new VisualBasicConverter.NodesVisitor(compilation.GetSemanticModel(tree, true));
            return tree.GetRoot().Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }

        enum TokenContext
        {
            Global,
            InterfaceOrModule,
            Local,
            Member,
            VariableOrConst,
            MemberInModule,
            MemberInClass,
            MemberInStruct,
            MemberInInterface
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

            return null;
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

        static SyntaxTokenList ConvertModifiers(IEnumerable<SyntaxToken> modifiers, TokenContext context = TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context));
        }

        static SyntaxTokenList ConvertModifiers(SyntaxTokenList modifiers, TokenContext context = TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context).Where(t => CSharpExtensions.Kind(t) != SyntaxKind.None));
        }

        static SyntaxToken? ConvertModifier(SyntaxToken m, TokenContext context = TokenContext.Global)
        {
            VBasic.SyntaxKind vbSyntaxKind = VBasic.VisualBasicExtensions.Kind(m);
            switch (vbSyntaxKind) {
                case VBasic.SyntaxKind.DateKeyword:
                    return SyntaxFactory.Identifier("System.DateTime");
            }
            var token = ConvertToken(vbSyntaxKind, context);
            return token == SyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        static IEnumerable<SyntaxToken> ConvertModifiersCore(IEnumerable<SyntaxToken> modifiers, TokenContext context)
        {
            var contextsWithIdenticalDefaults = new[] { TokenContext.Global, TokenContext.Local, TokenContext.InterfaceOrModule, TokenContext.MemberInInterface };
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
            if (context == TokenContext.MemberInModule)
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
                    return context == TokenContext.Member;
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
                || (context == TokenContext.VariableOrConst && SyntaxTokenExtensions.IsKind(token, VBasic.SyntaxKind.ConstKeyword));
        }

        static SyntaxToken VisualBasicDefaultVisibility(TokenContext context)
        {
            switch (context) {
                case TokenContext.Global:
                case TokenContext.InterfaceOrModule:
                    return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                case TokenContext.Member:
                case TokenContext.MemberInModule:
                case TokenContext.MemberInClass:
                case TokenContext.MemberInInterface:
                case TokenContext.MemberInStruct:
                    return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                case TokenContext.Local:
                case TokenContext.VariableOrConst:
                    return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
            }
            throw new ArgumentOutOfRangeException(nameof(context), context, "Specified argument was out of the range of valid values.");
        }

        static SyntaxToken ConvertToken(SyntaxToken t, TokenContext context = TokenContext.Global)
        {
            VBasic.SyntaxKind vbSyntaxKind = VBasic.VisualBasicExtensions.Kind(t);
            return SyntaxFactory.Token(ConvertToken(vbSyntaxKind, context));
        }

        static SyntaxKind ConvertToken(VBasic.SyntaxKind t, TokenContext context = TokenContext.Global)
        {
            switch (t) {
                case VBasic.SyntaxKind.None:
                    return SyntaxKind.None;
                // built-in types
                case VBasic.SyntaxKind.BooleanKeyword:
                    return SyntaxKind.BoolKeyword;
                case VBasic.SyntaxKind.ByteKeyword:
                    return SyntaxKind.ByteKeyword;
                case VBasic.SyntaxKind.SByteKeyword:
                    return SyntaxKind.SByteKeyword;
                case VBasic.SyntaxKind.ShortKeyword:
                    return SyntaxKind.ShortKeyword;
                case VBasic.SyntaxKind.UShortKeyword:
                    return SyntaxKind.UShortKeyword;
                case VBasic.SyntaxKind.IntegerKeyword:
                    return SyntaxKind.IntKeyword;
                case VBasic.SyntaxKind.UIntegerKeyword:
                    return SyntaxKind.UIntKeyword;
                case VBasic.SyntaxKind.LongKeyword:
                    return SyntaxKind.LongKeyword;
                case VBasic.SyntaxKind.ULongKeyword:
                    return SyntaxKind.ULongKeyword;
                case VBasic.SyntaxKind.DoubleKeyword:
                    return SyntaxKind.DoubleKeyword;
                case VBasic.SyntaxKind.SingleKeyword:
                    return SyntaxKind.FloatKeyword;
                case VBasic.SyntaxKind.DecimalKeyword:
                    return SyntaxKind.DecimalKeyword;
                case VBasic.SyntaxKind.StringKeyword:
                    return SyntaxKind.StringKeyword;
                case VBasic.SyntaxKind.CharKeyword:
                    return SyntaxKind.CharKeyword;
                case VBasic.SyntaxKind.ObjectKeyword:
                    return SyntaxKind.ObjectKeyword;
                // literals
                case VBasic.SyntaxKind.NothingKeyword:
                    return SyntaxKind.NullKeyword;
                case VBasic.SyntaxKind.TrueKeyword:
                    return SyntaxKind.TrueKeyword;
                case VBasic.SyntaxKind.FalseKeyword:
                    return SyntaxKind.FalseKeyword;
                case VBasic.SyntaxKind.MeKeyword:
                    return SyntaxKind.ThisKeyword;
                case VBasic.SyntaxKind.MyBaseKeyword:
                    return SyntaxKind.BaseKeyword;
                // modifiers
                case VBasic.SyntaxKind.PublicKeyword:
                    return SyntaxKind.PublicKeyword;
                case VBasic.SyntaxKind.FriendKeyword:
                    return SyntaxKind.InternalKeyword;
                case VBasic.SyntaxKind.ProtectedKeyword:
                    return SyntaxKind.ProtectedKeyword;
                case VBasic.SyntaxKind.PrivateKeyword:
                    return SyntaxKind.PrivateKeyword;
                case VBasic.SyntaxKind.ByRefKeyword:
                    return SyntaxKind.RefKeyword;
                case VBasic.SyntaxKind.ParamArrayKeyword:
                    return SyntaxKind.ParamsKeyword;
                case VBasic.SyntaxKind.ReadOnlyKeyword:
                    return SyntaxKind.ReadOnlyKeyword;
                case VBasic.SyntaxKind.OverridesKeyword:
                    return SyntaxKind.OverrideKeyword;
                //New isn't as restrictive as shadows, but it will behave the same for all existing programs
                case VBasic.SyntaxKind.ShadowsKeyword:
                case VBasic.SyntaxKind.OverloadsKeyword:
                    return SyntaxKind.NewKeyword;
                case VBasic.SyntaxKind.OverridableKeyword:
                    return SyntaxKind.VirtualKeyword;
                case VBasic.SyntaxKind.SharedKeyword:
                    return SyntaxKind.StaticKeyword;
                case VBasic.SyntaxKind.ConstKeyword:
                    return SyntaxKind.ConstKeyword;
                case VBasic.SyntaxKind.PartialKeyword:
                    return SyntaxKind.PartialKeyword;
                case VBasic.SyntaxKind.MustInheritKeyword:
                    return SyntaxKind.AbstractKeyword;
                case VBasic.SyntaxKind.MustOverrideKeyword:
                    return SyntaxKind.AbstractKeyword;
                case VBasic.SyntaxKind.NotOverridableKeyword:
                case VBasic.SyntaxKind.NotInheritableKeyword:
                    return SyntaxKind.SealedKeyword;
                // unary operators
                case VBasic.SyntaxKind.UnaryMinusExpression:
                    return SyntaxKind.UnaryMinusExpression;
                case VBasic.SyntaxKind.UnaryPlusExpression:
                    return SyntaxKind.UnaryPlusExpression;
                case VBasic.SyntaxKind.NotExpression:
                    return SyntaxKind.LogicalNotExpression;
                // binary operators
                case VBasic.SyntaxKind.ConcatenateExpression:
                case VBasic.SyntaxKind.AddExpression:
                    return SyntaxKind.AddExpression;
                case VBasic.SyntaxKind.SubtractExpression:
                    return SyntaxKind.SubtractExpression;
                case VBasic.SyntaxKind.MultiplyExpression:
                    return SyntaxKind.MultiplyExpression;
                case VBasic.SyntaxKind.DivideExpression:
                case VBasic.SyntaxKind.IntegerDivideExpression:
                    return SyntaxKind.DivideExpression;
                case VBasic.SyntaxKind.ModuloExpression:
                    return SyntaxKind.ModuloExpression;
                case VBasic.SyntaxKind.AndAlsoExpression:
                    return SyntaxKind.LogicalAndExpression;
                case VBasic.SyntaxKind.OrElseExpression:
                    return SyntaxKind.LogicalOrExpression;
                case VBasic.SyntaxKind.OrExpression:
                    return SyntaxKind.BitwiseOrExpression;
                case VBasic.SyntaxKind.AndExpression:
                    return SyntaxKind.BitwiseAndExpression;
                case VBasic.SyntaxKind.ExclusiveOrExpression:
                    return SyntaxKind.ExclusiveOrExpression;
                case VBasic.SyntaxKind.EqualsExpression:
                case VBasic.SyntaxKind.CaseEqualsClause:
                    return SyntaxKind.EqualsExpression;
                case VBasic.SyntaxKind.NotEqualsExpression:
                case VBasic.SyntaxKind.CaseNotEqualsClause:
                    return SyntaxKind.NotEqualsExpression;
                case VBasic.SyntaxKind.GreaterThanExpression:
                case VBasic.SyntaxKind.CaseGreaterThanClause:
                    return SyntaxKind.GreaterThanExpression;
                case VBasic.SyntaxKind.GreaterThanOrEqualExpression:
                case VBasic.SyntaxKind.CaseGreaterThanOrEqualClause:
                    return SyntaxKind.GreaterThanOrEqualExpression;
                case VBasic.SyntaxKind.LessThanExpression:
                case VBasic.SyntaxKind.CaseLessThanClause:
                    return SyntaxKind.LessThanExpression;
                case VBasic.SyntaxKind.LessThanOrEqualExpression:
                case VBasic.SyntaxKind.CaseLessThanOrEqualClause:
                    return SyntaxKind.LessThanOrEqualExpression;
                case VBasic.SyntaxKind.IsExpression:
                    return SyntaxKind.EqualsExpression;
                case VBasic.SyntaxKind.IsNotExpression:
                    return SyntaxKind.NotEqualsExpression;
                case VBasic.SyntaxKind.LeftShiftExpression:
                    return SyntaxKind.LeftShiftExpression;
                case VBasic.SyntaxKind.RightShiftExpression:
                    return SyntaxKind.RightShiftExpression;
                // assignment
                case VBasic.SyntaxKind.SimpleAssignmentStatement:
                    return SyntaxKind.SimpleAssignmentExpression;
                case VBasic.SyntaxKind.ConcatenateAssignmentStatement:
                case VBasic.SyntaxKind.AddAssignmentStatement:
                    return SyntaxKind.AddAssignmentExpression;
                case VBasic.SyntaxKind.SubtractAssignmentStatement:
                    return SyntaxKind.SubtractAssignmentExpression;
                case VBasic.SyntaxKind.MultiplyAssignmentStatement:
                    return SyntaxKind.MultiplyAssignmentExpression;
                case VBasic.SyntaxKind.IntegerDivideAssignmentStatement:
                case VBasic.SyntaxKind.DivideAssignmentStatement:
                    return SyntaxKind.DivideAssignmentExpression;
                case VBasic.SyntaxKind.LeftShiftAssignmentStatement:
                    return SyntaxKind.LeftShiftAssignmentExpression;
                case VBasic.SyntaxKind.RightShiftAssignmentStatement:
                    return SyntaxKind.RightShiftAssignmentExpression;
                // Casts
                case VBasic.SyntaxKind.CObjKeyword:
                    return SyntaxKind.ObjectKeyword;
                case VBasic.SyntaxKind.CBoolKeyword:
                    return SyntaxKind.BoolKeyword;
                case VBasic.SyntaxKind.CCharKeyword:
                    return SyntaxKind.CharKeyword;
                case VBasic.SyntaxKind.CSByteKeyword:
                    return SyntaxKind.SByteKeyword;
                case VBasic.SyntaxKind.CByteKeyword:
                    return SyntaxKind.ByteKeyword;
                case VBasic.SyntaxKind.CShortKeyword:
                    return SyntaxKind.ShortKeyword;
                case VBasic.SyntaxKind.CUShortKeyword:
                    return SyntaxKind.UShortKeyword;
                case VBasic.SyntaxKind.CIntKeyword:
                    return SyntaxKind.IntKeyword;
                case VBasic.SyntaxKind.CUIntKeyword:
                    return SyntaxKind.UIntKeyword;
                case VBasic.SyntaxKind.CLngKeyword:
                    return SyntaxKind.LongKeyword;
                case VBasic.SyntaxKind.CULngKeyword:
                    return SyntaxKind.ULongKeyword;
                case VBasic.SyntaxKind.CDecKeyword:
                    return SyntaxKind.DecimalKeyword;
                case VBasic.SyntaxKind.CSngKeyword:
                    return SyntaxKind.FloatKeyword;
                case VBasic.SyntaxKind.CDblKeyword:
                    return SyntaxKind.DoubleKeyword;
                case VBasic.SyntaxKind.CStrKeyword:
                    return SyntaxKind.StringKeyword;
                // Converts 
                case VBasic.SyntaxKind.NarrowingKeyword:
                    return SyntaxKind.ExplicitKeyword;
                case VBasic.SyntaxKind.WideningKeyword:
                    return SyntaxKind.ImplicitKeyword;
                //
                case VBasic.SyntaxKind.AssemblyKeyword:
                    return SyntaxKind.AssemblyKeyword;
                case VBasic.SyntaxKind.AsyncKeyword:
                    return SyntaxKind.AsyncKeyword;
                case VBasic.SyntaxKind.AscendingKeyword:
                    return SyntaxKind.AscendingKeyword;
                case VBasic.SyntaxKind.DescendingKeyword:
                    return SyntaxKind.DescendingKeyword;

                // Not direct conversions

                case VBasic.SyntaxKind.ExponentiateAssignmentStatement:
                    return SyntaxKind.SimpleAssignmentExpression;
                case VBasic.SyntaxKind.ExponentiateExpression:
                    break;
            }
            throw new NotSupportedException(t + " not supported!");
        }
    }
}
