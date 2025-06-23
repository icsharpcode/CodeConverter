using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

/// <summary>
/// See https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/xml/how-to-transform-xml-by-using-linq
/// </summary>
public class XmlExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task NestedXmlEchoSimpleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TwoLayerNestedXmlWithExpressionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultiLineXmlExpressionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq

Class TestClass
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
                        
    Public Function TransformDescription(s As String) As String
        Return s
    End Function
End Class", @"using System.Linq;
using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var catalog = new XDocument(
new XElement(""Catalog"",
new XElement(""Book"", new XAttribute(""id"", ""bk101""),
new XElement(""Author"", ""Garghentini, Davide""),
new XElement(""Title"", ""XML Developer's Guide""),
new XElement(""Price"", ""44.95""),
new XElement(""Description"", @""
          An in-depth look at creating applications
          with "", new XElement(""technology"", ""XML""), @"". For
          "", new XElement(""audience"", ""beginners""), @"" or
          "", new XElement(""audience"", ""advanced""), @"" developers.
        "")
),
new XElement(""Book"", new XAttribute(""id"", ""bk331""),
new XElement(""Author"", ""Spencer, Phil""),
new XElement(""Title"", ""Developing Applications with Visual Basic .NET""),
new XElement(""Price"", ""45.95""),
new XElement(""Description"", @""
          Get the expert insights, practical code samples,
          and best practices you need
          to advance your expertise with "", new XElement(""technology"", @""Visual
          Basic .NET""), @"".
          Learn how to create faster, more reliable applications
          based on professional,
          pragmatic guidance by today's top "", new XElement(""audience"", ""developers""), @"".
        "")
)
)

);
        var htmlOutput = new XElement(""html"",

                  new XElement(""body"", from book in catalog.Elements(""Catalog"").Elements(""Book"")
                                       select new XElement(""div"",
                 new XElement(""h1"", book.Elements(""Title"").Value),
                 new XElement(""h3"", ""By "" + book.Elements(""Author"").Value),
                 new XElement(""h3"", ""Price = "" + book.Elements(""Price"").Value),


                                                  new XElement(""h2"", ""Description""), TransformDescription((string)book.Elements(""Description"").ElementAtOrDefault(0)), new XElement(""hr"")
                 )
                    )
                );
    }

    public string TransformDescription(string s)
    {
        return s;
    }
}
1 target compilation errors:
CS1061: 'IEnumerable<XElement>' does not contain a definition for 'Value' and no accessible extension method 'Value' accepting a first argument of type 'IEnumerable<XElement>' could be found (are you missing a using directive or an assembly reference?)", false, incompatibleWithAutomatedCommentTesting: true /* auto-testing of comments doesn't work because it tries to put VB comments inside the xml literal */);
        //BUG: See compilation error
    }



    [Fact]
    public async Task AssignmentStatementWithXmlElementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task AssignmentStatementWithXmlElementAndEmbeddedExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SelfClosingXmlTagAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }



    [Fact]
    public async Task ConditionalMemberAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();

    }



    [Fact]
    public async Task NamespaceImportAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task XmlBuiltinNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();

    }

    [Fact]
    public async Task XmlCDataAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();

    }
}