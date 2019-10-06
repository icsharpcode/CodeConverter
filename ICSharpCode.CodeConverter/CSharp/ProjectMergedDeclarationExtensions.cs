using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;

namespace ICSharpCode.CodeConverter.CSharp
{
    /// <summary>
    /// Allows transforming embedded/merged declarations into real documents. i.e. the VB My namespace
    /// </summary>
    /// <remarks>
    /// Rather than renaming the declarations, it may make more sense to change the parse options to eliminate the original one using the internal: WithSuppressEmbeddedDeclarations.
    /// </remarks>
    internal static class ProjectMergedDeclarationExtensions
    {
        private static Func<Location, SyntaxTree> _getEmbeddedSyntaxTree;

        public static async Task<Project> WithRenamedMergedMyNamespace(this Project vbProject)
        {
            int mergedDeclarationCount = 1;
            var compilation = await vbProject.GetCompilationAsync();
            var ns = compilation.SourceModule.GlobalNamespace;

            var projectDir = Path.Combine(Path.GetDirectoryName(vbProject.FilePath), "My Project");
            var roots = await ns.Locations.Where(l => !l.IsInSource).Select(GetEmbeddedSyntaxTree).SelectAsync(t => t.GetTextAsync());
            foreach (var root in roots) {
                string name = "MergedDeclaration" + mergedDeclarationCount++;
                var modifiedText = root.ToString().Replace("Namespace My", $"Namespace {Constants.MergedMyNamespace}");
                vbProject = vbProject.AddDocument(name, modifiedText, filePath: Path.Combine(projectDir, name + ".Designer.vb")).Project;
            }

            return vbProject;
        }

        private static SyntaxTree GetEmbeddedSyntaxTree(Location loc)
        {
            if (_getEmbeddedSyntaxTree == null) {
                var property = loc.GetType().GetProperty("PossiblyEmbeddedOrMySourceTree");
                _getEmbeddedSyntaxTree = property?.GetMethod.GetRuntimeBaseDefinition()
                    .CreateOpenInstanceDelegateForcingType<Location, SyntaxTree>();
            }
            return _getEmbeddedSyntaxTree?.Invoke(loc);
        }

        public static async Task<Project> RenameMergedMyNamespace(this Project project)
        {
            for (var symbolToRename = await GetFirstSymbolWithName(project); symbolToRename != null; symbolToRename = await GetFirstSymbolWithName(project)) {
                var renamedSolution = await Renamer.RenameSymbolAsync(project.Solution, symbolToRename, "My", default(OptionSet));
                project = renamedSolution.GetProject(project.Id);
            }

            return project;
        }

        private static async Task<ISymbol> GetFirstSymbolWithName(Project project)
        {
            return (await project.GetCompilationAsync()).GetSymbolsWithName(s => s == Constants.MergedMyNamespace, SymbolFilter.Namespace).FirstOrDefault();
        }
    }
}