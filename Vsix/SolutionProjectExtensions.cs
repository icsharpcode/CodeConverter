using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace CodeConverter.VsExtension
{
    /// <summary>
    /// http://www.wwwlicious.com/2011/03/29/envdte-getting-all-projects-html/#comment-2557459433
    /// </summary>
    public static class SolutionProjectExtensions
    {
        public static IEnumerable<Project> GetAllProjects(this Solution sln)
        {
            return sln.Projects.Cast<Project>().SelectMany(GetProjects);
        }

        public static IEnumerable<Project> GetProjects(this Project project)
        {
            if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder) {
                return project.ProjectItems.Cast<ProjectItem>()
                    .Select(x => x.SubProject).Where(x => x != null)
                    .SelectMany(GetProjects);
            }
            return new[] { project };
        }
    }
}