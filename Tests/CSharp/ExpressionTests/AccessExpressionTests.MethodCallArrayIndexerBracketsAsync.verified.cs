
public partial class A
{
    public void Test()
    {
        string str1 = GetStringFromNone()[0];
        str1 = GetStringFromNone()[0];
        string str2 = GetStringFromNone()[1];
        string[] str3 = GetStringsFromString("abc");
        str3 = GetStringsFromString("abc");
        string str4 = GetStringsFromString("abc")[1];
        string fromStr3 = GetMoreStringsFromString("bc")[1][0];
        string explicitNoParameter = GetStringsFromAmbiguous()[0][1];
        string usesParameter1 = GetStringsFromAmbiguous(0)[1][2];
    }

    public string[] GetStringFromNone()
    {
        return new string[] { "A", "B", "C" };
    }

    public string[] GetStringsFromString(string parm)
    {
        return new string[] { "1", "2", "3" };
    }

    public string[][] GetMoreStringsFromString(string parm)
    {
        return new string[][] { new string[] { "1" } };
    }

    public string[][] GetStringsFromAmbiguous()
    {
        return new string[][] { new string[] { "1" } };
    }

    public string[][] GetStringsFromAmbiguous(int amb)
    {
        return new string[][] { new string[] { "1" } };
    }
}