using System.Diagnostics;
using System.Linq;
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

    [Fact]
    public async Task ResultWidenedToIntegerOrStringIsCastUnlessAlreadyInRangeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TakeInteger(i As Integer)
    End Sub
    Private Sub TestMethod(b1 As Byte, b2 As Byte, us As UShort, sb As SByte)
        Dim i1 As Integer = Not b1
        TakeInteger(Not b1)
        Dim s1 = ""x"" & Not b1
        Dim l1 As Long = b1 << 3
        Dim i2 As Integer = Not us
        Dim i3 As Integer = b1 + b2
        Dim i4 As Integer = b1 And b2
        TakeInteger(b1 Or b2)
        Dim s2 = ""x"" & (b1 Xor b2)
        Dim i5 As Integer = Not sb
        Dim i6 As Integer = b1 \ b2
        Dim i7 As Integer = -b1
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TakeInteger(int i)
    {
    }
    private void TestMethod(byte b1, byte b2, ushort us, sbyte sb)
    {
        int i1 = (byte)~b1;
        TakeInteger((byte)~b1);
        string s1 = ""x"" + (byte)~b1;
        long l1 = (byte)(b1 << 3);
        int i2 = (ushort)~us;
        int i3 = (byte)(b1 + b2);
        int i4 = b1 & b2;
        TakeInteger(b1 | b2);
        string s2 = ""x"" + (b1 ^ b2);
        int i5 = ~sb;
        int i6 = b1 / b2;
        int i7 = -b1;
    }
}");
    }

    [Fact]
    public async Task ResultConvertedToObjectIsBoxedAsVbTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TakeObject(o As Object)
    End Sub
    Private Sub TestMethod(b1 As Byte, b2 As Byte)
        Dim o1 As Object = b1 Or b2
        TakeObject(b1 And b2)
        Dim o2 As Object = b1 + b2
        Dim t = (b1 Xor b2).GetType()
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TakeObject(object o)
    {
    }
    private void TestMethod(byte b1, byte b2)
    {
        object o1 = (byte)(b1 | b2);
        TakeObject((byte)(b1 & b2));
        object o2 = (byte)(b1 + b2);
        var t = ((byte)(b1 ^ b2)).GetType();
    }
}");
    }

    [Fact]
    public async Task LambdaReturningResultIsCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte)
        Dim f1 = Function() b1 And b2
        Dim f2 = Function() b1 + b2
        Dim f3 = Function()
                     Return Not b1
                 End Function
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2)
    {
        byte f1() => (byte)(b1 & b2);
        byte f2() => (byte)(b1 + b2);
        byte f3() => (byte)~b1;
    }
}");
    }

    [Fact]
    public async Task OnlyOutermostOfChainedOperatorsIsCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, s1 As Short)
        Dim a = b1 And b2 And b2
        Dim b = (b1 Or b2) Xor b2
        Dim c = (Not b1) And b2
        Dim d = -(s1 + s1) * s1
        Dim e = (b1 And b2) = b1
        Dim f = (b1 And b2) <> 0
        Dim g = (Not b1) > b2
        Dim h = (b1 + b2) \ b2
        Dim i = (b1 And b2) >> 1
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, short s1)
    {
        byte a = (byte)(b1 & b2 & b2);
        byte b = (byte)((b1 | b2) ^ b2);
        byte c = (byte)(~b1 & b2);
        short d = (short)(-(s1 + s1) * s1);
        bool e = (b1 & b2) == b1;
        bool f = (b1 & b2) != 0;
        bool g = (byte)~b1 > b2;
        byte h = (byte)((byte)(b1 + b2) / b2);
        byte i = (byte)((b1 & b2) >> 1);
    }
}");
    }

    [Fact]
    public async Task OperatorsOnOtherTypesAreNotCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.IO

Class TestClass
    Private Sub TestMethod(x As Boolean, y As Boolean, i As Integer, j As Integer, l As Long, f As FileAttributes)
        Dim a = x And y Or Not x Xor y
        Dim b = Not i And j Or i Xor j << 2
        Dim c = -l Or l >> 1
        Dim d = f And Not FileAttributes.Hidden Or FileAttributes.ReadOnly
    End Sub
End Class", @"using System.IO;

