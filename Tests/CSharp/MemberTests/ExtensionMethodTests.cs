using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class ExtensionMethodTests : ConverterTestBase
{
    [Fact]
    public async Task TestExtensionMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExtensionMethodWithExistingImportAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestRefExtensionMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExtensionWithinExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExtensionWithinTypeDerivedFromExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExtensionWithinNestedExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExtensionWithMeWithinExtendedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}