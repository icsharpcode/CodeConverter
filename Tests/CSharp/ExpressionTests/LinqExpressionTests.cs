using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LinqExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task Issue895_LinqWhereAfterGroupAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Characterize_Issue948_GroupByMember_Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
        // BUG: Order by should be on @group.Key
    }

    [Fact]
    public async Task Issue736_LinqEarlySelectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue635_LinqDistinctOrderByAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Linq1Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Linq2Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Linq3Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Linq4Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Linq5Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqAsEnumerableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqMultipleFromsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqNoFromsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqPartitionDistinctAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact(Skip = "Issue #29 - Aggregate not supported")]
    public async Task LinqAggregateSumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact(Skip = "Issue #29 - Group join not supported")]
    public async Task LinqGroupJoinAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqJoinReorderExpressionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqMultipleJoinConditionsReorderExpressionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqMultipleIdentifierOnlyJoinConditionsReorderExpressionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqGroupByTwoThingsAnonymouslyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
        // Current characterization is slightly wrong, I think it still needs this on the end "into g select new { Length = g.Key.Length, Count = g.Key.Count, Group = g.AsEnumerable() }"
    }

    [Fact]
    public async Task LinqSelectVariableDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LinqGroupByAnonymousAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task LinqCommasToFromAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task Issue1011_LinqExpressionWithNullableCharacterizationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task AnExpressionTreeMayNotContainIsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}