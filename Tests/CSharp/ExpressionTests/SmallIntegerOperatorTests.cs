using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

/// <summary>
/// VB keeps Byte, SByte, Short and UShort for the result of its operators, C# promotes them to int.
/// </summary>
public class SmallIntegerOperatorTests : ConverterTestBase
{
    [Fact]
    public async Task NotOnNumericOperandIsBitwiseComplementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(i As Integer, sb As SByte, nsb As SByte?, ni As Integer?)
        Dim a As Integer = Not i
        Dim b As Integer? = Not ni
        Dim c As Integer? = Not nsb
        Dim d As SByte = Not sb
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int i, sbyte sb, sbyte? nsb, int? ni)
    {
        int a = ~i;
        var b = ~ni;
        int? c = ~nsb;
        sbyte d = (sbyte)~sb;
    }
}");
    }
}
