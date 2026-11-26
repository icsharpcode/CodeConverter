using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class CompoundAssignmentTests : ConverterTestBase
{
    [Fact]
    public async Task LateBoundCompoundAssignmentCallsTheObjectOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum E
    A
End Enum

Class TestClass
    Property Q(i As Integer) As Byte
        Get
            Return 0
        End Get
        Set(value As Byte)
        End Set
    End Property
    Private Sub TestMethod(b As Byte, ob As Object, o As Object, i As Integer, s As String, e As E, n As Integer?, dt As Date)
        b <<= ob
        i >>= ob
        o <<= 2
        o += 1
        b += ob
        o -= ob
        o *= 2
        o /= 2
        b \= ob
        o ^= 2
        i ^= ob
        o &= ""x""
        s &= o
        e &= ob
        n &= ob
        dt &= ob
        Q(0) += ob
        Q(1) <<= ob
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum E
{
    A
}

internal partial class TestClass
{
    public byte get_Q(int i)
    {
        return 0;
    }
    public void set_Q(int i, byte value)
    {
    }
    private void TestMethod(byte b, object ob, object o, int i, string s, E e, int? n, DateTime dt)
    {
        b = Conversions.ToByte(Operators.LeftShiftObject(b, ob));
        i = Conversions.ToInteger(Operators.RightShiftObject(i, ob));
        o = Operators.LeftShiftObject(o, 2);
        o = Operators.AddObject(o, 1);
        b = Conversions.ToByte(Operators.AddObject(b, ob));
        o = Operators.SubtractObject(o, ob);
        o = Operators.MultiplyObject(o, 2);
        o = Operators.DivideObject(o, 2);
        b = Conversions.ToByte(Operators.IntDivideObject(b, ob));
        o = Operators.ExponentObject(o, 2);
        i = Conversions.ToInteger(Operators.ExponentObject(i, ob));
        o = Operators.ConcatenateObject(o, ""x"");
        s = Conversions.ToString(Operators.ConcatenateObject(s, o));
        e = (E)Conversions.ToInteger(Operators.ConcatenateObject(e, ob));
        n = (int?)Operators.ConcatenateObject(n, ob);
        dt = Conversions.ToDate(Operators.ConcatenateObject(dt, ob));
        set_Q(0, Conversions.ToByte(Operators.AddObject(get_Q(0), ob)));
        set_Q(1, Conversions.ToByte(Operators.LeftShiftObject(get_Q(1), ob)));
    }
}");
    }
}
