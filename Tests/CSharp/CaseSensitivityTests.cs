using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class CaseSensitivityTests : ConverterTestBase
{
    [Fact]
    public async Task HandlesWithDifferingCaseTestAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue1154_NamespaceAndClassSameNameDifferentCaseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }



}