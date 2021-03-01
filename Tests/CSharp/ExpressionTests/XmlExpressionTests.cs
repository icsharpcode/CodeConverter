using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    /// <summary>
    /// See https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/xml/how-to-transform-xml-by-using-linq
    /// </summary>
    public class XmlExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task NestedXmlEchoSimpleAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim hello = ""Hello""
        dim x = <h1><%= hello %></h1>
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        string hello = ""Hello"";
        XElement x = XElement.Parse($""<h1>{hello}</h1>"");
    }
}");
        }
        [Fact]
        public async Task TwoLayerNestedXmlWithExpressionsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim var1 = 1
        Dim var2 = 2
        dim x = <h1><%= var1 %><%= var2 %><span><%= var2 %><%= var1 %></span></h1>
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        int var1 = 1;
        int var2 = 2;
        XElement x = XElement.Parse($""<h1>{var1}{var2}<span>{var2}{var1}</span> </h1>"");
    }
}");
        }
    }
}
