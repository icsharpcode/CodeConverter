using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class DocumentExtensions
    {
        public static async Task<Document> WithSimplifiedSyntaxRoot(this Document doc, SyntaxNode newRoot)
        {
            var withSyntaxRoot = doc.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Simplifier.Annotation));
            return await Simplifier.ReduceAsync(withSyntaxRoot);
        }
    }
}