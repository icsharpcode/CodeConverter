using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class SolutionAndProjectTests : ProjectConverterTestBase
    {
        [Fact]
        public async Task ConvertSolution()
        {
            await ConvertProjectsWhere<CSToVBConversion>(p => true);
        }

        [Fact]
        public async Task ConvertSingleProject()
        {
            await ConvertProjectsWhere<CSToVBConversion>(p => p.Name == "CSharpConsoleApp");
        }
    }
}
