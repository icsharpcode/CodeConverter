using System.Xml.Linq;

internal partial class TestClass
{
    private void TestMethod()
    {
        int var1 = 1;
        int var2 = 2;
        var x = new XElement("h1", var1, var2, new XElement("span", var2, var1));
    }
}