using System.Collections.Generic;
using System.IO;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class CompilationOptionsExtensions
    {

        public static Document CreateProjectDocumentFromTree(this CompilationOptions options,
            Workspace workspace, SyntaxTree tree, IEnumerable<MetadataReference> references, ParseOptions parseOptions,
            string singleDocumentAssemblyName = null)
        {
            singleDocumentAssemblyName = singleDocumentAssemblyName ?? "ProjectToBeConverted";
            ProjectId projectId = ProjectId.CreateNewId();

            string projFileExtension = parseOptions.Language == LanguageNames.CSharp ? ".csproj" : ".vbproj";
            var projectFilePath = Path.Combine(Directory.GetCurrentDirectory() + singleDocumentAssemblyName + projFileExtension);

            var solution = workspace.CurrentSolution.AddProject(projectId, singleDocumentAssemblyName,
                singleDocumentAssemblyName, options.Language)
                .WithProjectFilePath(projectId, projectFilePath);

            var project = solution.GetProject(projectId)
                .WithCompilationOptions(options)
                .WithParseOptions(parseOptions)
                .WithMetadataReferences(references);
            return project.AddDocument("CodeToConvert", tree.GetRoot(), filePath: Path.Combine(Directory.GetCurrentDirectory(), "TempCodeToConvert.txt"));
        }

        public static CompilationOptions WithMetadataImportOptionsAll(this CompilationOptions baseOptions)
        {
            return CachedReflectedDelegates.LazyWithMetadataImportOptions.Value(baseOptions, 2 /*MetadataImportOptions.All*/);
        }
    }
}