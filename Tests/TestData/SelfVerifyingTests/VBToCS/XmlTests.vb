Imports System
Imports System.Linq
Imports System.Xml.Linq
Imports Xunit

Imports <xmlns:t="http://example.com/test">
Imports <xmlns="http://example.com/">

Public Class XmlTests

    <Fact>
    Sub TestXDocumentWithXmlNamespaceImports()
        Dim document As XDocument =
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <books>
                    <t:book/>
                </books>

        Assert.Equal("<books xmlns:t=""http://example.com/test"" xmlns=""http://example.com/"">
  <t:book />
</books>", document.ToString())
    End Sub


    <Fact>
    Sub TestXDocumentWithXmlNamespaceImportsFromExistingElement()
        Dim body As XElement = <books>
                                   <t:book/>
                               </books>
        Dim document As XDocument =
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <%= body %>

        Assert.Equal("<books xmlns:t=""http://example.com/test"" xmlns=""http://example.com/"">
  <t:book />
</books>", document.ToString())
    End Sub

End Class
