using System.Runtime.InteropServices;

internal partial class Class1
{
    [DllImport("lib.dll")]
    public static extern void External();
}