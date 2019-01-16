using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class SolutionAndProjectTests : ProjectConverterTestBase
    {
        [Fact(Skip = "Hits nullref on appveyor")]
        public void ConvertSolution()
        {
            ConvertProjectsWhere<CSToVBConversion>(p => true);
        }

        [Fact(Skip = "Hits nullref on appveyor")]
        public void ConvertSingleProject()
        {
            ConvertProjectsWhere<CSToVBConversion>(p => p.Name == "CSharpConsoleApp");
        }
    }
}
