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
namespace ANamespace
{
    /// <summary> Form for viewing the my. </summary>
/// <remarks> David, 10/1/2020. </remarks>
    public partial class MyForm : isr.Automata.Finite.Forms.BimanualToggleForm
    {
    }
}
1 source compilation errors:
BC30002: Type 'isr.Automata.Finite.Forms.BimanualToggleForm' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'isr' could not be found (are you missing a using directive or an assembly reference?)
");
        }
    }
}
