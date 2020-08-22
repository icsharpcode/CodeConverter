using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class CompilationOptionsExtensions
    {
        public static Document AddDocumentFromTree(this Project project, SyntaxTree tree)
        {
            return project.AddDocument("CodeToConvert", tree.GetRoot(), filePath: string.IsNullOrEmpty(tree.FilePath) ? TempFilePath(tree.Options.Language) : tree.FilePath);
        }

        private static string TempFilePath(string optionsLanguage)
        {
            return Path.Combine(Path.GetTempPath(), "TempCodeToConvert." + optionsLanguage == LanguageNames.CSharp ? "cs" : "vb");
        }

        public static async Task<Project> CreateProjectAsync(this CompilationOptions options, IEnumerable<MetadataReference> references, ParseOptions parseOptions, string singleDocumentAssemblyName = "ProjectToBeConverted")
        {
            ProjectId projectId = ProjectId.CreateNewId();

            string projFileExtension = parseOptions.Language == LanguageNames.CSharp ? ".csproj" : ".vbproj";
            var projectFilePath = Path.Combine(Directory.GetCurrentDirectory() + singleDocumentAssemblyName + projFileExtension);
            var solution = (await ThreadSafeWorkspaceHelper.EmptyAdhocSolution.GetValueAsync()).AddProject(projectId, singleDocumentAssemblyName,
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