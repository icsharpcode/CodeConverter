using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB.MissingSemanticModelInfo;

public class ExpressionTests : ConverterTestBase
{

    [Fact]
    public async Task AmpersandToStringOnUnknownTypeAsync()
    {
        await TestConversionCSharpToVisualBasicAsync(@"
class TestClass
{
    string TestMethod()
    {
        return ""Conversion warning: Qualified name reduction failed for this file. "" + ex;
    }
}", @"
Friend Class TestClass
    Private Function TestMethod() As String
        Return ""Conversion warning: Qualified name reduction failed for this file. "" & ex.ToString()
    End Function
End Class

1 source compilation errors:
CS0103: The name 'ex' does not exist in the current context
1 target compilation errors:
BC30451: 'ex' is not declared. It may be inaccessible due to its protection level.", expectCompilationErrors: true);
    }
}