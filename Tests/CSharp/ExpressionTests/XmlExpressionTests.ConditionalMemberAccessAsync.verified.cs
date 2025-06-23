using System.Linq;
using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var xDocument = new XElement("Test");
        var elements1 = xDocument.Elements("Something").SingleOrDefault()?.Elements("SomethingElse");
    }
}