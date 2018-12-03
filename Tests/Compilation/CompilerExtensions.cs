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
        /// <remarks>The transitive closure of the references for <paramref name="requiredAssemblies"/> are added.</remarks>
        public static Assembly AssemblyFromCode(this ICompiler compiler, string code, params Assembly[] requiredAssemblies)
        {
            var allReferences = DefaultReferences.NetStandard2.Concat(GetMetadataReferences(requiredAssemblies));
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

        private static IEnumerable<PortableExecutableReference> GetMetadataReferences(Assembly[] assemblies)
        {
            return WithAllReferences(assemblies).Select(a => MetadataReference.CreateFromFile(a.Location));
        }

        private static IReadOnlyCollection<Assembly> WithAllReferences(IEnumerable<Assembly> initalAssemblies)
        {
            var toAdd = new Queue<Assembly>(initalAssemblies);
            var assemblies = new HashSet<Assembly>();
            while (toAdd.Any()) {
                var current = toAdd.Dequeue();
                if (assemblies.Add(current)) {
                    foreach (var reference in LoadDirectReferences(current)) {
                        toAdd.Enqueue(reference);
                    }
                }
            }

            return assemblies;
        }

        private static List<Assembly> LoadDirectReferences(Assembly a)
        {
            return a.GetReferencedAssemblies().Select(Assembly.Load).ToList();
        }
    }
}
