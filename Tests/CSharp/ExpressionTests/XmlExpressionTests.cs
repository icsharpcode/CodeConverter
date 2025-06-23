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
        var x = new XElement(""h1"", hello);
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
        var x = new XElement(""h1"", var1, var2, new XElement(""span"", var2, var1));
    }
}");
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
        var b = new XElement(""someXmlTag"");
        var c = new XElement(""someXmlTag"", new XElement(""bla"", new XAttribute(""anAttribute"", ""itsValue""), ""tata""), new XElement(""someContent"", ""tata""));
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
        var xElement = new XElement(""Elem1"", new XAttribute(""Attr1"", value1), new XAttribute(""Attr2"", 100));
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
        var xElement = new XElement(""Elem1"", new XAttribute(""Attr1"", ""something""));
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
        var xDocument = new XElement(""Test"");
        var elements1 = xDocument.Elements(""Something"").SingleOrDefault()?.Elements(""SomethingElse"");
    }
}");

    }



    [Fact]
    public async Task NamespaceImportAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"' Place Imports statements at the top of your program.  
Imports <xmlns=""http://DefaultNamespace"">
Imports <xmlns:ns=""http://NewNamespace"">

Module Module1

  Sub Main()
    ' Create element by using the default global XML namespace. 
    Dim inner = <innerElement/>

    ' Create element by using both the default global XML namespace and the namespace identified with the ""ns"" prefix.
    Dim outer = <ns:outer><ns:innerElement attr=""value""></ns:innerElement><siblingElement></siblingElement><%= inner %></ns:outer>

    ' Display element to see its final form. 
    Console.WriteLine(outer)
  End Sub

End Module", @"using System;
using System.Xml.Linq;
using XmlImports = XmlImportsCodeToConvert;

internal static class XmlImportsCodeToConvert
{
    // Place Imports statements at the top of your program.  
    internal static readonly XNamespace Default = ""http://DefaultNamespace"";
    internal static readonly XNamespace ns = ""http://NewNamespace"";
    private static readonly XAttribute[] namespaceAttributes = {
        new XAttribute(""xmlns"", Default.NamespaceName),
        new XAttribute(XNamespace.Xmlns + ""ns"", ns.NamespaceName)
    };

    internal static XElement Apply(XElement x)
    {
        foreach (var d in x.DescendantsAndSelf())
        {
            foreach (var n in namespaceAttributes)
            {
                var a = d.Attribute(n.Name);
                if (a != null && a.Value == n.Value)
                {
                    a.Remove();
                }
            }
        }
        x.Add(namespaceAttributes);
        return x;
    }

    internal static XDocument Apply(XDocument x)
    {
        Apply(x.Root);
        return x;
    }
}

internal static partial class Module1
{

    public static void Main()
    {
        // Create element by using the default global XML namespace. 
        var inner = XmlImports.Apply(new XElement(XmlImports.Default + ""innerElement""));

        // Create element by using both the default global XML namespace and the namespace identified with the ""ns"" prefix.
        var outer = XmlImports.Apply(new XElement(XmlImports.ns + ""outer"", new XElement(XmlImports.ns + ""innerElement"", new XAttribute(""attr"", ""value"")), new XElement(XmlImports.Default + ""siblingElement""), inner));

        // Display element to see its final form. 
        Console.WriteLine(outer);
    }

}");
    }

    [Fact]
    public async Task XmlBuiltinNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()       
        Dim xElement = <Name xml:lang=""de"">Beispiel</Name>
        Dim xElement2 = <Name xmlns:ns1=""http://www.example.com/namespace/1"">Beispiel</Name>
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var xElement = new XElement(""Name"", new XAttribute(XNamespace.Xml + ""lang"", ""de""), ""Beispiel"");
        var xElement2 = new XElement(""Name"", new XAttribute(XNamespace.Xmlns + ""ns1"", ""http://www.example.com/namespace/1""), ""Beispiel"");
    }
}");

    }

    [Fact]
    public async Task XmlCDataAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()       
       Dim processedHtml = <![CDATA[
<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
	<head>
		<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" /><title>
	</head>
	<body>
		<p class=""cs95E872D0""><span class=""cs9D249CCB"">&nbsp;Regards,</span></p>
    </body>
</html>
    ]]>.Value()
    End Sub
End Class", @"using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        string processedHtml = new XCData(@""
<!DOCTYPE html PUBLIC """"-//W3C//DTD XHTML 1.0 Transitional//EN"""" """"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"""">
<html xmlns=""""http://www.w3.org/1999/xhtml"""">
	<head>
		<meta http-equiv=""""Content-Type"""" content=""""text/html; charset=utf-8"""" /><title>
	</head>
	<body>
		<p class=""""cs95E872D0""""><span class=""""cs9D249CCB"""">&nbsp;Regards,</span></p>
    </body>
</html>
    "").Value;
    }
}");

    }
}