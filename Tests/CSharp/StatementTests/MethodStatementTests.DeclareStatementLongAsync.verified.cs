using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

public partial class AcmeClass
{
    [DllImport("user32")]
    private static extern void SetForegroundWindow(int hwnd);

    public static void Main()
    {
        foreach (var proc in Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)))
        {
            SetForegroundWindow(proc.MainWindowHandle.ToInt32());
            Thread.Sleep(1000);
        }
    }
}