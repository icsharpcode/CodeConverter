using System;
using System.Xml.Linq;
using XmlImports = XmlImportsCodeToConvert;

internal static class XmlImportsCodeToConvert
{
    // Place Imports statements at the top of your program.  
    internal static readonly XNamespace Default = "http://DefaultNamespace";
    internal static readonly XNamespace ns = "http://NewNamespace";
    private static readonly XAttribute[] namespaceAttributes = {
        new XAttribute("xmlns", Default.NamespaceName),
        new XAttribute(XNamespace.Xmlns + "ns", ns.NamespaceName)
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
        var inner = XmlImports.Apply(new XElement(XmlImports.Default + "innerElement"));

        // Create element by using both the default global XML namespace and the namespace identified with the "ns" prefix.
        var outer = XmlImports.Apply(new XElement(XmlImports.ns + "outer", new XElement(XmlImports.ns + "innerElement", new XAttribute("attr", "value")), new XElement(XmlImports.Default + "siblingElement"), inner));

        // Display element to see its final form. 
        Console.WriteLine(outer);
    }

}