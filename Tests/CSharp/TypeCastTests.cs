using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class TypeCastTests : ConverterTestBase
{
    [Fact]
    public async Task NumericStringToEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CIntObjectToIntegerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CDateAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastObjectToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ImplicitCastObjectToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastArrayListAssignmentToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ImplicitCastObjecStringToStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitOperatorInvocation_Issue678Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CTypeFractionalAndBooleanToIntegralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CTypeFractionalAndBooleanToNullableIntegralsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    
    [Fact]
    public async Task CastObjectToGenericListAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastingToEnumRightSideShouldBeForcedToBeIntegralAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CTypeObjectToIntegerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastingStringToEnumShouldUseConversionsToIntegerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastingIntegralTypeToEnumShouldUseExplicitCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastingIntegralTypeToNullableEnumShouldUseExplicitCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TryCastObjectToGenericListAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RetainNullableBoolWhenNeededAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestNullableBoolConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestNullableEnumConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestNumbersNullableConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastConstantNumberToLongAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastConstantNumberToFloatAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastConstantNumberToDecimalAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastConstantNumberToCharacterWAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task CastConstantNumberToCharacterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    
    [Fact]
    public async Task CastObjectToNullableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task TestSingleCharacterStringLiteralBecomesCharWhenNeededAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task TestSelectCaseComparesCharsAndStringsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestSingleCharacterStringLiteralBecomesChar_WhenExplictCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestCastHasBracketsWhenElementAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultipleNestedCastsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    /// <summary>
    /// We just use ConditionalCompareObjectEqual to make it a bool, but VB emits a late binding call something like this:
    /// array[0] = Operators.CompareObjectEqual(left, right, false);
    /// array[1] = "Identical values stored in objects should be equal";
    /// NewLateBinding.LateCall(this, null, "AssertTrue", array, null, null, null, true);
    /// This will likely be the same in the vast majority of cases
    /// </summary>
    [Fact]
    public async Task ObjectComparisonIsConvertedToBoolRatherThanLateBoundAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestGenericCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestInferringImplicitGenericTypesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


        [Fact]
    public async Task TestCTypeStringToEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue894_LinqQueryWhereClauseIsAlwaysBooleanAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}