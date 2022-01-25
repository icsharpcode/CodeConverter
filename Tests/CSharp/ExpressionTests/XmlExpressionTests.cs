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
        XElement x = new XElement(""h1"", hello);
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
        XElement x = new XElement(""h1"", var1, var2, new XElement(""span"", var2, var1));
    }
}");
        }

        [Fact]
        public async Task MultiLineXmlExpressionsAsync()
        {
            //BUG: Newlines appear as \r\n
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
Dim catalog =
  <?xml version=""1.0""?>
    <Catalog>
      <Book id=""bk101"">
        <Author>Garghentini, Davide</Author>
        <Title>XML Developer's Guide</Title>
        <Price>44.95</Price>
        <Description>
          An in-depth look at creating applications
          with <technology>XML</technology>. For
          <audience>beginners</audience> or
          <audience>advanced</audience> developers.
        </Description>
      </Book>
      <Book id=""bk331"">
        <Author>Spencer, Phil</Author>
        <Title>Developing Applications with Visual Basic .NET</Title>
        <Price>45.95</Price>
        <Description>
          Get the expert insights, practical code samples,
          and best practices you need
          to advance your expertise with <technology>Visual
          Basic .NET</technology>.
          Learn how to create faster, more reliable applications
          based on professional,
          pragmatic guidance by today's top <audience>developers</audience>.
        </Description>
      </Book>
    </Catalog>

Dim htmlOutput =
  <html>
    <body>
      <%= From book In catalog.<Catalog>.<Book>
          Select <div>
                   <h1><%= book.<Title>.Value %></h1>
                   <h3><%= ""By "" & book.<Author>.Value %></h3>
                    <h3><%= ""Price = "" & book.<Price>.Value %></h3>
                    <h2>Description</h2>
                    <%= TransformDescription(book.<Description>(0)) %>
                    <hr/>
                  </div> %>
    </body>
  </html>
    End Sub
End Class", @"using System.Linq;
using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        XDocument catalog = new XDocument(new XElement(""Catalog"", new XElement(""Book"", new XAttribute(""id"", ""bk101""), new XElement(""Author"", ""Garghentini, Davide""), new XElement(""Title"", ""XML Developer's Guide""), new XElement(""Price"", ""44.95""), new XElement(""Description"", ""\r\n          An in-depth look at creating applications\r\n          with "", new XElement(""technology"", ""XML""), "". For\r\n          "", new XElement(""audience"", ""beginners""), "" or\r\n          "", new XElement(""audience"", ""advanced""), "" developers.\r\n        "")), new XElement(""Book"", new XAttribute(""id"", ""bk331""), new XElement(""Author"", ""Spencer, Phil""), new XElement(""Title"", ""Developing Applications with Visual Basic .NET""), new XElement(""Price"", ""45.95""), new XElement(""Description"", ""\r\n          Get the expert insights, practical code samples,\r\n          and best practices you need\r\n          to advance your expertise with "", new XElement(""technology"", ""Visual\r\n          Basic .NET""), "".\r\n          Learn how to create faster, more reliable applications\r\n          based on professional,\r\n          pragmatic guidance by today's top "", new XElement(""audience"", ""developers""), "".\r\n        ""))));
        XElement htmlOutput = new XElement(""html"", new XElement(""body"", from book in catalog.Elements(""Catalog"").Elements(""Book"")
                                                                        select new XElement(""div"", new XElement(""h1"", book.Elements(""Title"").Value), new XElement(""h3"", ""By "" + book.Elements(""Author"").Value), new XElement(""h3"", ""Price = "" + book.Elements(""Price"").Value), new XElement(""h2"", ""Description""), TransformDescription(book.Elements(""Description"").ElementAtOrDefault(0)), new XElement(""hr""))));
    }
}
1 source compilation errors:
BC36610: Name 'TransformDescription' is either not declared or not in the current scope.
2 target compilation errors:
CS1061: 'IEnumerable<XElement>' does not contain a definition for 'Value' and no accessible extension method 'Value' accepting a first argument of type 'IEnumerable<XElement>' could be found (are you missing a using directive or an assembly reference?)
CS0103: The name 'TransformDescription' does not exist in the current context",
hasLineCommentConversionIssue: true);
        }



        [Fact]
        public async Task AssignmentStatementWithXmlElementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b = <someXmlTag></someXmlTag>
        Dim c = <someXmlTag><bla anAttribute=""itsValue"">tata</bla><someContent>tata</someContent></someXmlTag>
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        XElement b = new XElement(""someXmlTag"");
        XElement c = new XElement(""someXmlTag"", new XElement(""bla"", new XAttribute(""anAttribute"", ""itsValue""), ""tata""), new XElement(""someContent"", ""tata""));
    }
}");
        }



        [Fact]
        public async Task AssignmentStatementWithXmlElementAndEmbeddedExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Const value1 = ""something""
        Dim xElement = <Elem1 Attr1=<%= value1 %> Attr2=<%= 100 %>></Elem1>
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        const string value1 = ""something"";
        XElement xElement = new XElement(""Elem1"", new XAttribute(""Attr1"", value1), new XAttribute(""Attr2"", 100));
    }
}");
        }



        [Fact]
        public async Task SelfClosingXmlTagAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()       
        Dim xElement = <Elem1 Attr1=""something"" />
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        XElement xElement = new XElement(""Elem1"", new XAttribute(""Attr1"", ""something""));
    }
}");
        }



        [Fact]
        public async Task ConditionalMemberAccessAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()       
        Dim xDocument = <Test></Test>
        Dim elements1 = xDocument.<Something>.SingleOrDefault()?.<SomethingElse>
    End Sub
End Class", @"using System.Linq;
using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        XElement xDocument = new XElement(""Test"");
        var elements1 = xDocument.Elements(""Something"").SingleOrDefault()?.Elements(""SomethingElse"");
    }
}");
        }
    }
}
