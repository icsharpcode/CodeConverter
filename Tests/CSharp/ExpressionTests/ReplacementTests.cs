using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ReplacementTests : ConverterTestBase
{
    [Fact]
    public async Task SimpleMethodReplacementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SimpleMyProjectMethodReplacementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
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