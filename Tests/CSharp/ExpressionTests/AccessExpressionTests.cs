using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

/// <summary>
/// Member/Element access
/// </summary>
public class AccessExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task MyClassExprAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DictionaryIndexingIssue769Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DictionaryIndexingIssue362Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MethodCallDictionaryAccessConditionalAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task IndexerWithParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MethodCallArrayIndexerBracketsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ElementAtOrDefaultIndexingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task DataTableIndexingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ElementAtOrDefaultInvocationIsNotDuplicatedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task EmptyArgumentListsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task UsesSquareBracketsForIndexerButParenthesesForMethodInvocationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ConditionalExpressionWithOmittedArgsListAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MemberAccessAndInvocationExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OmittedParamsArrayAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ThisMemberAccessExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task BaseMemberAccessExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task UnqualifiedBaseMemberAccessExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PartiallyQualifiedNameAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TypePromotedModuleIsQualifiedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MemberAccessCasingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task XmlMemberAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExclamationPointOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExclamationPointOperator765Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task AliasedImportsWithTypePromotionIssue401Async()
    {
        for (int i = 0; i < 3; i++) {
            try {
                await FlakeyAliasedImportsWithTypePromotionIssue401Async();
                return;
            } catch (Exception) {
                // I believe there are two valid simplifications and the simplifier is non-deterministic
                // Just retry a few times and see if we get the one we expect before failing
                // At the same time as this loop I added "aliasedAgain" in the hope that it'd discourage the simplifier from fully qualifying Strings
            }
        }

        await FlakeyAliasedImportsWithTypePromotionIssue401Async();
    }

    private async Task FlakeyAliasedImportsWithTypePromotionIssue401Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestGenericMethodGroupGainsBracketsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task UsesSquareBracketsForItemIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}