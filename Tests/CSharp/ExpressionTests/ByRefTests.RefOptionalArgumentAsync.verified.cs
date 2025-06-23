using System.Runtime.InteropServices;

public partial class OptionalRefIssue91
{
    public static bool TestSub([Optional, DefaultParameterValue(false)] ref bool IsDefault)
    {
        return default;
    }

    public static bool CallingFunc()
    {
        bool argIsDefault = false;
        bool argIsDefault1 = true;
        return TestSub(IsDefault: ref argIsDefault) && TestSub(ref argIsDefault1);
    }
}