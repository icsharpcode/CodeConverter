using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;
using static ICSharpCode.CodeConverter.CommandLine.CodeConvProgram;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    /// <summary>
    /// <see cref="MultiFileTestFixture"/> for info on how these tests work.
    /// </summary>
    [Collection(MultiFileTestFixture.Collection)]
    public class MultiFileSolutionAndProjectTests
    {
        private readonly MultiFileTestFixture _multiFileTestFixture;

        public MultiFileSolutionAndProjectTests(MultiFileTestFixture multiFileTestFixture)
        {
            _multiFileTestFixture = multiFileTestFixture;
        }

        [Fact]
        public async Task ConvertWholeSolution()
        {
            await _multiFileTestFixture.ConvertProjectsWhere(p => true, Language.CS);
        }

        [Fact]
        public async Task ConvertVbLibraryOnly()
        {
            await _multiFileTestFixture.ConvertProjectsWhere(p => p.Name == "VbLibrary", Language.CS);
        }
    }
}
