using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class IndexerTests : ConverterTestBase
{
    [Fact]
    public async Task InterfaceImplementationOfIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InterfaceImplementationOfIndexerAsAbstractAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedImplementationOfIndexerWithAbstractAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ReadOnlyImplementationOfIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WriteOnlyImplementationOfIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}