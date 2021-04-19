using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

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

        //[Fact] /* enable for executing locally */
        [Fact(Skip = "Doesn't work on Github actions agent")] /* disable for executing locally */
        public async Task ConvertWholeSolutionAsync()
        {

            await _multiFileTestFixture.ConvertProjectsWhereAsync(p => true, Language.CS);
        }

        //[Fact] /* enable for executing locally */
        [Fact(Skip = "Doesn't work on Github actions agent")]  /* disable for executing locally */
        public async Task ConvertVbLibraryOnlyAsync()
        {
            await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name == "VbLibrary", Language.CS);
        }
    }
}
