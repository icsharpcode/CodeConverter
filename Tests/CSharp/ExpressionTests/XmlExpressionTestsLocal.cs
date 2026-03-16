using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;
using System.Threading.Tasks;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class XmlExpressionTestsLocal : ConverterTestBase
{
    [Fact]
    public async Task XmlLiteralDecodingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim v = <xml>&lt;</xml>.Value
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        string v = new XElement(""xml"", ""<"").Value;
    }
}");
    }
}
