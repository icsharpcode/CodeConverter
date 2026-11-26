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
}
