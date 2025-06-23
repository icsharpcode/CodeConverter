using System.Linq;
using System.Xml.Linq;

public partial class Class1
{
    private void LoadValues(string strPlainKey)
    {
        var xmlFile = XDocument.Parse(strPlainKey);
        var objActivationInfo = xmlFile.Elements("ActivationKey").First();
    }
}