using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ParameterTests : ConverterTestBase
{
    [Fact]
    public async Task OptionalParameter_DoesNotThrowInvalidCastExceptionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OptionalLastParameter_ExpandsOptionalOmittedArgAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OptionalFirstParameter_ExpandsOptionalOmittedArgAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OmittedArgumentAfterNamedArgument_WhenMethodHasCollidingOverload_ShouldExpandAllOptionalArgsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}