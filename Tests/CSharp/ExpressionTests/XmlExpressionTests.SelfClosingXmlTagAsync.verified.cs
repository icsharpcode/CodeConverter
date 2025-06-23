using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var xElement = new XElement("Elem1", new XAttribute("Attr1", "something"));
    }
}