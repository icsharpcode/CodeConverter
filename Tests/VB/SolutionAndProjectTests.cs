using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace CodeConverter.Tests.VB
{
    [Collection(MsBuildFixture.Collection)]
    public class SolutionAndProjectTests
    {
        private readonly MsBuildFixture _msBuildFixture;

        public SolutionAndProjectTests(MsBuildFixture msBuildFixture)
        {
            _msBuildFixture = msBuildFixture;
        }

        [Fact]
        public async Task ConvertSolution()
        {
            await _msBuildFixture.ConvertProjectsWhere<CSToVBConversion>(p => true);
        }

        [Fact]
        public async Task ConvertSingleProject()
        {
            await _msBuildFixture.ConvertProjectsWhere<CSToVBConversion>(p => p.Name == "CSharpConsoleApp");
        }
    }
}
