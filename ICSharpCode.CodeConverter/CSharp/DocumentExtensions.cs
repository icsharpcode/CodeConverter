using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class DocumentExtensions
    {
        public static async Task<Document> WithSimplifiedSyntaxRoot(this Document doc, SyntaxNode root)
        {
            var withSyntaxRoot = doc.WithSyntaxRoot(root.WithAdditionalAnnotations(Simplifier.Annotation));
            return await Simplifier.ReduceAsync(withSyntaxRoot);
        }
    }
}