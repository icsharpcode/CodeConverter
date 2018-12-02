using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace CodeConverter.Tests.Compilation
{
    public class CompilerFrontend
    {
        private readonly ICompiler _compiler;

        public CompilerFrontend(ICompiler compiler)
        {
            _compiler = compiler;
        }
        
        /// <summary>
        /// Compiles the given source file into an IL byte array.
        /// </summary>
        public byte[] FromFile(string filename, string assemblyName, IEnumerable<MetadataReference> additionalReferences = null)
        {
            string source = File.ReadAllText(filename);
            return FromString(source, additionalReferences);
        }

        /// <summary>
        /// Compiles the given string of source code into an IL byte array.
        /// </summary>
        public byte[] FromString(string code, IEnumerable<MetadataReference> additionalReferences = null)
        {
            var allReferences = DefaultReferences.NetStandard2.Concat(additionalReferences ?? new List<MetadataReference>());
            var parsedSyntaxTree = _compiler.CreateTree(code);
            var compilation = _compiler.CreateCompilationFromTree(parsedSyntaxTree, allReferences);

            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                if (!result.Success)
                {
                    throw new CompilationException($"Compilation failed:\n{string.Join("\n", result.Diagnostics)}");
                }
                else
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream.ToArray();
                }
            }
        }
    }
}
