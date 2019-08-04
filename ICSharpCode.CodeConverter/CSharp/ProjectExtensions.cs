using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal static class ProjectExtensions
    {
        public static async Task<Compilation> CreateReferenceOnlyCompilationFromAnyOptionsAsync(this Project project, CompilationOptions baseOptions)
        {
            var options = baseOptions.WithMetadataImportOptionsAll();
            var viewerId = ProjectId.CreateNewId();
            var projectReferences = project.ProjectReferences.Concat(new[] {new ProjectReference(project.Id)});
            var viewerProjectInfo = ToProjectInfo(project, viewerId, project.Name + viewerId, options,
                projectReferences);
            var csharpViewOfVbProject = project.Solution.AddProject(viewerProjectInfo).GetProject(viewerId);
            return await csharpViewOfVbProject.GetCompilationAsync();
        }

        public static ProjectInfo ToProjectInfo(this Project project, ProjectId projectId, string projectName, CompilationOptions options, IEnumerable<ProjectReference> projectProjectReferences)
        {
            return ProjectInfo.Create(projectId, project.Version, projectName, project.AssemblyName,
                options.Language, null, project.OutputFilePath,
                options, null, new DocumentInfo[0], projectProjectReferences,
                project.MetadataReferences, project.AnalyzerReferences);
        }

        public static Project ToProjectFromAnyOptions(this Project project, CompilationOptions compilationOptions)
        {
            var projectInfo = project.ToProjectInfo(project.Id, project.Name, compilationOptions,
                project.ProjectReferences);
            var convertedSolution = project.Solution.RemoveProject(project.Id).AddProject(projectInfo);
            return convertedSolution.GetProject(project.Id);
        }
    }
}