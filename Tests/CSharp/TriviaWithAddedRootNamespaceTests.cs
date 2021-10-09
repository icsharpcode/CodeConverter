using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    public class TriviaWithAddedRootNamespaceTests : ConverterTestBase
    {
        public TriviaWithAddedRootNamespaceTests() : base("ANamespace")
        {

        }

        [Fact]
        public async Task CommentAtStartOfFile663Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"''' <summary> Form for viewing the my. </summary>
''' <remarks> David, 10/1/2020. </remarks>
Public Class MyForm
    Inherits isr.Automata.Finite.Forms.BimanualToggleForm
End Class",
@"
public partial class AClass
{
    /* TODO ERROR:sdAMethod()
    {
    }
    /* TODO ERROR: Skipped EndIfDirectiveTrivia
#End If
*/
}
");
        }
    }
}
