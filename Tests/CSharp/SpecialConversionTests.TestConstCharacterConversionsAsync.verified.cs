using System.Data;

internal partial class TestConstCharacterConversions
{
    public object GetItem(DataRow dr)
    {
        const string a = "\a";
        const string b = "\b";
        const string t = "\t";
        const string n = "\n";
        const string v = "\v";
        const string f = "\f";
        const string r = "\r";
        const string x = "\u000e";
        const string 字 = "字";
        return default;
    }
}