using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class BinaryExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task OmitsConversionForEnumBinaryExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task BinaryOperatorsIsIsNotLeftShiftRightShiftAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LikeOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ShiftAssignmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task IntegerArithmeticAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NullableDoubleArithmeticAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ImplicitConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task FloatingPointDivisionIsForcedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ConditionalExpressionInBinaryExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
        public async Task NotOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task AndOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task OrOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task RelationalOperatorsOnNullableTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task SimplifiesAlreadyCheckedNullableComparison_HasValueAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
    }

        [Fact]
        public async Task SimplifiesAlreadyCheckedNullableComparison_NotNothingAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task DoesNotSimplifyComparisonWhenNullChecksAreNotDefinitelyTrueAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task HalfSimplifiesComparisonWhenOneSideAlreadyNullCheckedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
    }

        [Fact]
        public async Task DoesNotSimplifyComparisonWhenNullableChecksAreUncertainAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task SimplifiesNullableEnumIfEqualityCheckAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task SimplifiesNullableDateIfEqualityCheckAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task RelationalOperatorsOnNullableTypeInConditionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task NullableBooleansComparedIssue982Async()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task NullableBooleanComparedToNormalBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync();
        }

        [Fact]
        public async Task ImplicitBooleanConversion712Async()
        {
            await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ImplicitIfStatementBooleanConversion712Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ConversionInComparisonOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RewrittenObjectOperatorDoesNotStackOverflowAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact(Skip = "Too slow")]
    public async Task DeeplyNestedBinaryExpressionShouldNotStackOverflowAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}