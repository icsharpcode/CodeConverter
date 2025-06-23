using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class ConstructorTests : ConverterTestBase
{
    [Fact]
    public async Task TestConstructorVisibilityAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestModuleConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestHoistedOutParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestHoistedOutParameterLambdaUsingByRefParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestConstructorWithImplicitPublicAccessibilityAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestStaticConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestConstructorStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}