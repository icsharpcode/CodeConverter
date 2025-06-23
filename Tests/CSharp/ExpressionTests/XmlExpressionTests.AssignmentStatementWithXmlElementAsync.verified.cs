using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        var b = new XElement("someXmlTag");
        var c = new XElement("someXmlTag", new XElement("bla", new XAttribute("anAttribute", "itsValue"), "tata"), new XElement("someContent", "tata"));
    }
}