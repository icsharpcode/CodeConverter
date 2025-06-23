
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

public partial class SimpleMyProjectMethodReplacementsWork
{
    public void TestMethod()
    {
        var str1 = default(string);
        var str2 = default(string);
        object x;
        DateTime dt;
        var Computer = new Microsoft.VisualBasic.Devices.Computer();
        x = Directory.GetCurrentDirectory();
        x = Path.GetTempFileName();
        x = Path.Combine(str1, str2);
        x = new DirectoryInfo(str1);
        x = new DriveInfo(str1);
        x = new FileInfo(str1);
        x = Path.GetFileName(str1);
        x = File.ReadAllBytes(str1);
        x = File.ReadAllText(str1);
        x = Directory.Exists(str1);
        x = File.Exists(str1);
        File.Delete(str1);
        x = Path.GetTempPath();
        x = CultureInfo.InstalledUICulture;
        x = RuntimeInformation.OSDescription;
        x = Computer.Info.OSPlatform;
        x = Environment.OSVersion.Version.ToString();
    }
}