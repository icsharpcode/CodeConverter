using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp;
using CSS = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.VB
{
    internal static class SemanticModelSymbolSetExtensions
    {

        public static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsPerScope(this SemanticModel semanticModel, ISymbol symbol)
        {
            return GetCsLocalSymbolsPerScope(semanticModel, symbol).Select(y => y.Union(symbol.Yield()));
        }

        /// <remarks>
        /// In VB there's a special extra local defined with the same name as the method name, so the method symbol should be included in any conflict analysis
        /// </remarks>
        private static IEnumerable<IEnumerable<ISymbol>> GetCsLocalSymbolsPerScope(SemanticModel semanticModel, ISymbol symbol)
        {
            switch (symbol) {
                case IMethodSymbol methodSymbol:
                    return GetCsSymbolsDeclaredByMethod(semanticModel, methodSymbol, (CSS.BaseMethodDeclarationSyntax n) => (CS.CSharpSyntaxNode)n.ExpressionBody ?? n.Body, new SymbolKind[] { SymbolKind.Local, SymbolKind.Parameter, SymbolKind.TypeParameter });
                case IPropertySymbol propertySymbol:
                    return GetCsSymbolsDeclaredByProperty(semanticModel, propertySymbol);
                case IEventSymbol eventSymbol:
                    return GetCsSymbolsDeclaredByEvent(semanticModel, eventSymbol);
                case IFieldSymbol fieldSymbol:
                    return GetCsSymbolsDeclaredByField(semanticModel, fieldSymbol).Yield();
                default:
                    return Array.Empty<ISymbol>().Yield();
            }
        }
        public static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsDeclaredByMethod<TNode>(SemanticModel semanticModel, IMethodSymbol methodSymbol, Func<TNode, CS.CSharpSyntaxNode> selectWhereNotNull, SymbolKind[] kinds)
        {
            if (methodSymbol == null) {
                return Enumerable.Empty<IEnumerable<ISymbol>>();
            }
            var bodies = DeclarationWhereNotNull(methodSymbol, selectWhereNotNull).Where(x => x.SyntaxTree == semanticModel.SyntaxTree);
            return bodies.SelectMany(GetDeepestBlocks).Select(block => semanticModel.LookupSymbols(block.SpanStart).Where(x => x.MatchesKind(kinds)));
        }

        private static IEnumerable<CSS.BlockSyntax> GetDeepestBlocks(CS.CSharpSyntaxNode body)
        {
            return body.DescendantNodesAndSelf().OfType<CSS.BlockSyntax>().Where(x => !x.DescendantNodes().OfType<CSS.BlockSyntax>().Any());
        }

        private static IEnumerable<TResult> DeclarationWhereNotNull<TNode, TResult>(ISymbol symbol, Func<TNode, TResult> selectWhereNotNull)
        {
            return symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()).OfType<TNode>().Select(selectWhereNotNull).Where(x => x != null);
        }

        private static IEnumerable<TResult> DeclarationWhereManyNotNull<TNode, TResult>(ISymbol symbol, Func<TNode, IEnumerable<TResult>> selectManyWhereNotNull)
        {
            return symbol.DeclaringSyntaxReferences.Select(d => d.GetSyntax()).OfType<TNode>().SelectMany(selectManyWhereNotNull).Where(x => x != null);
        }

        private static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsDeclaredByProperty(SemanticModel semanticModel, IPropertySymbol propertySymbol)
        {
            Func<CSS.AccessorDeclarationSyntax, CS.CSharpSyntaxNode> getAccessorBody = (n) => (CS.CSharpSyntaxNode)n.ExpressionBody ?? n.Body;
            return GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.GetMethod, getAccessorBody, new SymbolKind[] { SymbolKind.Local, SymbolKind.Parameter, SymbolKind.TypeParameter })
                .Concat(GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.SetMethod, getAccessorBody, new SymbolKind[] { SymbolKind.Local, SymbolKind.TypeParameter }));
        }

        private static IEnumerable<ISymbol> GetCsSymbolsDeclaredByField(SemanticModel semanticModel, IFieldSymbol fieldSymbol)
        {
            return DeclarationWhereManyNotNull(fieldSymbol,
                (CSS.BaseFieldDeclarationSyntax f) => f.Declaration.Variables.Select(v => v.Initializer?.Value))
                .SelectMany(i => semanticModel.LookupSymbols(i.SpanStart, fieldSymbol.ContainingType));
        }

        private static IEnumerable<IEnumerable<ISymbol>> GetCsSymbolsDeclaredByEvent(SemanticModel semanticModel, IEventSymbol propertySymbol)
        {
            var kinds = new SymbolKind[] { SymbolKind.Local, SymbolKind.TypeParameter };
            Func<CSS.AccessorDeclarationSyntax, CS.CSharpSyntaxNode> getAccessorBody = (n) => (CS.CSharpSyntaxNode)n.ExpressionBody ?? n.Body;
            return GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.AddMethod, getAccessorBody, kinds)
                .Concat(GetCsSymbolsDeclaredByMethod(semanticModel, propertySymbol.RemoveMethod, getAccessorBody, kinds));
        }
    }
}