using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using VbSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using CsSyntax = Microsoft.CodeAnalysis.CSharp.Syntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using SyntaxNodeExtensions = ICSharpCode.CodeConverter.Util.SyntaxNodeExtensions;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class CommentConvertingNodesVisitor : VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>>
    {
        private readonly VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>> _wrappedVisitor;

        public CommentConvertingNodesVisitor(VisualBasicSyntaxVisitor<Task<CSharpSyntaxNode>> wrappedVisitor)
        {
            this._wrappedVisitor = wrappedVisitor;
        }
        public override async Task<CSharpSyntaxNode> DefaultVisit(SyntaxNode node)
        {
            return await DefaultVisitInner(node);
        }

        private async Task<CSharpSyntaxNode> DefaultVisitInner(SyntaxNode node)
        {
            return await _wrappedVisitor.Visit(node);
        }

        public override async Task<CSharpSyntaxNode> VisitCompilationUnit(VbSyntax.CompilationUnitSyntax node)
        {
            var convertedNode = (CsSyntax.CompilationUnitSyntax)await DefaultVisitInner(node);
            // Special case where we need to map manually because it's a special zero-width token that just has leading trivia that isn't at the start of the line necessarily
            return convertedNode.WithEndOfFileToken(
                convertedNode.EndOfFileToken.WithSourceMappingFrom(node.EndOfFileToken)
            );
        }
    }
}