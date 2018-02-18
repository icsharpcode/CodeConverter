using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CompilationErrorFixer
    {
        private readonly CSharpSyntaxTree syntaxTree;
        private readonly SemanticModel semanticModel;

        public CompilationErrorFixer(CSharpCompilation compilation, CSharpSyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
            this.semanticModel = compilation.GetSemanticModel(syntaxTree, true);
        }

        public CSharpSyntaxNode Fix()
        {
            var syntaxNode = syntaxTree.GetRoot();
            return syntaxNode.ReplaceNodes(syntaxNode.DescendantNodes(), ComputeReplacementNode);
        }

        private SyntaxNode ComputeReplacementNode(SyntaxNode originalNode, SyntaxNode potentiallyRewrittenNode)
        {
            if (!(potentiallyRewrittenNode is ArgumentListSyntax nodeToReturn)) return potentiallyRewrittenNode;

            var invocationExpression = nodeToReturn.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocationExpression == null) return potentiallyRewrittenNode;

            var methodSymbol = semanticModel.GetSymbolInfo(invocationExpression).CandidateSymbols.OfType<IMethodSymbol>()
                .FirstOrDefault(s => invocationExpression.ArgumentList.Arguments.Count == s.Parameters.Length);
            if (methodSymbol != null) {
                //Won't work for named parameters
                for (var index = 0; index < Math.Min(nodeToReturn.Arguments.Count, methodSymbol.Parameters.Length); index++) {
                    var argument = nodeToReturn.Arguments[index];
                    var refOrOutKeyword = GetRefKeyword(methodSymbol.Parameters[index]);
                    var currentSyntaxKind = nodeToReturn.Arguments[index].Kind();
                    if (!refOrOutKeyword.IsKind(currentSyntaxKind)) {
                        nodeToReturn = nodeToReturn.ReplaceNode(argument, argument.WithRefOrOutKeyword(refOrOutKeyword));
                    }
                }
            }
            return nodeToReturn;
        }

        private static SyntaxToken GetRefKeyword(IParameterSymbol formalParameter)
        {
            SyntaxToken token;
            switch (formalParameter.RefKind) {
                case RefKind.None:
                    token = default(SyntaxToken);
                    break;
                case RefKind.Ref:
                    token = SyntaxFactory.Token(SyntaxKind.RefKeyword);
                    break;
                case RefKind.Out:
                    token = SyntaxFactory.Token(SyntaxKind.OutKeyword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return token.WithTrailingTrivia(SyntaxFactory.Whitespace(" "));
        }
    }
}