using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using Xunit;
using static ICSharpCode.CodeConverter.CommandLine.CodeConvProgram;

namespace ICSharpCode.CodeConverter.Tests.VB
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
        public async Task ConvertWholeSolutionAsync()
        {
            await _multiFileTestFixture.ConvertProjectsWhereAsync(p => true, Language.VB);
        }

        [Fact]
        public async Task ConvertCSharpConsoleAppOnlyAsync()
        {
            await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name == "CSharpConsoleApp", Language.VB);
        }
    }
}
