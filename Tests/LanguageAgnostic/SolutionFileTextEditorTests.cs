using System.Linq;
using ICSharpCode.CodeConverter.Shared;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic
{
    public class SolutionFileTextEditorTests
    {
        [Fact]
        public void WhenInSolutionBaseDirThenUpdated()
        {
            AssertProjectReplacement(SolutionProjectReferenceLine("VbLibrary", @"VbLibrary.vbproj"),
                UpdatedSolutionProjectReferenceLine("VbLibrary", @"VbLibrary.csproj"),
                (FilePath: @"C:\MySolution\VbLibrary.vbproj", DirectoryPath: @"C:\MySolution\"));
        }
        [Fact]
        public void WhenInProjectFolderThenUpdated()
        {
            AssertProjectReplacement(SolutionProjectReferenceLine("VbLibrary", @"VbLibrary\VbLibrary.vbproj"),
                UpdatedSolutionProjectReferenceLine("VbLibrary", @"VbLibrary\VbLibrary.csproj"),
                (FilePath: @"C:\MySolution\VbLibrary\VbLibrary.vbproj", DirectoryPath: @"C:\MySolution\VbLibrary\"));
        }

        [Fact]
        public void GivenDifferentLibraryThenNotUpdated()
        {
            string unaffectedProject = SolutionProjectReferenceLine("Prefix.VbLibrary", @"Prefix.VbLibrary\Prefix.VbLibrary.vbproj");
            AssertProjectReplacement(unaffectedProject, unaffectedProject, (FilePath: @"C:\MySolution\VbLibrary\VbLibrary.vbproj", DirectoryPath: @"C:\MySolution\VbLibrary\"));
        }

        [Fact]
        public void GivenDifferentLibraryThenNotUpdated2()
        {
            string unaffectedProject = SolutionProjectReferenceLine("VbLibrary", @"VbLibrary\VbLibrary.vbproj");
            AssertProjectReplacement(unaffectedProject, unaffectedProject, (FilePath: @"C:\MySolution\Prefix.VbLibrary\Prefix.VbLibrary.vbproj", DirectoryPath: @"C:\MySolution\Prefix.VbLibrary\"));
        }

        [Fact]
        public void GivenDifferentLibraryWhenInSolutionBaseDirThenNotUpdated()
        {
            string unaffectedProject = SolutionProjectReferenceLine("Prefix.VbLibrary", @"Prefix.VbLibrary.vbproj");
            AssertProjectReplacement(unaffectedProject, unaffectedProject, (FilePath: @"C:\MySolution\VbLibrary.vbproj", DirectoryPath: @"C:\MySolution\"));
        }

        [Fact]
        public void GivenDifferentLibraryWhenInSolutionBaseDirThenNotUpdated2()
        {
            string unaffectedProject = SolutionProjectReferenceLine("VbLibrary", @"VbLibrary.vbproj");
            AssertProjectReplacement(unaffectedProject, unaffectedProject, (FilePath: @"C:\MySolution\Prefix.VbLibrary.vbproj", DirectoryPath: @"C:\MySolution\"));
        }

        private static string SolutionProjectReferenceLine(string projectName, string projRelativePath) =>
            $@"Project(""{{F184B08F-C81C-45F6-A57F-5ABD9991F28F}}"") = ""{projectName}"", ""{projRelativePath}"", ""{{CFAB82CD-BA17-4F08-99E2-403FADB0C46A}}""";

        private static string UpdatedSolutionProjectReferenceLine(string projectName, string projRelativePath) =>
            $@"Project(""{{F184B08F-C81C-45F6-A57F-5ABD9991F28F}}"") = ""{projectName}"", ""{projRelativePath}"", ""{{7D8F03A6-764F-00F9-1BBA-8A41CF27F0CA}}""";

        private static void AssertProjectReplacement(string originalText, string expectedOutput, params (string FilePath, string DirectoryPath)[] projects)
        {
            var replacements = SolutionFileTextEditor.GetProjectReferenceReplacements(projects, originalText).ToList();
            var actualOutput = TextReplacementConverter.Replace(originalText, replacements);
            Assert.Equal(Utils.HomogenizeEol(expectedOutput), Utils.HomogenizeEol(actualOutput));
        }
    }
}
