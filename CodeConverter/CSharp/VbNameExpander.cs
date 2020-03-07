using System.Threading;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class VbNameExpander : ISyntaxExpander
    {
        private static readonly SyntaxToken _dotToken = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.Token(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.DotToken);
        public static ISyntaxExpander Instance { get; } = new VbNameExpander();

        public bool ShouldExpandWithinNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return !ShouldExpandNode(node, root, semanticModel) &&
                   !IsRoslynInstanceExpressionBug(node as MemberAccessExpressionSyntax); ;
        }

        public bool ShouldExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return ShouldExpandName(node) ||
                   ShouldExpandMemberAccess(node, root, semanticModel);
        }

        private static bool ShouldExpandMemberAccess(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel)
        {
            return node is MemberAccessExpressionSyntax maes && !IsRoslynInstanceExpressionBug(maes) &&
                   ShouldBeQualified(node, semanticModel.GetSymbolInfo(node).Symbol, semanticModel, root);
        }

        private static bool ShouldExpandName(SyntaxNode node)
        {
            return node is NameSyntax && NameCanBeExpanded(node);
        }

        public SyntaxNode ExpandNode(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel,
            Workspace workspace)
        {
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (node is SimpleNameSyntax sns && IsMyBaseBug(node, symbol, root, semanticModel) && semanticModel.GetOperation(node) is IMemberReferenceOperation mro && mro.Instance != null) {
                var expressionSyntax = (ExpressionSyntax)mro.Instance.Syntax;
                return MemberAccess(expressionSyntax, sns);
            }
            if (node is MemberAccessExpressionSyntax maes && IsTypePromotion(node, symbol, root, semanticModel) && semanticModel.GetOperation(node) is IMemberReferenceOperation mro2 && mro2.Instance != null) {
                var expressionSyntax = (ExpressionSyntax)mro2.Instance.Syntax;
                return MemberAccess(expressionSyntax, SyntaxFactory.IdentifierName(mro2.Member.Name));
            }
            return IsOriginalSymbolGenericMethod(semanticModel, node) ? node : Simplifier.Expand(node, semanticModel, workspace);
        }

        /// <summary>
        /// Aim to qualify each name once at the highest level we can get the correct qualification.
        /// i.e. qualify "b.c" to "a.b.c", don't recurse in and try to qualify b or c alone.
        /// We recurse in until we find a static symbol, or find something that Roslyn's expand doesn't deal with sufficiently
        /// This leaves the possibility of not qualifying some instance references which didn't contain
        /// </summary>
        private static bool ShouldBeQualified(SyntaxNode node,
            ISymbol symbol, SemanticModel semanticModel, SyntaxNode root)
        {

            return symbol?.IsStatic == true || IsMyBaseBug(node, symbol, root, semanticModel) || IsTypePromotion(node, symbol, root, semanticModel);
        }

        private static bool NameCanBeExpanded(SyntaxNode node)
        {
            // Workaround roslyn bug where it will try to expand something even when the parent node cannot contain the type of the expanded node
            if (node.Parent is NameColonEqualsSyntax || node.Parent is NamedFieldInitializerSyntax) return false;
            // Workaround roslyn bug where it duplicates the inferred name
            if (node.Parent is InferredFieldInitializerSyntax) return false;
            return true;
        }

        /// <returns>True iff calling Expand would qualify with MyBase when the symbol isn't in the base type
        /// See https://github.com/dotnet/roslyn/blob/97123b393c3a5a91cc798b329db0d7fc38634784/src/Workspaces/VisualBasic/Portable/Simplification/VisualBasicSimplificationService.Expander.vb#L657</returns>
        private static bool IsMyBaseBug(SyntaxNode node, ISymbol symbol, SyntaxNode root, SemanticModel semanticModel)
        {
            if (IsInstanceReference(symbol) && node is NameSyntax) {
                return GetEnclosingNamedType(semanticModel, root, node.SpanStart) is ITypeSymbol
                           implicitQualifyingSymbol &&
                       !implicitQualifyingSymbol.ContainsMember(symbol);
            }

            return false;
        }

        private static bool IsTypePromotion(SyntaxNode node, ISymbol symbol, SyntaxNode root, SemanticModel semanticModel)
        {
            if (IsInstanceReference(symbol) && node is MemberAccessExpressionSyntax maes && maes.Expression != null) {
                var qualifyingType = semanticModel.GetTypeInfo(maes.Expression).Type;
                return qualifyingType == null || !qualifyingType.ContainsMember(symbol);
            }

            return false;
        }

        /// <summary>
        /// Roslyn bug - accidentally expands "New" into an identifier causing compile error
        /// </summary>
        public static bool IsRoslynInstanceExpressionBug(MemberAccessExpressionSyntax node)
        {
            return node?.Expression is InstanceExpressionSyntax;
        }

        /// <summary>
        /// Roslyn bug - accidentally expands anonymous types to just "Global."
        /// Since the C# reducer also doesn't seem to reduce generic extension methods, it's best to avoid those too, so let's just avoid all generic methods
        /// </summary>
        private static bool IsOriginalSymbolGenericMethod(SemanticModel semanticModel, SyntaxNode node)
        {
            return semanticModel.GetSymbolInfo(node).Symbol.IsGenericMethod();
        }

        private static bool IsInstanceReference(ISymbol symbol)
        {
            return symbol?.IsStatic == false && (symbol.Kind == SymbolKind.Method || symbol.Kind ==
                                                 SymbolKind.Field || symbol.Kind == SymbolKind.Property);
        }

        private static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expressionSyntax, SimpleNameSyntax sns)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                expressionSyntax,
                _dotToken,
                sns);
        }

        /// <summary>
        /// Pasted from AbstractGenerateFromMembersCodeRefactoringProvider
        /// Gets the enclosing named type for the specified position.  We can't use
        /// <see cref="SemanticModel.GetEnclosingSymbol"/> because that doesn't return
        /// the type you're current on if you're on the header of a class/interface.
        /// </summary>
        private static INamedTypeSymbol GetEnclosingNamedType(
            SemanticModel semanticModel, SyntaxNode root, int start,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = root.FindToken(start);
            if (token == ((ICompilationUnitSyntax)root).EndOfFileToken) {
                token = token.GetPreviousToken();
            }

            for (var node = token.Parent; node != null; node = node.Parent) {
                if (semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol declaration) {
                    return declaration;
                }
            }

            return null;
        }
    }
}