using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class StandaloneExpressionTests : ConverterTestBase
    {
        [Fact]
        public void Reassignment()
        {
            TestConversionVisualBasicToCSharp(
@"Dim num as Integer = 4
num = 5",
@"int num = 4;
num = 5;",
standaloneStatements: true);
        }
    }
}
