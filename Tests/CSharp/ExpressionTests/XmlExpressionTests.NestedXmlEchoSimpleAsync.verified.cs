using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        string hello = "Hello";
        var x = new XElement("h1", hello);
    }
}