internal partial class TestClass
{
    private void TestMethod(bool x, bool y, int i, int j, long l, FileAttributes f)
    {
        bool a = (x & y | !x) ^ y;
        int b = (~i & j | i) ^ j << 2;
        long c = -l | l >> 1;
        var d = f & ~FileAttributes.Hidden | FileAttributes.ReadOnly;
    }
}");
    }

    [Fact]
    public async Task ConstantResultOutOfCSharpRangeIsCastUncheckedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Const A As Byte = 1
    Const B As Byte = Not A
    Const C As Byte = A Or B
    Private Sub TestMethod()
        Dim o As Object = A And B
    End Sub
End Class", @"
internal partial class TestClass
{
    private const byte A = 1;
    private const byte B = unchecked((byte)~A);
    private const byte C = A | B;
    private void TestMethod()
    {
        object o = (byte)(A & B);
    }
}");
    }

    [Fact]
    public async Task NullableResultIsCastToNullableVbTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, nb As Byte?, ns As Short?)
        Dim a = nb And nb
        Dim b = nb Or b1
        Dim c = -ns
        Dim d = ns << 1
        Dim e = Not nb
        Dim f As Integer? = nb And b1
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, byte? nb, short? ns)
    {
        var a = (byte?)(nb & nb);
        var b = (byte?)(nb | b1);
        var c = (short?)-ns;
        var d = (short?)(ns << 1);
        var e = (byte?)~nb;
        int? f = nb & b1;
    }
}");
    }

    [Fact]
    public async Task CompoundAssignmentsStayCompoundWhereCSharpCastsBackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, s1 As Short, l As Long, i As Integer)
        b1 <<= 1
        s1 >>= 2
        l <<= i
        i >>= 3
        b1 += b2
        b1 = b1 And b2
        b1 -= b2 Or b2
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, short s1, long l, int i)
    {
        b1 <<= 1;
        s1 >>= 2;
        l <<= i;
        i >>= 3;
        b1 += b2;
        b1 = (byte)(b1 & b2);
        b1 -= (byte)(b2 | b2);
    }
}");
    }

    [Fact]
    public async Task ShiftCountIsMaskedToTheBitsOfTheShiftedTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, sb As SByte, s1 As Short, us As UShort, i As Integer, l As Long, nb As Byte?, n As Integer, nn As Integer?)
        Dim a = b1 << n
        Dim b = sb >> n
        Dim c = s1 << n
        Dim d = us >> n
        Dim e = b1 << 9
        Dim f = b1 >> -1
        Dim g = b1 << 7
        Dim h = s1 << 15
        Dim j = i << n
        Dim k = l >> n
        Dim m = nb << n
        Dim o = b1 << nn
        b1 <<= n
        us >>= 17
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, sbyte sb, short s1, ushort us, int i, long l, byte? nb, int n, int? nn)
    {
        byte a = (byte)(b1 << (n & 7));
        sbyte b = (sbyte)(sb >> (n & 7));
        short c = (short)(s1 << (n & 15));
        ushort d = (ushort)(us >> (n & 15));
        byte e = (byte)(b1 << 1);
        byte f = (byte)(b1 >> 7);
        byte g = (byte)(b1 << 7);
        short h = (short)(s1 << 15);
        int j = i << n;
        long k = l >> n;
        var m = (byte?)(nb << (n & 7));
        var o = (byte?)(b1 << (nn & 7));
        b1 <<= n & 7;
        us >>= 1;
    }
}");
    }

    [Fact]
    public async Task OperatorsOnEachSmallTypeAreCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, s1 As Short, s2 As Short, sb As SByte, us As UShort)
        Dim a = Not b1
        Dim b = b1 Xor b2
        Dim c = s1 Or s2
        Dim d = s1 And s2
        Dim e = s1 Xor s2
        Dim f = Not s1
        Dim g = sb And sb
        Dim h = -sb
        Dim i = Not us
        Dim j = us Or us
        Dim k = +b1
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, short s1, short s2, sbyte sb, ushort us)
    {
        byte a = (byte)~b1;
        byte b = (byte)(b1 ^ b2);
        short c = (short)(s1 | s2);
        short d = (short)(s1 & s2);
        short e = (short)(s1 ^ s2);
        short f = (short)~s1;
        sbyte g = (sbyte)(sb & sb);
        sbyte h = (sbyte)-sb;
        ushort i = (ushort)~us;
        ushort j = (ushort)(us | us);
        byte k = (byte)+b1;
    }
}");
    }

    [Fact]
    public async Task UserDefinedOperatorsAreNotPromotedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Structure S
    Public Shared Operator And(x As S, y As S) As Byte
        Return 1
    End Operator
    Public Shared Operator +(x As S, y As Byte) As S
        Return x
    End Operator
