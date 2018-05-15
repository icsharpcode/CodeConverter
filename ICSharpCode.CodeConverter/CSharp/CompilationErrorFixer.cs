using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CompilationErrorFixer
    {
        private const string UnresolvedTypeOrNamespaceDiagnosticId = "CS0246";
        private const string UnusedUsingDiagnosticId = "CS8019";
        private readonly CSharpSyntaxTree _syntaxTree;
        private readonly SemanticModel _semanticModel;

        public CompilationErrorFixer(CSharpCompilation compilation, CSharpSyntaxTree syntaxTree)
        {
            this._syntaxTree = syntaxTree;
            this._semanticModel = compilation.GetSemanticModel(syntaxTree, true);
        }

        public CSharpSyntaxNode Fix()
        {
            var syntaxNode = _syntaxTree.GetRoot();
            return syntaxNode.ReplaceNodes(syntaxNode.DescendantNodes(), ComputeReplacementNode)
                .TypeSwitch(
                    (CompilationUnitSyntax x) => TidyUsings(x),
                    node => node
                );
        }

        private SyntaxNode ComputeReplacementNode(SyntaxNode originalNode, SyntaxNode potentiallyRewrittenNode)
        {
            return potentiallyRewrittenNode.TypeSwitch(
                (ArgumentListSyntax x) => FixOutParameters(x),
                node => node
            );
        }

        /// <remarks>These are tidied up so we can add as many GlobalImports as we want when building compilations</remarks>
        private CSharpSyntaxNode TidyUsings(CompilationUnitSyntax compilationUnitSyntax)
        {
            var diagnostics = _semanticModel.GetDiagnostics().ToList();
            var unusedUsings = diagnostics
                .Where(d => d.Id == UnusedUsingDiagnosticId)
                .Select(d => compilationUnitSyntax.FindNode(d.Location.SourceSpan))
                .OfType<UsingDirectiveSyntax>()
                .ToList();

            var nodesWithUnresolvedTypes = diagnostics
                .Where(d => d.Id == UnresolvedTypeOrNamespaceDiagnosticId && d.Location.IsInSource)
                .Select(d => compilationUnitSyntax.FindNode(d.Location.SourceSpan))
                .ToLookup(d => d.GetAncestor<UsingDirectiveSyntax>());
            unusedUsings = unusedUsings.Except(nodesWithUnresolvedTypes.Select(g => g.Key)).ToList();

            if (!nodesWithUnresolvedTypes[null].Any() && unusedUsings.Any()) {
                compilationUnitSyntax =
                    compilationUnitSyntax.RemoveNodes(unusedUsings, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return compilationUnitSyntax;
        }

        private SyntaxNode FixOutParameters(ArgumentListSyntax argumentListSyntax)
        {
            var invocationExpression = argumentListSyntax.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocationExpression == null) {
                return argumentListSyntax;
            }

            if (_semanticModel.GetSymbolInfo(invocationExpression)
                .ExtractBestMatch(s => s is IMethodSymbol ims && argumentListSyntax.Arguments.Count == ims.Parameters.Length) is IMethodSymbol methodSymbol)
            {
                //Won't work for named parameters
                for (var index = 0;
                    index < Math.Min(argumentListSyntax.Arguments.Count, methodSymbol.Parameters.Length);
                    index++)
                {
                    var argument = argumentListSyntax.Arguments[index];
                    var refOrOutKeyword = GetRefKeyword(methodSymbol.Parameters[index]);
                    var currentSyntaxKind = argumentListSyntax.Arguments[index].Kind();
                    if (!refOrOutKeyword.IsKind(currentSyntaxKind))
                    {
                        argumentListSyntax =
                            argumentListSyntax.ReplaceNode(argument, argument.WithRefOrOutKeyword(refOrOutKeyword));
                    }
                }
            }

            return argumentListSyntax;
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