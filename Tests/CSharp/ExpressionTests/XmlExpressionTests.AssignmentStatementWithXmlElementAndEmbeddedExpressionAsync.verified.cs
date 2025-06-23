using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        const string value1 = "something";
        var xElement = new XElement("Elem1", new XAttribute("Attr1", value1), new XAttribute("Attr2", 100));
    }
}