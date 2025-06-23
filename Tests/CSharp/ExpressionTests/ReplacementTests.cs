using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ReplacementTests : ConverterTestBase
{
    [Fact]
    public async Task SimpleMethodReplacementsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SimpleMyProjectMethodReplacementsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
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

    [Fact]
    public async Task MyDocumentsReplacementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim x = Microsoft.VisualBasic.MyServices.SpecialDirectoriesProxy.MyDocuments",
            @"string x = Environment.GetFolderPath(Environment.SpecialFolder.Personal);",
            expectSurroundingBlock: true
        );
    }
}