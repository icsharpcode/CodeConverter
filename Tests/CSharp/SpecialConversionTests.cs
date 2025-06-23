using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class SpecialConversionTests : ConverterTestBase
{
    [Fact]
    public async Task RaiseEventAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestCustomEventAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestFullWidthCharacterCustomEventAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task HexAndBinaryLiteralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task HexAndBinaryLiterals754Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue483_HexAndBinaryLiteralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue544_AssignUsingMidAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue1147_LargeNumericHexLiteralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task TestConstCharacterConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestNonConstCharacterConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestNonVisualBasicChrMethodConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task UsingBoolInToExpressionAsync()
    {
        // Beware, this will never enter the loop, it's buggy input due to the "i <", but it compiles and runs, so the output should too (and do the same thing)
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task StringOperatorsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}