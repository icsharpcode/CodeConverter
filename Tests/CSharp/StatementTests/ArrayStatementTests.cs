using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class ArrayStatementTests : ConverterTestBase
{

    [Fact]
    public async Task ValuesOfArrayAssignmentWithSurroundingClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NonDefaultTypedArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayDeclarationWithRangeStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InitializeArrayOfArraysAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayEraseAndRedimStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Redim2dArrayAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RedimArrayOfGenericsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    [Fact]
    public async Task ArrayInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayInitializationStatementInVarDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayInitializationStatementWithTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue554_AvoidImplicitArrayTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayInitializationStatementWithLengthAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ArrayInitializationStatementWithLengthAndNoValuesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    /// <summary>
    /// Inspired by: https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/arrays/
    /// </summary>
    [Fact]
    public async Task LotsOfArrayInitializationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultidimensionalArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultidimensionalArrayInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultidimensionalArrayInitializationStatementWithTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultidimensionalArrayInitializationStatementWithAndWithoutLengthsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task JaggedArrayDeclarationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task JaggedArrayInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task JaggedArrayInitializationStatementWithTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task JaggedArrayInitializationStatementWithLengthAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SplitArrayDeclarationsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}