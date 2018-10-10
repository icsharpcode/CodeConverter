using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class SolutionAndProjectTests : ProjectConverterTestBase
    {
        [Fact(Skip = "https://github.com/icsharpcode/CodeConverter/issues/184")]
        public void ConvertSolution()
        {
            ConvertProjectsWhere<CSToVBConversion>(p => true);
        }

        [Fact(Skip = "https://github.com/icsharpcode/CodeConverter/issues/184")]
        public void ConvertSingleProject()
        {
            ConvertProjectsWhere<CSToVBConversion>(p => p.Name == "CSharpConsoleApp");
        }
    }
}
