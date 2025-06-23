using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class TriviaTests : ConverterTestBase
{
    [Fact]
    public async Task Issue506_IfStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue15_NestedRegionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RegionsWithEventsIssue772Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue15_IfTrueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue15_IfFalseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue771_DoNotTrimLineCommentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue771_DoNotTrimBlockCommentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodXmlDocAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestGeneratedMethodXmlDocAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task StatementNewlinesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue1017_TestRegionsMovingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestCarryingOverErrorTriviaAddedByConverterAsync()
    {
        var vbCode = @"Public Class VisualBasicClass
    Public Class TestClass
        Public Property A As Integer
        Public Property B As integer
    End class
   Public Sub New()
        On Error Resume Next
   End Sub
End Class";

        var options = new TextConversionOptions(DefaultReferences.NetStandard2);
        var conversionResult = await ProjectConversion.ConvertTextAsync<VBToCSConversion>(vbCode, options);

        var regex = new Regex(@"#error Cannot convert \w+ - see comment for details\s+ \/\* Cannot convert.*?\*\/", RegexOptions.Singleline);
        Assert.Single(conversionResult.Exceptions);
        Assert.Matches(regex, conversionResult.ConvertedCode);
    }
}