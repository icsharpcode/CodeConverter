using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MissingSemanticModelInfo;

public class ExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task InvokeIndexerOnPropertyValueAsync()
    {
        // Chances of having an unknown delegate stored as a field/property/local seem lower than having an unknown non-delegate
        // type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
        await TestConversionVisualBasicToCSharpAsync();
    }
    [Fact]
    public async Task InvokeMethodOnPropertyValueAsync()
    {
        // Chances of having an unknown delegate stored as a field/property/local seem lower than having an unknown non-delegate
        // type with an indexer stored, so for a standalone identifier err on the side of assuming it's an indexer
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InvokeMethodWithUnknownReturnTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ForNextMutatingMissingFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OutParameterNonCompilingTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    [Fact]
    public async Task EnumSwitchAndValWithUnusedMissingTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastToSameTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync() ;
    }

    [Fact]
    public async Task UnknownTypeInvocationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CharacterizeRaiseEventWithMissingDefinitionActsLikeMultiIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ConvertBuiltInMethodWithUnknownArgumentTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CallShouldAlwaysBecomeInvocationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Call mySuperFunction(strSomething, , optionalSomething)",
            @"mySuperFunction(strSomething, default, optionalSomething);",
            expectSurroundingBlock: true, missingSemanticInfo: true
        );
    }

}