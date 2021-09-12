using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class ParameterTests : ConverterTestBase
    {
        [Fact]
        public async Task OptionalParameter_DoesNotThrowInvalidCastException()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class MyAttribute
    Inherits Attribute
End Class

Public Class MyController
    Public Function GetNothing(
        <MyAttribute()> Optional indexer As Integer? = 0
    ) As String
        Return Nothing
    End Function
End Class
",
@"using System;

public partial class MyAttribute : Attribute
{
}

public partial class MyController
{
    public string GetNothing([My()] int? indexer = 0)
    {
        return null;
    }
}");
        }
    }
}
