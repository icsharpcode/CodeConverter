using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class ParameterTests : ConverterTestBase
    {
        [Fact]
        public async Task OptionalParameter_DoesNotThrowInvalidCastExceptionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class MyTestAttribute
    Inherits Attribute
End Class

Public Class MyController
    Public Function GetNothing(
        <MyTest()> Optional indexer As Integer? = 0
    ) As String
        Return Nothing
    End Function
End Class
",
@"using System;

public partial class MyTestAttribute : Attribute
{
}

public partial class MyController
{
    public string GetNothing([MyTest()] int? indexer = 0)
    {
        return null;
    }
}");
        }
    }
}
