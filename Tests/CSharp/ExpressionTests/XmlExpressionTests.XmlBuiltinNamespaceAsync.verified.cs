using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var xElement = new XElement("Name", new XAttribute(XNamespace.Xml + "lang", "de"), "Beispiel");
        var xElement2 = new XElement("Name", new XAttribute(XNamespace.Xmlns + "ns1", "http://www.example.com/namespace/1"), "Beispiel");
    }
}