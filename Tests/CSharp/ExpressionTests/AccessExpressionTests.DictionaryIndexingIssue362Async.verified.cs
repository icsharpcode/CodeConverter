using System.Collections.Generic;
using System.Linq;

internal static partial class Module1
{
    private static Dictionary<int, string> Dict = new Dictionary<int, string>();

    public static void Main()
    {
        int x = Dict.Values.ElementAtOrDefault(0).Length;
    }
}