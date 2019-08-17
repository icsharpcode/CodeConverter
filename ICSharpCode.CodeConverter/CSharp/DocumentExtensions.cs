using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using VBasic = Microsoft.CodeAnalysis.VisualBasic;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CSSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class DocumentExtensions
    {
        public static async Task<Document> WithSimplifiedSyntaxRootAsync(this Document doc)
        {
            var root = await doc.GetSyntaxRootAsync();
            var withSyntaxRoot = doc.WithSyntaxRoot(root.WithAdditionalAnnotations(Simplifier.Annotation));
            return await Simplifier.ReduceAsync(withSyntaxRoot);
        }

        public static async Task<Document> WithExpandedSyntaxRootAsync(this Document doc)
        {
            var syntaxRootAsync = await doc.GetSyntaxRootAsync();
            var expandableNodes = syntaxRootAsync.DescendantNodes(n => !CanBeExpanded(n)).Where(CanBeExpanded).ToList();
            syntaxRootAsync = await syntaxRootAsync.ReplaceNodesAsync(expandableNodes,
                (original, _, token) => Simplifier.ExpandAsync(original, doc, cancellationToken: token), CancellationToken.None);
            return doc.WithSyntaxRoot(syntaxRootAsync);
        }

        private static bool CanBeExpanded(SyntaxNode node)
        {
            return CanVbNodeBeExpanded(node) || CanCsNodeBeExpanded(node);
        }

        private static bool CanCsNodeBeExpanded(SyntaxNode node)
        {
            return node is CSharpSyntaxNode &&
                   (node is CSSyntax.AttributeSyntax ||
                    node is CSSyntax.AttributeArgumentSyntax ||
                    node is CSSyntax.ConstructorInitializerSyntax ||
                    node is CSSyntax.ExpressionSyntax ||
                    node is CSSyntax.FieldDeclarationSyntax ||
                    node is CSSyntax.StatementSyntax ||
                    node is CSSyntax.CrefSyntax ||
                    node is CSSyntax.XmlNameAttributeSyntax ||
                    node is CSSyntax.TypeConstraintSyntax ||
                    node is CSSyntax.BaseTypeSyntax);
        }

        private static bool CanVbNodeBeExpanded(SyntaxNode vbNode)
        {
            return vbNode is VBasic.VisualBasicSyntaxNode &&
                   (vbNode is VBSyntax.ExpressionSyntax ||
                    vbNode is VBSyntax.StatementSyntax ||
                    vbNode is VBSyntax.AttributeSyntax ||
                    vbNode is VBSyntax.SimpleArgumentSyntax ||
                    vbNode is VBSyntax.CrefReferenceSyntax ||
                    vbNode is VBSyntax.TypeConstraintSyntax);
        }
    }
}