End Structure

Class TestClass
    Private Sub TestMethod(x As S, b1 As Byte, b2 As Byte)
        Dim a = x And x
        Dim b = x + (b1 And b2)
    End Sub
End Class", @"
internal partial struct S
{
    public static byte operator &(S x, S y)
    {
        return 1;
    }
    public static S operator +(S x, byte y)
    {
        return x;
    }
}

internal partial class TestClass
{
    private void TestMethod(S x, byte b1, byte b2)
    {
        byte a = x & x;
        var b = x + (byte)(b1 & b2);
    }
}");
    }

    [Fact]
    public async Task LongChainOfInRangeOperatorsConvertsInLinearTimeAsync()
    {
        const int operatorCount = 25;
        string Chain(string op) => "b" + string.Concat(Enumerable.Repeat($" {op} b", operatorCount));
        var stopwatch = Stopwatch.StartNew();
        await TestConversionVisualBasicToCSharpAsync($@"Class TestClass
    Private Sub TestMethod(b As Byte)
        Dim x As Integer = {Chain("Mod")}
        Dim y As Integer = {Chain("\\")}
    End Sub
End Class", $@"
internal partial class TestClass
{{
    private void TestMethod(byte b)
    {{
        int x = {Chain("%")};
        int y = {Chain("/")};
    }}
}}");
        Assert.True(stopwatch.Elapsed.TotalSeconds < 15, $"Took {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task ConstantSubexpressionOutOfCSharpRangeIsCastUncheckedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Const C1 As UShort = 65534, C2 As UShort = 40000
    Const CB As Byte = 200
    Const K1 As Byte = CB << 1
    Private Sub TestMethod(b As Byte, us As UShort)
        Dim x As UShort = (Not C1) * C2 + us
        Dim y As Integer = (Not CB) Mod b
        Dim z As Integer = (Not C1) * C2
        Select Case b
            Case CB << 1
        End Select
    End Sub
End Class", @"
internal partial class TestClass
{
    private const ushort C1 = 65534;
    private const ushort C2 = 40000;
    private const byte CB = 200;
    private const byte K1 = unchecked((byte)(CB << 1));
    private void TestMethod(byte b, ushort us)
    {
        ushort x = (ushort)(unchecked((ushort)~C1) * C2 + us);
        int y = unchecked((byte)~CB) % b;
        int z = unchecked((ushort)~C1) * C2;
        switch (b)
        {
            case unchecked((byte)(CB << 1)):
                break;
        }
    }
}");
    }

    [Fact]
    public async Task ConversionToAnotherNumericTypeCastsOnceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, s1 As Short, s2 As Short, us As UShort, sb As SByte)
        Dim a = CByte(us >> 8)
        Dim b = CByte(s1 And s2)
        Dim c = CInt(b1 And b2)
        Dim d = CInt(us >> 8)
        Dim e = CType(us And us, Integer)
        Dim f = CLng(Not sb)
        Dim g As Byte = s1 And s2
        Dim h As SByte = b1 And b2
        Dim i = CByte(Not us)
        Dim j = CInt(Not b1)
        Dim k As Short = b1 And b2
        Dim m As Integer = b1 Mod b2
        Dim n As Integer = sb \ sb
        Dim o As Integer = +b1
        Dim p = CDbl(Not b1)
        Dim q As Func(Of Short) = Function() b1 And b2
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, short s1, short s2, ushort us, sbyte sb)
    {
        byte a = (byte)(us >> 8);
        byte b = (byte)(s1 & s2);
        int c = b1 & b2;
        int d = us >> 8;
        int e = us & us;
        long f = ~sb;
        byte g = (byte)(s1 & s2);
        sbyte h = (sbyte)(b1 & b2);
        byte i = (byte)~us;
        int j = (byte)~b1;
        short k = (short)(b1 & b2);
        int m = b1 % b2;
        int n = (sbyte)(sb / sb);
        int o = +b1;
        double p = (byte)~b1;
        Func<short> q = () => (short)(b1 & b2);
    }
}");
    }

    [Fact]
    public async Task ShiftCountMaskIsFoldedOrOmittedWhereRedundantAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Const Nine As Byte = 9
    Private Sub TestMethod(b1 As Byte, n As Integer)
        Dim a = b1 << 9L
        Dim b = b1 << Nine
        Dim c = b1 << (n And 3)
        Dim d = b1 << (3 And n)
        Dim e = b1 << (n And 8)
    End Sub
