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

    [Fact]
    public async Task SplitCompoundAssignmentEvaluatesLeftSideOperandsOnceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Private _objects As Object()
    Private _ints As Integer()
    Private _points As Point()

    Structure Point
        Public X As Integer
    End Structure

    Class Holder
        Public F As Integer
        Property P As Integer
        Property Q(i As Integer) As Byte
            Get
                Return 0
            End Get
            Set(value As Byte)
            End Set
        End Property
    End Class

    Function Idx() As Integer
        Return 1
    End Function

    Function GetH() As Holder
        Return New Holder()
    End Function

    Sub M(o As Object, i As Integer, h As Holder)
        _objects(Idx()) += o
        _ints(Idx()) += 2.5
        _points(Idx()).X += o
        GetH().F += o
        GetH().P += 2.5
        GetH().Q(Idx()) += 1
        _ints(i) += o
        h.F += o
        _objects(i + 1) += o
        _ints(Idx()) += 1
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class C
{
    private object[] _objects;
    private int[] _ints;
    private Point[] _points;

    public partial struct Point
    {
        public int X;
    }

    public partial class Holder
    {
        public int F;
        public int P { get; set; }
        public byte get_Q(int i)
        {
            return 0;
        }
        public void set_Q(int i, byte value)
        {
        }
    }

    public int Idx()
    {
        return 1;
    }

    public Holder GetH()
    {
        return new Holder();
    }

    public void M(object o, int i, Holder h)
    {
        var index1 = Idx();
        _objects[index1] = Operators.AddObject(_objects[index1], o);
        var index2 = Idx();
        _ints[index2] = (int)Math.Round(_ints[index2] + 2.5d);
        ref var receiver = ref _points[Idx()];
        receiver.X = Conversions.ToInteger(Operators.AddObject(receiver.X, o));
        var receiver1 = GetH();
        receiver1.F = Conversions.ToInteger(Operators.AddObject(receiver1.F, o));
        var receiver2 = GetH();
        receiver2.P = (int)Math.Round(receiver2.P + 2.5d);
        var receiver3 = GetH();
        var index3 = Idx();
        receiver3.set_Q(index3, (byte)(receiver3.get_Q(index3) + 1));
        _ints[i] = Conversions.ToInteger(Operators.AddObject(_ints[i], o));
        h.F = Conversions.ToInteger(Operators.AddObject(h.F, o));
        _objects[i + 1] = Operators.AddObject(_objects[i + 1], o);
        _ints[Idx()] += 1;
    }
}");
    }
}
