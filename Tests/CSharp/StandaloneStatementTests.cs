using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class StandaloneStatementTests : ConverterTestBase
{
    [Fact]
    public async Task ReassignmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim num as Integer = 4
num = 5",
            @"int num = 4;
num = 5;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task CallAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Call mySuperFunction",
            @"mySuperFunction();",
            expectSurroundingBlock: true, missingSemanticInfo: true);
    }

    [Fact]
    public async Task ObjectMemberInitializerSyntaxAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim obj as New AttributeUsageAttribute With
{
    .AllowMultiple = True,
    .Inherited = False
}
obj = Nothing",
            @"var obj = new AttributeUsageAttribute()
{
    AllowMultiple = true,
    Inherited = false
};
obj = null;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task AnonymousObjectCreationExpressionSyntaxAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim obj = New With
{
    .Name = ""Hello"",
    .Value = ""World""
}
obj = Nothing",
            @"var obj = new
{
    Name = ""Hello"",
    Value = ""World""
};
obj = null;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task SingleAssigmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim x = 3",
            @"int x = 3;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task SingleFieldDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SingleEmptyClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SingleAbstractMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SingleEmptyNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SingleUnusedUsingAliasTidiedAwayAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task QuerySyntaxAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}