End Class", @"
internal partial class TestClass
{
    private const byte Nine = 9;
    private void TestMethod(byte b1, int n)
    {
        byte a = (byte)(b1 << 1);
        byte b = (byte)(b1 << 1);
        byte c = (byte)(b1 << (n & 3));
        byte d = (byte)(b1 << (3 & n));
        byte e = (byte)(b1 << (n & 8 & 7));
    }
}");
    }

    [Fact]
    public async Task ShiftOfByteEnumIsMaskedAndShiftsTheUnderlyingValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum E As Byte
    A = 1
End Enum

Class TestClass
    Private Sub TestMethod(en As E, n As Integer, s As Short, b1 As Byte)
        Dim q = en << n
        en <<= 9
        en >>= n
        s += s << 1
        b1 += b1 + b1
    End Sub
End Class", @"
internal enum E : byte
{
    A = 1
}

internal partial class TestClass
{
    private void TestMethod(E en, int n, short s, byte b1)
    {
        byte q = (byte)((byte)en << (n & 7));
        en = (E)((byte)en << 1);
        en = (E)((byte)en >> (n & 7));
        s += (short)(s << 1);
        b1 += (byte)(b1 + b1);
    }
}");
    }

    [Fact]
    public async Task InRangeResultInCoalesceOrUnformattedInterpolationIsNotCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, nb1 As Byte?, nb2 As Byte?, sb As SByte)
        Dim a = If(nb1 And nb2, 0)
        Dim b = If(Not nb1, 0)
        Dim c = $""{b1 And b2}""
        Dim d = $""{b1 And b2,5}""
        Dim e = $""{sb And sb:X}""
        Dim f = $""{Not b1}""
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, byte? nb1, byte? nb2, sbyte sb)
    {
        int a = nb1 & nb2 ?? 0;
        int b = (byte?)~nb1 ?? 0;
        string c = $""{b1 & b2}"";
        string d = $""{b1 & b2,5}"";
        string e = $""{(sbyte)(sb & sb):X}"";
        string f = $""{(byte)~b1}"";
    }
}");
    }

    [Fact]
    public async Task CompoundAssignmentToParameterizedPropertyCastsTheSetterArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Property Q(i As Integer) As Byte
        Get
            Return 0
        End Get
        Set(value As Byte)
        End Set
    End Property
    Property R(i As Integer) As Short
        Get
            Return 0
        End Get
        Set(value As Short)
        End Set
    End Property
    Private Sub TestMethod(b As Byte, s As Short, n As Integer)
        Q(1) += b
        Q(1) -= b
        Q(1) \= b
        Q(1) <<= n
        Q(1) >>= 9
        Q(1) = Q(1) And b
        R(1) += s
        R(1) <<= 1
    End Sub
End Class", @"
internal partial class TestClass
{
    public byte get_Q(int i)
    {
        return 0;
    }
    public void set_Q(int i, byte value)
    {
    }
    public short get_R(int i)
    {
        return 0;
    }
    public void set_R(int i, short value)
    {
    }
    private void TestMethod(byte b, short s, int n)
    {
        set_Q(1, (byte)(get_Q(1) + b));
        set_Q(1, (byte)(get_Q(1) - b));
        set_Q(1, (byte)(get_Q(1) / b));
        set_Q(1, (byte)(get_Q(1) << (n & 7)));
        set_Q(1, (byte)(get_Q(1) >> 1));
        set_Q(1, (byte)(get_Q(1) & b));
        set_R(1, (short)(get_R(1) + s));
        set_R(1, (short)(get_R(1) << 1));
    }
}");
    }

    [Fact]
    public async Task ConstantShiftIsUncheckedOnlyWhenItLeavesTheRangeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Const CB1 As Byte = 3
    Const CB2 As Byte = 200
    Const K1 As Byte = CB1 << 1
    Const K2 As Byte = CB2 << 1
    Private Sub TestMethod(b As Byte)
        Dim i1 As Integer = CB1 << 1
        Dim i2 = (CB1 << 2) Or b
        Dim i3 = 1US << 16
        Dim i4 = Not (CB1 << 2)
        Dim i5 = Not (Not 1US)
    End Sub
