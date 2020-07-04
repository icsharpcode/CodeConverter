using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Rename;

namespace ICSharpCode.CodeConverter.CSharp
{
    public class ReproIssue
    {
        public static async Task<CSharpCompilationOptions> ReproAsync(Project realVbProjectInVsWorkspace)
        {
            string namespaceToRename = "TheNamespaceToRename";
            var originalDocPath = "something.cs";

            var cSharpCompilationOptions = CreateCsProjectInfo(realVbProjectInVsWorkspace, out var projectInfo);

            var convertedCsProject = realVbProjectInVsWorkspace.Solution.RemoveProject(realVbProjectInVsWorkspace.Id).AddProject(projectInfo)
                .GetProject(realVbProjectInVsWorkspace.Id);

            var docId = DocumentId.CreateNewId(convertedCsProject.Id);
            var compilationUnitSyntax = SyntaxFactory.ParseCompilationUnit($"namespace {namespaceToRename} {{}}");

            var convertedWithDocs = convertedCsProject.Solution
                .AddDocument(docId, originalDocPath, compilationUnitSyntax, filePath: originalDocPath)
                .GetProject(convertedCsProject.Id);

            var compilation = await convertedWithDocs.GetCompilationAsync();
            var symbol = compilation.GetSymbolsWithName(s => s.StartsWith(namespaceToRename), SymbolFilter.Namespace).First();
            var renamedSolution = await Renamer.RenameSymbolAsync(convertedWithDocs.Solution, symbol, "RenamedNamespace",
                convertedWithDocs.Solution.Workspace.Options);
            return cSharpCompilationOptions;
        }

        private static CSharpCompilationOptions CreateCsProjectInfo(Project proj, out ProjectInfo projectInfo)
        {
            var cSharpCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
            projectInfo = ProjectInfo.Create(proj.Id, proj.Version, proj.Name,
                proj.AssemblyName,
                cSharpCompilationOptions.Language, null, proj.OutputFilePath,
                cSharpCompilationOptions, CSharpCompiler.ParseOptions, System.Array.Empty<DocumentInfo>(),
                proj.ProjectReferences,
                proj.MetadataReferences, proj.AnalyzerReferences);
            return cSharpCompilationOptions;
        }
    }
}