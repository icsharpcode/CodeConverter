using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class OnErrorStatementTests : ConverterTestBase
{
    [Fact]
    public async Task BasicGotoAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RemainingScopeIssueCharacterizationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}