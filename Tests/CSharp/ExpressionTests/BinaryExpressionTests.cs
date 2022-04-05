using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class BinaryExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task OmitsConversionForEnumBinaryExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Enum RankEnum As SByte
    First = 1
    Second = 2
End Enum

Public Class TestClass
    Sub TestMethod()
        Dim eEnum = RankEnum.Second
        Dim enumEnumEquality As Boolean = eEnum = RankEnum.First
    End Sub
End Class", @"
internal enum RankEnum : sbyte
{
    First = 1,
    Second = 2
}

public partial class TestClass
{
    public void TestMethod()
    {
        var eEnum = RankEnum.Second;
        bool enumEnumEquality = eEnum == RankEnum.First;
    }
}");
    }

    [Fact]
    public async Task BinaryOperatorsIsIsNotLeftShiftRightShiftAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private bIs as Boolean = New Object Is New Object
    Private bIsNot as Boolean = New Object IsNot New Object
    Private bLeftShift as Integer = 1 << 3
    Private bRightShift as Integer = 8 >> 3
End Class", @"
internal partial class TestClass
{
    private bool bIs = ReferenceEquals(new object(), new object());
    private bool bIsNot = !ReferenceEquals(new object(), new object());
    private int bLeftShift = 1 << 3;
    private int bRightShift = 8 >> 3;
}");
    }

    [Fact]
    public async Task LikeOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim x = """" Like ""*x*""
    End Sub
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        bool x = LikeOperator.LikeString("""", ""*x*"", CompareMethod.Binary);
    }
}");
    }

    [Fact]
    public async Task ShiftAssignmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        x <<= 4
        x >>= 3
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int x = 1;
        x <<= 4;
        x >>= 3;
    }
}");
    }

    [Fact]
    public async Task IntegerArithmeticAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 7 ^ 6 Mod 5 \ 4 + 3 * 2
        x += 1
        x -= 2
        x *= 3
        x \= 4
        x ^= 5
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        double x = Math.Pow(7d, 6d) % (5 / 4) + 3 * 2;
        x += 1d;
        x -= 2d;
        x *= 3d;
        x = x / 4L;
        x = Math.Pow(x, 5d);
    }
}");
    }

    [Fact]
    public async Task ImplicitConversionsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Double = 1
        Dim y As Decimal = 2
        Dim i1 As Integer = 1
        Dim i2 As Integer = 2
        Dim d1 = i1 / i2
        Dim z = x + y
        Dim z2 = y + x
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        double x = 1d;
        decimal y = 2m;
        int i1 = 1;
        int i2 = 2;
        double d1 = i1 / (double)i2;
        double z = x + (double)y;
        double z2 = (double)y + x;
    }
}
");
    }

    [Fact]
    public async Task FloatingPointDivisionIsForcedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 10 / 3
        x /= 2
        Dim y = 10.0 / 3
        y /= 2
        Dim z As Integer = 8
        z /= 3
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        double x = 10d / 3d;
        x /= 2d;
        double y = 10.0d / 3d;
        y /= 2d;
        int z = 8;
        z = (int)Math.Round(z / 3d);
    }
}");
    }

    [Fact]
    public async Task ConditionalExpressionInBinaryExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Integer = 5 - If((str = """"), 1, 2)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        int result = 5 - (string.IsNullOrEmpty(str) ? 1 : 2);
    }
}");
    }
        [Fact]
        public async Task CastNullableToNonNullableAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod() As Boolean
        Dim x As Boolean? = Nothing
        Return x
    End Function

    Private Function TestMethod2() As Integer
        Dim x As Integer? = Nothing
        Return x
    End Function
End Class", @"
internal partial class TestClass712
{
    private bool TestMethod()
    {
        bool? x = default;
        return (bool)x;
    }

    private int TestMethod2()
    {
        int? x = default;
        return (int)x;
    }
}");
        }

        [Fact]
        public async Task NotOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod() As Integer
        Dim x As Boolean? = Nothing
        If Not x Then Return 1 Else Return 2
    End Function
End Class", @"
internal partial class TestClass712
{
    private int TestMethod()
    {
        bool? x = default;
        if (x == false)
            return 1;
        else
            return 2;
    }
}");
        }

        [Fact]
        public async Task AndOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Boolean? = Nothing
        Dim b As Boolean? = Nothing
        Dim x As Boolean = False

        If a And b Then Return
        If a AndAlso b Then Return
        If a And x Then Return
        If a AndAlso x Then Return
        If x And a Then Return
        If x AndAlso a Then Return

        Dim res As Boolean? = a And b
        res = a AndAlso b
        res = a And x
        res = a AndAlso x 
        res = x And a
        res = x AndAlso a 
        
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? a = default;
        bool? b = default;
        bool x = false;
        if ((a & b) == true)
            return;
        if ((a is var arg1 && arg1.HasValue && !arg1.Value ? (bool?)false : !(b is { } arg2) ? null : arg2 ? arg1 : false) == true)
            return;
        if ((a & x) == true)
            return;
        if ((a is var arg3 && !arg3.HasValue || arg3.Value) && x && arg3.HasValue)
            return;
        if ((x & a) == true)
            return;
        if (x && a.GetValueOrDefault())
            return;
        var res = a & b;
        res = a is var arg7 && arg7.HasValue && !arg7.Value ? (bool?)false : !(b is { } arg8) ? null : arg8 ? arg7 : false;
        res = a & x;
        res = a is var arg9 && arg9.HasValue && !arg9.Value ? false : x ? arg9 : false;
        res = x & a;
        res = x ? a : false;
    }
}");
        }

        [Fact]
        public async Task OrOperatorOnNullableBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Boolean? = Nothing
        Dim b As Boolean? = Nothing
        Dim x As Boolean = False

        If a Or b Then Return
        If a OrElse b Then Return
        If a Or x Then Return
        If a OrElse x Then Return
        If x Or a Then Return
        If x OrElse a Then Return

        Dim res As Boolean? = a Or b
        res = a OrElse b
        res = a Or x
        res = a OrElse x 
        res = x Or a
        res = x OrElse a 
        
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? a = default;
        bool? b = default;
        bool x = false;
        if ((a | b) == true)
            return;
        if (a.GetValueOrDefault() || b.GetValueOrDefault())
            return;
        if ((a | x) == true)
            return;
        if (a.GetValueOrDefault() || x)
            return;
        if ((x | a) == true)
            return;
        if (x || a.GetValueOrDefault())
            return;
        var res = a | b;
        res = a is var arg1 && arg1.GetValueOrDefault() ? (bool?)true : !(b is { } arg2) ? null : arg2 ? true : arg1;
        res = a | x;
        res = a is var arg3 && arg3.GetValueOrDefault() ? true : x ? true : arg3;
        res = x | a;
        res = x ? true : a;
    }
}");
        }

        [Fact]
        public async Task RelationalOperatorsOnNullableTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Integer? = Nothing
        Dim y As Integer? = Nothing
        Dim a As Integer = 0

        Dim res As Boolean? = x = y
        res = x <> y
        res = x > y
        res = x >= y
        res = x < y
        res = x <= y

        res = a = y
        res = a <> y
        res = a > y
        res = a >= y
        res = a < y
        res = a <= y

        res = x = a
        res = x <> a
        res = x > a
        res = x >= a
        res = x < a
        res = x <= a
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int? x = default;
        int? y = default;
        int a = 0;
        var res = x is var arg1 && y is { } arg2 && arg1.HasValue ? arg1 == arg2 : (bool?)null;
        res = x is var arg3 && y is { } arg4 && arg3.HasValue ? arg3 != arg4 : (bool?)null;
        res = x is var arg5 && y is { } arg6 && arg5.HasValue ? arg5 > arg6 : (bool?)null;
        res = x is var arg7 && y is { } arg8 && arg7.HasValue ? arg7 >= arg8 : (bool?)null;
        res = x is var arg9 && y is { } arg10 && arg9.HasValue ? arg9 < arg10 : (bool?)null;
        res = x is var arg11 && y is { } arg12 && arg11.HasValue ? arg11 <= arg12 : (bool?)null;
        res = a is var arg13 && y is { } arg14 ? arg13 == arg14 : (bool?)null;
        res = a is var arg15 && y is { } arg16 ? arg15 != arg16 : (bool?)null;
        res = a is var arg17 && y is { } arg18 ? arg17 > arg18 : (bool?)null;
        res = a is var arg19 && y is { } arg20 ? arg19 >= arg20 : (bool?)null;
        res = a is var arg21 && y is { } arg22 ? arg21 < arg22 : (bool?)null;
        res = a is var arg23 && y is { } arg24 ? arg23 <= arg24 : (bool?)null;
        res = a is var arg26 && x is { } arg25 ? arg25 == arg26 : (bool?)null;
        res = a is var arg28 && x is { } arg27 ? arg27 != arg28 : (bool?)null;
        res = a is var arg30 && x is { } arg29 ? arg29 > arg30 : (bool?)null;
        res = a is var arg32 && x is { } arg31 ? arg31 >= arg32 : (bool?)null;
        res = a is var arg34 && x is { } arg33 ? arg33 < arg34 : (bool?)null;
        res = a is var arg36 && x is { } arg35 ? arg35 <= arg36 : (bool?)null;
    }
}");
        }

        [Fact]
        public async Task RelationalOperatorsOnNullableTypeInConditionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Integer? = Nothing
        Dim y As Integer? = Nothing
        Dim a As Integer = 0

        If x = y Then Return
        If x <> y Then Return
        If x > y Then Return
        If x >= y Then Return
        If x < y Then Return
        If x <= y Then Return

        If a = y Then Return
        If a <> y Then Return
        If a > y Then Return
        If a >= y Then Return
        If a < y Then Return
        If a <= y Then Return

        IF x = a Then Return
        IF x <> a Then Return
        IF x > a Then Return
        IF x >= a Then Return
        IF x < a Then Return
        IF x <= a Then Return
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int? x = default;
        int? y = default;
        int a = 0;
        if (x is var arg1 && y is { } arg2 && arg1.HasValue && arg1 == arg2)
            return;
        if (x is var arg3 && y is { } arg4 && arg3.HasValue && arg3 != arg4)
            return;
        if (x is var arg5 && y is { } arg6 && arg5.HasValue && arg5 > arg6)
            return;
        if (x is var arg7 && y is { } arg8 && arg7.HasValue && arg7 >= arg8)
            return;
        if (x is var arg9 && y is { } arg10 && arg9.HasValue && arg9 < arg10)
            return;
        if (x is var arg11 && y is { } arg12 && arg11.HasValue && arg11 <= arg12)
            return;
        if (a is var arg13 && y is { } arg14 && arg13 == arg14)
            return;
        if (a is var arg15 && y is { } arg16 && arg15 != arg16)
            return;
        if (a is var arg17 && y is { } arg18 && arg17 > arg18)
            return;
        if (a is var arg19 && y is { } arg20 && arg19 >= arg20)
            return;
        if (a is var arg21 && y is { } arg22 && arg21 < arg22)
            return;
        if (a is var arg23 && y is { } arg24 && arg23 <= arg24)
            return;
        if (a is var arg26 && x is { } arg25 && arg25 == arg26)
            return;
        if (a is var arg28 && x is { } arg27 && arg27 != arg28)
            return;
        if (a is var arg30 && x is { } arg29 && arg29 > arg30)
            return;
        if (a is var arg32 && x is { } arg31 && arg31 >= arg32)
            return;
        if (a is var arg34 && x is { } arg33 && arg33 < arg34)
            return;
        if (a is var arg36 && x is { } arg35 && arg35 <= arg36)
            return;
    }
}");
        }

        [Fact]
        public async Task NullableBooleanComparedToNormalBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim a = var1 = False
        Dim b = var1 = True
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? var1 = default;
        var a = false is var arg2 && var1 is { } arg1 ? arg1 == arg2 : (bool?)null;
        var b = true is var arg4 && var1 is { } arg3 ? arg3 == arg4 : (bool?)null;
    }
}");
        }

        [Fact]
        public async Task ImplicitBooleanConversion712Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim var2 As Boolean? = Nothing
        Return var1 OrElse Not var2
    End Function
End Class", @"
internal partial class TestClass712
{
    private object TestMethod()
    {
        bool? var1 = default;
        bool? var2 = default;
        return var1 is var arg1 && arg1.GetValueOrDefault() ? (bool?)true : !(!var2 is { } arg2) ? null : arg2 ? true : arg1;
    }
}");
    }

    [Fact]
    public async Task ImplicitIfStatementBooleanConversion712Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass712
    Private Function TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim var2 As Boolean? = Nothing
        If var1 OrElse Not var2 Then Return True Else Return False
    End Function
End Class", @"
internal partial class TestClass712
{
    private object TestMethod()
    {
        bool? var1 = default;
        bool? var2 = default;
        if (var1.GetValueOrDefault() || (!var2).GetValueOrDefault())
            return true;
        else
            return false;
    }
}");
    }

    [Fact]
    public async Task ConversionInComparisonOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class ConversionInComparisonOperatorTest
    Public Sub Foo()
        Dim SomeDecimal As Decimal = 12.3
        Dim ACalc As Double = 32.1
        If ACalc > 60 / SomeDecimal Then
            Console.WriteLine(1)
        End If
    End Sub
End Class", @"using System;

public partial class ConversionInComparisonOperatorTest
{
    public void Foo()
    {
        decimal SomeDecimal = 12.3m;
        double ACalc = 32.1d;
        if (ACalc > (double)(60m / SomeDecimal))
        {
            Console.WriteLine(1);
        }
    }
}");
    }
}