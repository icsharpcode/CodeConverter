using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class CompilationOptionsExtensions
    {
        private static Lazy<Workspace> Workspace = new Lazy<Workspace>(() => new AdhocWorkspace());

        public static Document AddDocumentFromTree(this Project project, SyntaxTree tree)
        {
            return project.AddDocument("CodeToConvert", tree.GetRoot(), filePath: Path.Combine(Directory.GetCurrentDirectory(), "TempCodeToConvert.txt"));
        }

        public static Project CreateProject(this CompilationOptions options, IEnumerable<MetadataReference> references, ParseOptions parseOptions, string singleDocumentAssemblyName = "ProjectToBeConverted")
        {
            ProjectId projectId = ProjectId.CreateNewId();

            string projFileExtension = parseOptions.Language == LanguageNames.CSharp ? ".csproj" : ".vbproj";
            var projectFilePath = Path.Combine(Directory.GetCurrentDirectory() + singleDocumentAssemblyName + projFileExtension);
            var solution = Workspace.Value.CurrentSolution.AddProject(projectId, singleDocumentAssemblyName,
                singleDocumentAssemblyName, options.Language)
                .WithProjectFilePath(projectId, projectFilePath);

            var project = solution.GetProject(projectId)
                .WithCompilationOptions(options)
                .WithParseOptions(parseOptions)
                .WithMetadataReferences(references);
            return project;
        }
    }
}