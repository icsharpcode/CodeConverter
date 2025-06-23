using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class PropertyMemberTests : ConverterTestBase
{


    [Fact]
    public async Task TestPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestParameterizedPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestParameterizedPropertyRequiringConversionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact] //https://github.com/icsharpcode/CodeConverter/issues/642
    public async Task TestOptionalParameterizedPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestParameterizedPropertyAndGenericInvocationAndEnumEdgeCasesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestParameterizedPropertyWithTriviaAsync()
    {
        //issue 1095
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PropertyWithMissingTypeDeclarationAsync()//TODO Check object is the inferred type
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestReadWriteOnlyInterfacePropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SynthesizedBackingFieldAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PropertyInitializersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestWriteOnlyPropertiesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestImplicitPrivateSetterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestParametrizedPropertyCalledWithNamedArgumentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestParametrizedPropertyCalledWithOmittedArgumentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestSetWithNamedParameterPropertiesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestPropertyAssignmentReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestGetIteratorDoesNotGainReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}