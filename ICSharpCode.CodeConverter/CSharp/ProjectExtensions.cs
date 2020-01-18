using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class ProjectExtensions
    {
        public static Project CreateReferenceOnlyProjectFromAnyOptions(this Project project, CompilationOptions baseOptions)
        {
            var options = baseOptions.WithMetadataImportOptionsAll();
            var viewerId = ProjectId.CreateNewId();
            var projectReferences = project.ProjectReferences.Concat(new[] {new ProjectReference(project.Id)});
            var viewerProjectInfo = project.ToProjectInfo(viewerId, project.Name + viewerId, options,
                projectReferences);
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
            var projectInfo = project.ToProjectInfo(project.Id, project.Name, compilationOptions,
                project.ProjectReferences, parseOptions);
            var convertedSolution = project.Solution.RemoveProject(project.Id).AddProject(projectInfo);
            return convertedSolution.GetProject(project.Id);
        }

        public static string GetDirectoryPath(this Project proj)
        {
            string projectFilePath = proj.FilePath;
            return projectFilePath != null ? Path.GetDirectoryName(projectFilePath) : null;
        }

        public static (Project project, List<(string Path, DocumentId DocId, string[] Errors)> firstPassDocIds)
            WithDocuments(this Project project, (string Path, SyntaxNode Node, string[] Errors)[] results)
        {
            var firstPassDocIds = results.Select(firstPassResult =>
            {
                DocumentId docId = null;
                if (firstPassResult.Node != null)
                {
                    var document = project.AddDocument(firstPassResult.Path, firstPassResult.Node,
                        filePath: firstPassResult.Path);
                    project = document.Project;
                    docId = document.Id;
                }

                return (firstPassResult.Path, docId, firstPassResult.Errors);
            }).ToList();

            //ToList ensures that the project returned has all documents added. We only return DocumentIds so it's easy to look up the final version of the doc later
            return (project, firstPassDocIds);
        }

        public static IEnumerable<(string Path, Document Doc, string[] Errors)> GetDocuments(this Project project, List<(string treeFilePath, DocumentId docId, string[] errors)> docIds)
        {
            return docIds.Select(f => (f.treeFilePath, f.docId != null ? project.GetDocument(f.docId) : null, f.errors));
        }
    }
}