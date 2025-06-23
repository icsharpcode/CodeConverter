using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// For generic loop related tests. Also see ExitableMethodExecutableStatementTests for tests of Exit Do, Exit For, etc.
/// </summary>
public class LoopStatementTests : ConverterTestBase
{

    [Fact]
    public async Task UntilStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TwoForEachStatementsWithImplicitVariableCreationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Int16ForLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExternallyDeclaredLoopVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForNonNegativeStepAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForNegativeStepAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForVariableStepAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForeachWithObjectCollectionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task ForWithSingleStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForNextMutatingFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForRequiringExtraVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForWithBlockAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NullInitValueForHoistedVariableIssue913Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LabeledAndForStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LoopWithVariableDeclarationInitializedWithDefault_ShouldNotBePulledOutOfTheLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LoopWithMultipleVariableDeclarationsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LoopWithVariableDeclarationInitializedWithAsNewClause_ShouldNotBePulledOutOfTheLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    
    [Fact]
    public async Task ForWithVariableDeclarationIssue897Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NestedLoopsWithVariableDeclarationIssue897Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForWithVariableDeclarationIssue1000Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForWithVariableDeclarationIssue998Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}