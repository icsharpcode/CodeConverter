using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace CodeConverter.Tests.Compilation
{
    public static class CompilerExtensions
    {
        /// <summary>
        /// Compiles the given string of source code into an IL byte array.
        /// </summary>
        public static Assembly AssemblyFromCode(this ICompiler compiler, string code, IEnumerable<MetadataReference> additionalReferences = null)
        {
            var allReferences = DefaultReferences.NetStandard2.Concat(additionalReferences ?? new List<MetadataReference>());
            var parsedSyntaxTree = compiler.CreateTree(code);
            var compilation = compiler.CreateCompilationFromTree(parsedSyntaxTree, allReferences);

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream()) {
                var result = compilation.Emit(dllStream, pdbStream);
                if (!result.Success) {
                    throw new CompilationException($"{compiler.GetType().Name} error:\r\n{string.Join("\r\n", result.Diagnostics)}");
                }

                dllStream.Seek(0, SeekOrigin.Begin);
                pdbStream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());
            }
        }
    }
}
