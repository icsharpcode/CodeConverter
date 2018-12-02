using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Text;
using ICSharpCode.CodeConverter.Shared;

namespace CodeConverter.Tests.Compilation
{
    /// <summary>
    /// A rudimentary VB.NET compiler using Roslyn.
    /// </summary>
    public class VBCompiler : ICompiler
    {
        private static readonly VisualBasicCompilationOptions DefaultCompilationOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        private static SyntaxTree Parse(string text)
        {
            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText);
        }

        private static Microsoft.CodeAnalysis.Compilation CompileAssembly(SyntaxTree syntaxTree, string assemblyName, IEnumerable<MetadataReference> additionalReferences = null)
        {
            var references = DefaultReferences.NetStandard2.Concat(additionalReferences ?? Enumerable.Empty<MetadataReference>());
            return VisualBasicCompilation.Create(assemblyName, new SyntaxTree[] { syntaxTree }, references, DefaultCompilationOptions);
        }

        public CompilerFrontend Compile { get; } = new CompilerFrontend() { Parse = Parse, Compile = CompileAssembly };
    }
}
