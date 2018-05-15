using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class SolutionAndProjectTests : ProjectConverterTestBase
    {
        [Fact]
        public void ConvertSolution()
        {
            ConvertProjectsWhere<CSToVBConversion>(p => true);
        }

        [Fact]
        public void ConvertSingleProject()
        {
            ConvertProjectsWhere<CSToVBConversion>(p => p.Name == "CSharpConsoleApp");
        }
    }
}