End Class", @"
internal partial class TestClass
{
    private const byte CB1 = 3;
    private const byte CB2 = 200;
    private const byte K1 = CB1 << 1;
    private const byte K2 = unchecked((byte)(CB2 << 1));
    private void TestMethod(byte b)
    {
        int i1 = CB1 << 1;
        byte i2 = (byte)(CB1 << 2 | b);
        ushort i3 = 1;
        byte i4 = unchecked((byte)~(CB1 << 2));
        ushort i5 = unchecked((ushort)~(ushort)~1);
    }
}");
    }

    [Fact]
    public async Task ConversionToNonNullableOrEnumOfNoWiderTypeCastsOnceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum EB As Byte
    A
End Enum

Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte, nb As Byte?)
        Dim a = CByte(nb + nb)
        Dim b = CType(b1 And b2, EB)
        Dim c = CType(Not b1, EB)
        Dim d = CType(Not b1, DayOfWeek)
        Dim e As Byte? = b1 And b2
    End Sub
End Class", @"using System;

internal enum EB : byte
{
    A
}

internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2, byte? nb)
    {
        byte a = (byte)(nb + nb);
        EB b = (EB)(b1 & b2);
        EB c = (EB)~b1;
        DayOfWeek d = (DayOfWeek)(byte)~b1;
        byte? e = (byte)(b1 & b2);
    }
}");
    }

    [Fact]
    public async Task NotOnFractionalOperandComplementsItAsLongAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(d As Double, nd As Double?, m As Decimal, f As Single)
        Dim a = Not d
        Dim b = Not nd
        Dim c = Not m
        Dim e = Not f
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(double d, double? nd, decimal m, float f)
    {
        long a = ~(long)Math.Round(d);
        var b = ~(nd.HasValue ? (long?)Math.Round(nd.Value) : null);
        long c = ~(long)Math.Round(m);
        long e = ~(long)Math.Round(f);
    }
}");
    }

    [Fact]
    public async Task UncheckedConstantKeepsInnerUncheckedHexLiteralAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Byte = Not CByte((&H80000010 \ 3) And &HFF)
        Dim us As UShort = Not CUShort((&H80000010 >> 20) And &HFFFF)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        byte b = unchecked((byte)~(((byte)(((((unchecked((int)0x80000010)) / 3))) & 0xFF))));
        ushort us = unchecked((ushort)~(((ushort)(((((unchecked((int)0x80000010)) >> 20))) & 0xFFFF))));
    }
}");
    }

    [Fact]
    public async Task ConstantFractionalConvertedToIntegralIsFoldedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Const D As Double = 2.5
    Const L1 As Long = Not D
    Const L2 As Long = D
    Sub M(Optional x As Long = Not D)
    End Sub
    Sub T(b As Long)
        Dim y As Long = Not 2.5
        Dim x As Long = 2.5
        Dim z As Long = 3.5
        Dim i As Integer = D
        Dim sb As SByte = -2.5
        Dim sh As Short = 5.5
        Dim us As UShort = 0.5
        Const local As Long = Not D
        Select Case b
            Case Not D
        End Select
    End Sub
