using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class ProjectExtensions
    {
        private static char[] DirSeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        public static Project CreateReferenceOnlyProjectFromAnyOptions(this Project project, CompilationOptions baseOptions, ParseOptions parseOptions)
        {
            var options = baseOptions.WithMetadataImportOptions(MetadataImportOptions.All);
            var viewerId = ProjectId.CreateNewId();
            var projectReferences = project.ProjectReferences.Concat(new[] {new ProjectReference(project.Id)});
            var viewerProjectInfo = project.ToProjectInfo(viewerId, project.Name + viewerId, options,
                projectReferences, parseOptions);
            var csharpViewOfVbProject = project.Solution.AddProject(viewerProjectInfo).GetProject(viewerId);
            return csharpViewOfVbProject;
        }

        public static ProjectInfo ToProjectInfo(this Project project, ProjectId projectId, string projectName,
            CompilationOptions options, IEnumerable<ProjectReference> projectProjectReferences,
            ParseOptions parseOptions = null)
        {
            return ProjectInfo.Create(projectId, project.Version, projectName, project.AssemblyName,
                options.Language, null, project.OutputFilePath,
                options, parseOptions, System.Array.Empty<DocumentInfo>(), projectProjectReferences,
                project.MetadataReferences, project.AnalyzerReferences);
        }

        public static Project ToProjectFromAnyOptions(this Project project, CompilationOptions compilationOptions, ParseOptions parseOptions)
        {
            // Use a new id to workaround VS caching issue first reported here: https://github.com/icsharpcode/CodeConverter/issues/586
            var newProjectId = ProjectId.CreateNewId("ConvertedProject");
            var projectInfo = project.ToProjectInfo(newProjectId, project.Name, compilationOptions,
                project.ProjectReferences, parseOptions);
            var convertedSolution = project.Solution.RemoveProject(project.Id).AddProject(projectInfo);
            return convertedSolution.GetProject(newProjectId);
        }

        public static string GetDirectoryPath(this Project proj)
        {
            string projectFilePath = proj.FilePath;
            if (projectFilePath != null) {
                return Path.GetDirectoryName(projectFilePath);
            }

            string solutionPath = GetDirectoryPath(proj.Solution);
            return proj.Documents
                .Where(d => d.FilePath != null && d.FilePath.StartsWith(solutionPath))
                .Select(d => d.FilePath.Replace(solutionPath, "").TrimStart(DirSeparators))
                .Where(p => p.IndexOfAny(DirSeparators) > -1)
                .Select(p => p.Split(DirSeparators).First())
                .OrderByDescending(p => p.Contains(proj.AssemblyName))
                .FirstOrDefault() ?? solutionPath;
        }

        public static string GetDirectoryPath(this Solution soln)
        {
            // Find a directory for projects that don't have a projectfile (e.g. websites) Current dir if in memory
            return soln.FilePath != null ? Path.GetDirectoryName(soln.FilePath) : Directory.GetCurrentDirectory();
        }

        public static (Project project, List<WipFileConversion<DocumentId>> firstPassDocIds)
            WithDocuments(this Project project, WipFileConversion<SyntaxNode>[] results)
        {
            var firstPassDocIds = results.Select(firstPassResult =>
            {
                DocumentId docId = null;
                if (firstPassResult.Wip != null)
                {
                    docId = DocumentId.CreateNewId(project.Id);
                    var solution = project.Solution.AddDocument(docId, firstPassResult.SourcePath, firstPassResult.Wip,
                        filePath: firstPassResult.SourcePath);
                    project = solution.GetProject(project.Id);
                }

                return firstPassResult.With(docId);
            }).ToList();

            //ToList ensures that the project returned has all documents added. We only return DocumentIds so it's easy to look up the final version of the doc later
            return (project, firstPassDocIds);
        }

        public static IEnumerable<WipFileConversion<Document>> GetDocuments(this Project project, List<WipFileConversion<DocumentId>> docIds)
        {
            return docIds.Select(f => f.With(f.Wip != null ? project.GetDocument(f.Wip) : null));
        }
    }
}