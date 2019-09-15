using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class CompilationOptionsExtensions
    {
        private static readonly Lazy<Func<CompilationOptions, byte, CompilationOptions>> LazyWithMetadataImportOptions =
            new Lazy<Func<CompilationOptions, byte, CompilationOptions>>(CreateWithMetadataImportOptionsDelegate);

        private static Func<CompilationOptions, byte, CompilationOptions> CreateWithMetadataImportOptionsDelegate()
        {
            return typeof(CompilationOptions)
                .GetMethod("WithMetadataImportOptions",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                .CreateOpenInstanceDelegateForcingType<CompilationOptions, byte, CompilationOptions>();
        }

        public static Document CreateProjectDocumentFromTree(this CompilationOptions options,
            Workspace workspace, SyntaxTree tree, IEnumerable<MetadataReference> references, ParseOptions parseOptions,
            string singleDocumentAssemblyName = null)
        {
            singleDocumentAssemblyName = singleDocumentAssemblyName ?? "ProjectToBeConverted";
            ProjectId projectId = ProjectId.CreateNewId();
            var solution = workspace.CurrentSolution.AddProject(projectId, singleDocumentAssemblyName,
                singleDocumentAssemblyName, options.Language);

            var project = solution.GetProject(projectId)
                .WithCompilationOptions(options)
                .WithParseOptions(parseOptions)
                .WithMetadataReferences(references);
            return project.AddDocument("CodeToConvert", tree.GetRoot(), filePath: Path.Combine(Directory.GetCurrentDirectory(), "TempCodeToConvert.txt"));
        }

        /// <summary>
        /// This method becomes public in CodeAnalysis 3.1 and hence we can be confident it won't disappear.
        /// Need to use reflection for now until that version is widely enough deployed as taking a dependency would mean everyone needs latest VS version.
        /// </summary>
        public static CompilationOptions WithMetadataImportOptionsAll(this CompilationOptions baseOptions)
        {
            return LazyWithMetadataImportOptions.Value(baseOptions, 2 /*MetadataImportOptions.All*/);
        }
    }
}