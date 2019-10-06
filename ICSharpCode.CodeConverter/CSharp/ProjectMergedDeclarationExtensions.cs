using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class ProjectMergedDeclarationExtensions
    {
        private static Func<Location, SyntaxTree> _getEmbeddedSyntaxTree;

        public static async Task<Project> WithFilePathsForEmbeddedDocuments(this Project vbProject)
        {
            int mergedDeclarationCount = 1;
            var compilation = await vbProject.GetCompilationAsync();
            var ns = compilation.SourceModule.GlobalNamespace;

            var projectDir = Path.GetDirectoryName(vbProject.FilePath);
            var roots = await ns.Locations.Where(l => !l.IsInSource).Select(GetEmbeddedSyntaxTree).SelectAsync(t => t.GetTextAsync());
            foreach (var root in roots) {
                string name = "MergedDeclaration" + mergedDeclarationCount++;
                var modifiedText = root.ToString().Replace("Namespace My", $"Namespace {Constants.MergedMyNamespace}My");
                vbProject = vbProject.AddDocument(name, modifiedText, filePath: Path.Combine(projectDir, name + ".vb")).Project;
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
    }
}