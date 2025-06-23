using System.Runtime.InteropServices;

public partial class AcmeClass
{
    [DllImport("CP210xManufacturing.dll", EntryPoint = "CP210x_GetNumDevices", CharSet = CharSet.Ansi)]
    internal static extern int GetNumDevices(ref string NumDevices);
}