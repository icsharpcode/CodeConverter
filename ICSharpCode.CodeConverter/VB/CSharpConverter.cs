using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressionSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;
using SyntaxFacts = Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts;
using SyntaxKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using TypeSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.TypeSyntax;
using VariableDeclaratorSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.VariableDeclaratorSyntax;
using static ICSharpCode.CodeConverter.VB.SyntaxKindExtensions;

namespace ICSharpCode.CodeConverter.VB
{
    public partial class CSharpConverter
    {
        public static VisualBasicSyntaxNode ConvertCompilationTree(CS.CSharpCompilation compilation, CS.CSharpSyntaxTree tree)
        {
            var visualBasicSyntaxVisitor = new CSharpConverter.NodesVisitor(compilation.GetSemanticModel(tree, true));
            return tree.GetRoot().Accept(visualBasicSyntaxVisitor.TriviaConvertingVisitor);
        }

        static IEnumerable<SyntaxToken> ConvertModifiersCore(IEnumerable<SyntaxToken> modifiers, TokenContext context)
        {
            if (context != SyntaxKindExtensions.TokenContext.Local && context != SyntaxKindExtensions.TokenContext.InterfaceOrModule)
            {
                bool visibility = false;
                foreach (var token in modifiers)
                {
                    if (IsVisibility(token, context))
                    {
                        visibility = true;
                        break;
                    }
                }
                if (!visibility && context == SyntaxKindExtensions.TokenContext.Member)
                    yield return CSharpDefaultVisibility(context); 
            }
            foreach (var token in modifiers.Where(m => !IgnoreInContext(m, context)))
            {
                var m = ConvertModifier(token, context);
                if (m.HasValue) yield return m.Value;
            }
        }

        static bool IgnoreInContext(SyntaxToken m, TokenContext context)
        {
            switch (context)
            {
                case SyntaxKindExtensions.TokenContext.InterfaceOrModule:
                    return m.IsKind(CS.SyntaxKind.PublicKeyword, CS.SyntaxKind.StaticKeyword);
            }
            return false;
        }

        static bool IsVisibility(SyntaxToken token, TokenContext context)
        {
            return token.IsKind(CS.SyntaxKind.PublicKeyword, CS.SyntaxKind.InternalKeyword, CS.SyntaxKind.ProtectedKeyword, CS.SyntaxKind.PrivateKeyword)
                || (context == SyntaxKindExtensions.TokenContext.VariableOrConst && SyntaxTokenExtensions.IsKind(token, CS.SyntaxKind.ConstKeyword));
        }

        static SyntaxToken CSharpDefaultVisibility(TokenContext context)
        {
            switch (context)
            {
                case SyntaxKindExtensions.TokenContext.Global:
                    return SyntaxFactory.Token(SyntaxKind.FriendKeyword);
                case SyntaxKindExtensions.TokenContext.Local:
                case SyntaxKindExtensions.TokenContext.VariableOrConst:
                case SyntaxKindExtensions.TokenContext.Member:
                    return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
            }
            throw new ArgumentOutOfRangeException(nameof(context));
        }

        static SyntaxTokenList ConvertModifiers(IEnumerable<SyntaxToken> modifiers, TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context));
        }

        static SyntaxTokenList ConvertModifiers(SyntaxTokenList modifiers, TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            return SyntaxFactory.TokenList(ConvertModifiersCore(modifiers, context));
        }

        static SyntaxToken? ConvertModifier(SyntaxToken m, TokenContext context = SyntaxKindExtensions.TokenContext.Global)
        {
            var token = SyntaxKindExtensions.ConvertToken(CS.CSharpExtensions.Kind(m), context);
            return token == SyntaxKind.None ? null : new SyntaxToken?(SyntaxFactory.Token(token));
        }

        static SeparatedSyntaxList<VariableDeclaratorSyntax> RemodelVariableDeclaration(CSS.VariableDeclarationSyntax declaration, CS.CSharpSyntaxVisitor<VisualBasicSyntaxNode> nodesVisitor)
        {
            var type = (TypeSyntax)declaration.Type.Accept(nodesVisitor);
            var declaratorsWithoutInitializers = new List<CSS.VariableDeclaratorSyntax>();
            var declarators = new List<VariableDeclaratorSyntax>();

            foreach (var v in declaration.Variables)
            {
                if (v.Initializer == null)
                {
                    declaratorsWithoutInitializers.Add(v);
                    continue;
                }
                else
                {
                    declarators.Add(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.SingletonSeparatedList(ExtractIdentifier(v)),
                            declaration.Type.IsVar ? null : SyntaxFactory.SimpleAsClause(type),
                            SyntaxFactory.EqualsValue((ExpressionSyntax)v.Initializer.Value.Accept(nodesVisitor))
                        )
                    );
                }
            }

            if (declaratorsWithoutInitializers.Count > 0)
            {
                declarators.Insert(0, SyntaxFactory.VariableDeclarator(SyntaxFactory.SeparatedList(declaratorsWithoutInitializers.Select(ExtractIdentifier)), SyntaxFactory.SimpleAsClause(type), null));
            }

            return SyntaxFactory.SeparatedList(declarators);
        }

        static ModifiedIdentifierSyntax ExtractIdentifier(CSS.VariableDeclaratorSyntax v)
        {
            return SyntaxFactory.ModifiedIdentifier(ConvertIdentifier(v.Identifier));
        }

        static SyntaxToken ConvertIdentifier(SyntaxToken id)
        {
            var idText = id.ValueText;
            // Underscore is a special character in VB lexer which continues lines - not sure where to find the whole set of other similar tokens if any
            // Rather than a complicated contextual rename, just add an extra dash to all identifiers and hope this method is consistently used
            if (idText.All(c => c == '_')) idText += "_";
            var keywordKind = SyntaxFacts.GetKeywordKind(idText);
            if (keywordKind != SyntaxKind.None && !SyntaxFacts.IsPredefinedType(keywordKind))
                return SyntaxFactory.Identifier("[" + idText + "]");
            return SyntaxFactory.Identifier(idText);
        }

        static ExpressionSyntax Literal(object o) => GetLiteralExpression(o);

        internal static ExpressionSyntax GetLiteralExpression(object value)
        {
            if (value is bool)
                return (bool)value ? SyntaxFactory.TrueLiteralExpression(SyntaxFactory.Token(SyntaxKind.TrueKeyword)) : SyntaxFactory.FalseLiteralExpression(SyntaxFactory.Token(SyntaxKind.FalseKeyword));
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
            if (value is double)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((double)value));
            if (value is decimal)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((decimal)value));

            if (value is char)
                return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)value));

            if (value is string)
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal((string)value));

            if (value == null)
                return SyntaxFactory.NothingLiteralExpression(SyntaxFactory.Token(SyntaxKind.NothingKeyword));

            return null;
        }
    }
}
