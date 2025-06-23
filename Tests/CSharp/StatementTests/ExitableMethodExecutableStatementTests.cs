using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// Covers:
/// Exit { Do | For | Select | Try | While } https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/exit-statement
/// Continue { Do | For | While } https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/continue-statement
///
/// Does not cover:
/// Exit { Function | Property | Sub } since they are not MethodExecutableStatements
/// </summary>
public class ExitableMethodExecutableStatementTests : ConverterTestBase
{
    [Fact]
    public async Task WhileStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SimpleDoStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DoWhileStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithExplicitTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithVarAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithUsedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithFieldVarUsedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithUnusedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithFieldVarUnusedOuterDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForEachStatementWithUnusedNestedDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SelectCaseWithExplicitExitAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlock_Issue690Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultipleBreakable_CreatesIfStatementsToExitContainingBlockIssue946Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task BreakableThenContinuable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultipleContinuable_CreatesIfStatementsToExitContainingBlock_WithoutRunningInterveningCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExitTry_CreatesBreakableLoop_Issue779Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithinNonExitedTryAndFor_ExitForGeneratesBreakAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithinForAndNonExitedTry_ExitForGeneratesBreakAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithinForAndExitedTry_ExitForGeneratesIfStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}