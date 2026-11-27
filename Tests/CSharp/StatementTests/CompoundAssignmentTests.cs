using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
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
        var receiver = _objects;
        var index1 = Idx();
        receiver[index1] = Operators.AddObject(receiver[index1], o);
        var receiver1 = _ints;
        var index2 = Idx();
        receiver1[index2] = (int)Math.Round(receiver1[index2] + 2.5d);
        var receiver2 = _points;
        var index3 = Idx();
        receiver2[index3].X = Conversions.ToInteger(Operators.AddObject(receiver2[index3].X, o));
        var receiver3 = GetH();
        receiver3.F = Conversions.ToInteger(Operators.AddObject(receiver3.F, o));
        var receiver4 = GetH();
        receiver4.P = (int)Math.Round(receiver4.P + 2.5d);
        var receiver5 = GetH();
        var index4 = Idx();
        receiver5.set_Q(index4, (byte)(receiver5.get_Q(index4) + 1));
        _ints[i] = Conversions.ToInteger(Operators.AddObject(_ints[i], o));
        h.F = Conversions.ToInteger(Operators.AddObject(h.F, o));
        _objects[i + 1] = Operators.AddObject(_objects[i + 1], o);
        _ints[Idx()] += 1;
    }
}");
    }

    [Fact]
    public async Task SetterValueIsNamedWhereOptionalParametersAreLeftOutOrArgumentsNamedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class H
    Public Property Q(i As Integer, Optional j As Integer = 0) As Integer
        Get
            Return 0
        End Get
        Set(newValue As Integer)
        End Set
    End Property

    Sub M()
        Q(1) += 1
        Q(1, 2) += 1
        Q(j:=1, i:=0) += 1
    End Sub
End Class", @"
internal partial class H
{
    public int get_Q(int i, int j = 0)
    {
        return 0;
    }
    public void set_Q(int i, int j = 0, int value = default)
    {
    }

    public void M()
    {
        set_Q(1, value: get_Q(1) + 1);
        set_Q(1, 2, get_Q(1, 2) + 1);
        set_Q(j: 1, i: 0, value: get_Q(j: 1, i: 0) + 1);
    }
}");
    }

    [Fact]
    public async Task LateBoundMemberReceiverIsEvaluatedOnceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Function GetO() As Object
        Return Nothing
    End Function

    Sub M(o As Object)
        GetO().F += 1
        o.F += 1
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class C
{
    public object GetO()
    {
        return null;
    }

    public void M(object o)
    {
        var receiver = GetO();
        ((dynamic)receiver).F = Operators.AddObject(((dynamic)receiver).F, 1);
        ((dynamic)o).F = Operators.AddObject(((dynamic)o).F, 1);
    }
}");
    }

    [Fact]
    public async Task ExponentiationIsConvertedBackToTheLeftSideTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Private _i As Integer
    Sub M(b As Byte, d As Double)
        _i ^= 2
        b ^= 2
        d ^= 2
    End Sub
End Class", @"using System;

