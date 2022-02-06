using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class ReplacementTests : ConverterTestBase
    {
        [Fact]
        public async Task SimpleMethodReplacementsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestSimpleMethodReplacements
    Sub TestMethod()
        Dim str1 As String
        Dim str2 As String
        Dim x As Object
        Dim dt As DateTime
        x = Microsoft.VisualBasic.DateAndTime.Now
        x = Microsoft.VisualBasic.DateAndTime.Today
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
        string str1;
        string str2;
        object x;
        var dt = default(DateTime);
        x = DateTime.Now;
        x = DateTime.Today;
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetYear(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMonth(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetDayOfMonth(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetHour(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetMinute(dt);
        x = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetSecond(dt);
    }
}");
        }

        [Fact]
        public async Task SimpleMyProjectMethodReplacementsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Imports System

Public Class SimpleMyProjectMethodReplacementsWork
    Sub TestMethod()
        Dim str1 As String
        Dim str2 As String
        Dim x As Object
        Dim dt As DateTime
        Dim Computer As New Microsoft.VisualBasic.Devices.Computer
        x = Computer.FileSystem.CurrentDirectory()
        x = Computer.FileSystem.GetTempFileName()
        x = Computer.FileSystem.CombinePath(str1, str2)
        x = Computer.FileSystem.GetDirectoryInfo(str1)
        x = Computer.FileSystem.GetDriveInfo(str1)
        x = Computer.FileSystem.GetFileInfo(str1)
        x = Computer.FileSystem.GetName(str1)
        x = Computer.FileSystem.ReadAllBytes(str1)
        x = Computer.FileSystem.ReadAllText(str1)
        x = Computer.FileSystem.DirectoryExists(str1)
        x = Computer.FileSystem.FileExists(str1)
        Computer.FileSystem.DeleteFile(str1)
        x = Computer.FileSystem.SpecialDirectories.Temp
        x = Computer.Info.InstalledUICulture
        x = Computer.Info.OSFullName
        x = Computer.Info.OSPlatform
        x = Computer.Info.OSVersion
    End Sub
End Class", @"using System;
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
}");
        }

        [Fact]
        public async Task AddressOfMyProjectMethodReplacementsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Dim y As Func(Of DateTime, Integer) = AddressOf Microsoft.VisualBasic.DateAndTime.Year",
                @"Func<DateTime, int> y = System.Threading.Thread.CurrentThread.CurrentCulture.Calendar.GetYear;",
                expectSurroundingBlock: true
            );
        }

        [Fact]
        public async Task IsArrayReplacementsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Dim x = Microsoft.VisualBasic.Information.IsArray(New Integer(3))",
                @"bool x = new int(3) is Array;",
                expectSurroundingBlock: true
            );
        }

        [Fact]
        public async Task IsDbNullReplacementsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Dim x = Microsoft.VisualBasic.Information.IsDBNull(New Object())",
                @"bool x = new object() is DBNull;",
                expectSurroundingBlock: true
            );
        }

        [Fact]
        public async Task IsNothingReplacementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Dim x = Microsoft.VisualBasic.Information.IsNothing(New Object())",
                @"bool x = new object() == null;",
                expectSurroundingBlock: true
            );
        }

        [Fact]
        public async Task IsErrorReplacementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Dim x = Microsoft.VisualBasic.Information.IsError(New Object())",
                @"bool x = new object() is Exception;",
                expectSurroundingBlock: true
            );
        }
    }
}
