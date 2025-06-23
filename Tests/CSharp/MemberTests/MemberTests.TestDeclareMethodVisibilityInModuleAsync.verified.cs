using System.Runtime.InteropServices;

internal static partial class Module1
{
    [DllImport("lib.dll")]
    public static extern void External();
}