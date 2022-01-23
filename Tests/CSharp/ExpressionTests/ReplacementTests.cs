using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class ReplacementTests : ConverterTestBase
    {
        [Fact]
        public async Task SimpleMethodReplacementsWorkAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestSimpleMethodReplacements
    Sub TestMethod()
        Dim str1 As String
        Dim str2 As String
        Dim x As Object
        Dim dt As DateTime
        x = MyProject.Computer.FileSystem.CurrentDirectory()
        x = MyProject.Computer.FileSystem.GetTempFileName()
        x = MyProject.Computer.FileSystem.CombinePath(str1, str2)
        x = MyProject.Computer.FileSystem.GetDirectoryInfo(str1)
        x = MyProject.Computer.FileSystem.GetDriveInfo(str1)
        x = MyProject.Computer.FileSystem.GetFileInfo(str1)
        x = MyProject.Computer.FileSystem.GetName(str1)
        x = MyProject.Computer.FileSystem.ReadAllBytes(str1)
        x = MyProject.Computer.FileSystem.ReadAllText(str1)
        x = MyProject.Computer.FileSystem.DirectoryExists(str1)
        x = MyProject.Computer.FileSystem.FileExists(str1)
        x = MyProject.Computer.FileSystem.DeleteFile(str1)
        x = MyProject.Computer.FileSystem.SpecialDirectories.Temp()
        x = MyProject.Computer.Info.InstalledUICulture()
        x = MyProject.Computer.Info.OSFullName()
        x = MyProject.Computer.Info.SPlatform()
        x = MyProject.Computer.Info.OSVersion()
        x = Microsoft.VisualBasic.DateAndTime.Now()
        x = Microsoft.VisualBasic.DateAndTime.Today()
        x = Microsoft.VisualBasic.DateAndTime.Year(dt)
        x = Microsoft.VisualBasic.DateAndTime.Month(dt)
        x = Microsoft.VisualBasic.DateAndTime.Day(dt)
        x = Microsoft.VisualBasic.DateAndTime.Hour(dt)
        x = Microsoft.VisualBasic.DateAndTime.Minute(dt)
        x = Microsoft.VisualBasic.DateAndTime.Second(dt)
    End Sub
End Class", @"using System;

public partial class TestSimpleMethodReplacements
{
    public void TestMethod()
    {
        var str1 = default(string);
        var str2 = default(string);
        object x;
        var dt = default(DateTime);
        x = MyProject.Computer.FileSystem.CurrentDirectory();
        x = MyProject.Computer.FileSystem.GetTempFileName();
        x = MyProject.Computer.FileSystem.CombinePath(str1, str2);
        x = MyProject.Computer.FileSystem.GetDirectoryInfo(str1);
        x = MyProject.Computer.FileSystem.GetDriveInfo(str1);
        x = MyProject.Computer.FileSystem.GetFileInfo(str1);
        x = MyProject.Computer.FileSystem.GetName(str1);
        x = MyProject.Computer.FileSystem.ReadAllBytes(str1);
        x = MyProject.Computer.FileSystem.ReadAllText(str1);
        x = MyProject.Computer.FileSystem.DirectoryExists(str1);
        x = MyProject.Computer.FileSystem.FileExists(str1);
        x = MyProject.Computer.FileSystem.DeleteFile(str1);
        x = MyProject.Computer.FileSystem.SpecialDirectories.Temp();
        x = MyProject.Computer.Info.InstalledUICulture();
        x = MyProject.Computer.Info.OSFullName();
        x = MyProject.Computer.Info.SPlatform();
        x = MyProject.Computer.Info.OSVersion();
        x = DateTime.Now;
        x = DateTime.Today;
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetYear(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMonth(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetDayOfMonth(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetHour(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMinute(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetSecond(dt);
    }
}
1 source compilation errors:
BC30451: 'MyProject' is not declared. It may be inaccessible due to its protection level.
1 target compilation errors:
CS0103: The name 'MyProject' does not exist in the current context");
        }
    }
}
