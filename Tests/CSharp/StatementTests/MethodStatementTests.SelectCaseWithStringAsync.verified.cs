using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class TestClass
{
    public static string TimeAgo(string x)
    {
        switch (Strings.UCase(x) ?? "")
        {
            case var @case when @case == (Strings.UCase("a") ?? ""):
            case var case1 when case1 == (Strings.UCase("b") ?? ""):
                {
                    return "ab";
                }
            case var case2 when case2 == (Strings.UCase("c") ?? ""):
                {
                    return "c";
                }
            case "d":
                {
                    return "d";
                }

            default:
                {
                    return "e";
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code