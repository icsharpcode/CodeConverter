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
    }
}