internal partial class C
{
    private int _i;
    public void M(byte b, double d)
    {
        _i = (int)Math.Round(Math.Pow(_i, 2d));
        b = (byte)Math.Round(Math.Pow(b, 2d));
        d = Math.Pow(d, 2d);
    }
}");
    }

    [Fact]
    public async Task SetterValueOfAReferencedPropertyIsNamedByItsParameterAsync()
    {
        var library = VisualBasicCompilation.Create("ReferencedLibrary",
            new[] { VisualBasicSyntaxTree.ParseText(@"Public Class Library
    Public Property Q(i As Integer, Optional j As Integer = 0) As Integer
        Get
            Return 0
        End Get
        Set(NewValue As Integer)
        End Set
    End Property
End Class") },
            DefaultReferences.With(), new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var image = new MemoryStream();
        var emitted = library.Emit(image);
        Assert.True(emitted.Success, string.Join(", ", emitted.Diagnostics));
        var references = DefaultReferences.With().Append(MetadataReference.CreateFromImage(image.ToArray())).ToArray();

        await AssertConvertedCodeResultEqualsAsync<VBToCSConversion>(@"Class C
    Sub M(l As Library)
        l.Q(1) += 1
        l.Q(2) = 5
        F(l.Q(3))
    End Sub
    Sub F(ByRef x As Integer)
    End Sub
End Class", @"
internal partial class C
{
    public void M(Library l)
    {
        l.set_Q(1, NewValue: l.get_Q(1) + 1);
        l.set_Q(2, NewValue: 5);
        int argx = l.get_Q(3);
        F(ref argx);
        l.set_Q(3, NewValue: argx);
    }
    public void F(ref int x)
    {
    }
}", new TextConversionOptions(references) { RootNamespaceOverride = string.Empty, ShowCompilationErrors = true });
    }

    [Fact]
    public async Task SplitCompoundRightSideIsParenthesizedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Property Q(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Sub M(i As Integer, c As Boolean, b As Byte)
        Q(i) \= If(c, 1, 2)
        b += If(c, 1, 2)
    End Sub
End Class", @"
internal partial class C
{
    public int get_Q(int i)
    {
        return 0;
    }
    public void set_Q(int i, int value)
    {
    }
    public void M(int i, bool c, byte b)
    {
        set_Q(i, get_Q(i) / (c ? 1 : 2));
        b = (byte)(b + (c ? 1 : 2));
    }
}");
    }

    [Fact]
    public async Task LeftOperandIsConvertedToTheOperatorsTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Sub M(m As Decimal, n As Integer?, s As String, b As Boolean, sg As Single, d As Double)
        m ^= 2
        n ^= 2
        s ^= 2
        b ^= 2
        sg \= 2
        d \= 2
        m \= 2
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class C
{
    public void M(decimal m, int? n, string s, bool b, float sg, double d)
    {
        m = (decimal)Math.Pow((double)m, 2d);
        n = n.HasValue ? (int)Math.Round(Math.Pow(n.Value, 2d)) : null;
        s = Math.Pow(Conversions.ToDouble(s), 2d).ToString();
        b = Conversions.ToBoolean(Math.Pow(Conversions.ToDouble(b), 2d));
        sg = (long)Math.Round(sg) / 2L;
        d = (long)Math.Round(d) / 2L;
        m = (long)Math.Round(m) / 2L;
    }
}");
    }
    [Fact]
    public async Task EnumOperandIsConvertedForOperatorsCSharpLacksOnEnumsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum E As Short
    A = 1
End Enum
Class C
    Sub M(x As E, n As E?, i As Integer)
        x += 1
        x -= i
        x *= 3
        x \= 2
        x += E.A
        n *= 3
    End Sub
End Class", @"
internal enum E : short
{
    A = 1
}

internal partial class C
{
    public void M(E x, E? n, int i)
    {
        x = x + 1;
        x = (E)((int)x - i);
        x = (E)((int)x * 3);
        x = (E)((int)x / 2);
        x = (E)((short)x + (short)E.A);
        n = (E?)((int?)n * 3);
    }
}");
    }
    [Fact]
    public async Task EnumPlusOrMinusConstantThatDoesntFitIsConvertedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum B As Byte
    A = 1
End Enum
Enum S As SByte
    A = 1
End Enum
Enum I As Integer
    A = 1
End Enum
Enum U As ULong
    A = 1
End Enum
Class C
    Sub M(b As B, s As S, i As I, u As U)
        b += 1
        b += 1000
        b += -1
        s -= 200
        i += 1L
        i += 1UI
        u += 1
    End Sub
End Class", @"using System;

internal enum B : byte
{
    A = 1
}

internal enum S : sbyte
{
    A = 1
}

internal enum I : int
{
    A = 1
}

internal enum U : ulong
{
    A = 1UL
}

internal partial class C
{
    public void M(B b, S s, I i, U u)
    {
        b = b + 1;
        b = (B)((int)b + 1000);
        b = (B)((int)b + -1);
        s = (S)((int)s - 200);
        i = (I)((long)i + 1L);
        i = (I)((long)i + 1L);
        u = (U)Math.Round((decimal)u + 1m);
    }
}");
    }
    [Fact]
    public async Task ConcatenationOfNullableIsAsForTheBinaryOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Sub M(s As String, o As Object, n As Integer?, d As Double?, b As Boolean?)
        s &= n
        s &= d
        s &= b
        o &= n
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class C
{
    public void M(string s, object o, int? n, double? d, bool? b)
    {
        s += n;
        s += d;
        s += b;
        o = Operators.ConcatenateObject(o, n);
    }
}");
    }
    [Fact]
    public async Task ParenthesizedNothingIsTypedAsNothingIsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Sub M(x As Integer, d As Double, t As Date)
        x /= (Nothing)
        x /= Nothing
        d += (Nothing)
        t += Nothing
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class C
{
    public void M(int x, double d, DateTime t)
    {
        x = (int)Math.Round(x / default(double));
        x = (int)Math.Round(x / (double)default);
        d += default(double);
        t = Conversions.ToDate(Conversions.ToString(t) + null);
    }
}");
    }
}
