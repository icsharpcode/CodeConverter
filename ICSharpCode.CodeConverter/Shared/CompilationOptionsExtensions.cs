using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class CompilationOptionsExtensions {

        public static Document CreateProjectDocumentFromTree(this CompilationOptions options,
            Workspace workspace, SyntaxTree tree, IEnumerable<MetadataReference> references)
        {
            ProjectId projectId = ProjectId.CreateNewId();
            var solution = workspace.CurrentSolution.AddProject(projectId, "ProjectToBeConverted",
                "ProjectToBeConverted", options.Language);

            var project = solution.GetProject(projectId)
                .WithCompilationOptions(options)
                .WithMetadataReferences(references);
            return project.AddDocument("CodeToConvert", tree.GetRoot(), filePath: Path.Combine(Directory.GetCurrentDirectory(), "TempCodeToConvert.txt"));
        }

        /// <summary>
        /// This method becomes public in CodeAnalysis 3.1 and hence we can be confident it won't disappear.
        /// Need to use reflection for now until that version is widely enough deployed as taking a dependency would mean everyone needs latest VS version.
        /// </summary>
        public static CompilationOptions WithMetadataImportOptionsAll(this CompilationOptions baseOptions)
        {
            Type optionsType = baseOptions.GetType();
            var withMetadataImportOptions = optionsType
                .GetMethod("WithMetadataImportOptions",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var options =
                withMetadataImportOptions.Invoke(baseOptions,
                    new object[] {(byte)2 /*MetadataImportOptions.All*/});
            return (CompilationOptions)options;
        }
    }
}