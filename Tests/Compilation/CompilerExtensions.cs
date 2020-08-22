using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Tests.Compilation
{
    public static class CompilerExtensions
    {
        /// <summary>
        /// Compiles the given string of source code into an IL byte array.
        /// </summary>
        /// <remarks>The transitive closure of the references for <paramref name="requiredAssemblies"/> are added.</remarks>
        public static Assembly AssemblyFromCode(this ICompiler compiler, SyntaxTree syntaxTree, params Assembly[] requiredAssemblies)
        {
            var allReferences = DefaultReferences.With(requiredAssemblies);
            var compilation = compiler.CreateCompilationFromTree(syntaxTree, allReferences);

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream()) {
                var result = compilation.Emit(dllStream, pdbStream);
                if (!result.Success) {
                    string codeLines = string.Join("\r\n", Utils.HomogenizeEol(syntaxTree.ToString())
                        .Split(new string[]{"\r\n"}, StringSplitOptions.None)
                        .Select((l, i) => $"{i+1:000}: {l}"));
                    throw new CompilationException(
                        $"{compiler.GetType().Name} error:\r\n{string.Join("\r\n", result.Diagnostics)}\r\n\r\nSource code:\r\n{codeLines}"
                    );
                }

                dllStream.Seek(0, SeekOrigin.Begin);
                pdbStream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(dllStream.ToArray(), pdbStream.ToArray());
            }
        }
    }
}
