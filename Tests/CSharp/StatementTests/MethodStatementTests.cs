using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class MethodStatementTests : ConverterTestBase
{
    [Fact]
    public async Task EmptyStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task AssignmentStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task EnumAssignmentStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task AssignmentStatementInDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task AssignmentStatementInVarDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    /// <summary>
    /// Implicitly typed lambdas exist in vb but are not happening in C#. See discussion on https://github.com/dotnet/roslyn/issues/14
    /// * For VB local declarations, inference happens. The closest equivalent in C# is a local function since Func/Action would be overly restrictive for some cases
    /// * For VB field declarations, inference doesn't happen, it just uses "Object", but in C# lambdas can't be assigned to object so we have to settle for Func/Action for externally visible methods to maintain assignability.
    /// </summary>
    [Fact]
    public async Task AssignmentStatementWithFuncAsync()
    {
        // BUG: pubWrite's body is missing a return statement
        // pubWrite is an example of when the LambdaConverter could analyze ConvertedType at usages, realize the return type is never used, and convert it to an Action.
        await TestConversionVisualBasicToCSharpAsync();
    }

    /// <summary>
    /// Technically it's possible to use a type-inferred lambda within a for loop
    /// Other than the above field/local declarations, candidates would be other things using <see cref="SplitVariableDeclarations"/>,
    /// e.g. ForEach (no assignment involved), Using block (can't have a disposable lambda)
    /// </summary>
    [Fact]
    public async Task ContrivedFuncInferenceExampleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ObjectInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TupleInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ObjectInitializationStatementInDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ObjectInitializationStatementInVarDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task EndStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task StopStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlockAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlockStruct634Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlock2Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlockValueAsync()
    {
        //Whitespace trivia bug on first statement in with block
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlockMeClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlockMeStructAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WithBlockForEachAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NestedWithBlockAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DeclarationStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    [Fact]
    public async Task DeclarationStatementTwoVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DeclareStatementLongAsync()
    {
        // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DeclareStatementVoidAsync()
    {
        // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DeclareStatementWithAttributesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task IfStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task IfStatementWithMultiStatementLineAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NestedBlockStatementsKeepSameNestingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SyncLockStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ThrowStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CallStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
        //BUG: Requires new Action wrapper
    }

    [Fact]
    public async Task AddRemoveHandlerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SelectCase1Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SelectCaseWithExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SelectCaseWithStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
        //BUG: Correct textual output, but requires var pattern syntax construct not available before CodeAnalysis 3
    }

    [Fact]
    public async Task SelectCaseWithExpression2Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SelectCaseWithNonDeterministicExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue579SelectCaseWithCaseInsensitiveTextCompareAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue707SelectCaseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TryCatchAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SwitchIntToEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact] //https://github.com/icsharpcode/CodeConverter/issues/585
    public async Task Issue585_SwitchNonStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task ExitMethodBlockStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task YieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SetterReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}