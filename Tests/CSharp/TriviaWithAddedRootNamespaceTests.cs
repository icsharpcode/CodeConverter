using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class TriviaWithAddedRootNamespaceTests : ConverterTestBase
{
    public TriviaWithAddedRootNamespaceTests() : base("ANamespace")
    {

    }

    [Fact]
    public async Task CommentAtStartOfFile663Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}