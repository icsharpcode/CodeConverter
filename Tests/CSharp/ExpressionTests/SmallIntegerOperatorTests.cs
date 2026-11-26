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
}