End Class", @"
internal partial class TestClass
{
    private const double D = 2.5d;
    private const long L1 = ~2L;
    private const long L2 = 2L;
    public void M(long x = ~2L)
    {
    }
    public void T(long b)
    {
        long y = ~2L;
        long x = 2L;
        long z = 4L;
        int i = 2;
        sbyte sb = -2;
        short sh = 6;
        ushort us = 0;
        const long local = ~2L;
        switch (b)
        {
            case ~2L:
                break;
        }
    }
}");
    }

    [Fact]
    public async Task CompoundShiftOfNonIntegralLeftSideShiftsItsVbConversionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(bo As Boolean, d As Double, f As Single, m As Decimal, s As String, nd As Double?, n As Integer)
        bo <<= 1
        bo <<= n
        d <<= 2
        f >>= 1
        m <<= 1
        s <<= 2
        nd <<= 2
        Dim x As Short = True << 3
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private void TestMethod(bool bo, double d, float f, decimal m, string s, double? nd, int n)
    {
        bo = Conversions.ToBoolean(Conversions.ToShort(bo) << 1);
        bo = Conversions.ToBoolean(Conversions.ToShort(bo) << (n & 15));
        d = (long)Math.Round(d) << 2;
        f = (long)Math.Round(f) >> 1;
        m = (long)Math.Round(m) << 1;
        s = (Conversions.ToLong(s) << 2).ToString();
        nd = (nd.HasValue ? (long?)Math.Round(nd.Value) : null) << 2;
        short x = (short)(Conversions.ToShort(true) << 3);
    }
}");
    }

    [Fact]
    public async Task InRangeResultConvertedToWiderEnumOrSwitchedOnIsNotCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum ES As Short
    A
End Enum

Class TestClass
    Private Sub TestMethod(b1 As Byte, b2 As Byte)
        Dim a = CType(b1 Or b2, ES)
        Dim b = CType(Not b1, ES)
        Select Case b1 And b2
            Case 1
        End Select
        Select Case Not b1
            Case 1
        End Select
    End Sub
End Class", @"
internal enum ES : short
{
    A
}

internal partial class TestClass
{
    private void TestMethod(byte b1, byte b2)
    {
        ES a = (ES)(b1 | b2);
        ES b = (ES)(byte)~b1;
        switch (b1 & b2)
        {
            case 1:
                break;
        }
        switch ((byte)~b1)
        {
            case 1:
                break;
        }
    }
}");
    }

    [Fact]
    public async Task FractionalConstantInCompoundAssignmentOrArrayBoundIsNotFoldedAloneAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Const D As Double = 1.5
    Const D4 As Double = 4.4
    Property P As Integer
    Property Q(i As Integer) As Byte
        Get
            Return 0
        End Get
        Set(value As Byte)
        End Set
    End Property
    Sub M(i As Integer, b As Byte, l As Long, s As Short, dv As Double)
        i += 2.5
        b *= 1.5
        l -= D
        s /= 4.0
        P += 0.5
        Q(1) += 2.5
        Dim c(D4) As Integer
        Dim a(2.5) As Integer
        ReDim a(D)
        Dim e(dv) As Integer
    End Sub
End Class", @"using System;

internal partial class C
{
    private const double D = 1.5d;
    private const double D4 = 4.4d;
    public int P { get; set; }
    public byte get_Q(int i)
    {
        return 0;
    }
    public void set_Q(int i, byte value)
    {
    }
    public void M(int i, byte b, long l, short s, double dv)
    {
        i = (int)Math.Round(i + 2.5d);
        b = (byte)Math.Round(b * 1.5d);
        l = (long)Math.Round(l - D);
        s = (short)Math.Round(s / 4.0d);
        P = (int)Math.Round(P + 0.5d);
        set_Q(1, (byte)Math.Round(get_Q(1) + 2.5d));
        var c = new int[5];
        var a = new int[3];
        a = new int[3];
        var e = new int[(int)Math.Round(dv) + 1];
    }
}");
    }

    [Fact]
    public async Task CoalesceOfTheSameSmallIntegerTypeIsCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(nb As Byte?, b As Byte, b2 As Byte)
        Console.WriteLine(If(Not nb, CByte(0)))
        Dim o1 As Object = If(nb << 2, b)
        Dim s1 As String = If(Not nb, b)
        Dim o2 As Object = If(nb And b2, CByte(0))
        Dim v = If(nb And b2, CByte(0))
        Dim w = If(nb And b2, 0)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(byte? nb, byte b, byte b2)
    {
        Console.WriteLine((byte?)~nb ?? 0);
        object o1 = (byte?)(nb << 2) ?? b;
        string s1 = ((byte?)~nb ?? b).ToString();
        object o2 = (byte?)(nb & b2) ?? 0;
        byte v = (byte?)(nb & b2) ?? 0;
        int w = nb & b2 ?? 0;
    }
}");
    }
}
