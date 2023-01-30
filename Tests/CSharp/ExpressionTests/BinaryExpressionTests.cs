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
        x = (double)(x / 4L);
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
        if ((a is var arg1 && arg1.HasValue && !arg1.Value ? false : !(b is { } arg2) ? null : arg2 ? arg1 : false) == true)
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
        res = a is var arg7 && arg7.HasValue && !arg7.Value ? false : !(b is { } arg8) ? null : arg8 ? arg7 : false;
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
        res = a is var arg1 && arg1.GetValueOrDefault() ? true : b is not { } arg2 ? null : arg2 ? true : arg1;
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

        var res = x.HasValue && y.HasValue ? x == y : (bool?)null;
        res = x.HasValue && y.HasValue ? x != y : null;
        res = x.HasValue && y.HasValue ? x > y : null;
        res = x.HasValue && y.HasValue ? x >= y : null;
        res = x.HasValue && y.HasValue ? x < y : null;
        res = x.HasValue && y.HasValue ? x <= y : null;

        res = y.HasValue ? a == y : null;
        res = y.HasValue ? a != y : null;
        res = y.HasValue ? a > y : null;
        res = y.HasValue ? a >= y : null;
        res = y.HasValue ? a < y : null;
        res = y.HasValue ? a <= y : null;

        res = x.HasValue ? x == a : null;
        res = x.HasValue ? x != a : null;
        res = x.HasValue ? x > a : null;
        res = x.HasValue ? x >= a : null;
        res = x.HasValue ? x < a : null;
        res = x.HasValue ? x <= a : null;
    }
}");
        }

        [Fact]
        public async Task SimplifiesAlreadyCheckedNullableComparison_HasValueAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays.HasValue AndAlso oldDays.HasValue AndAlso newDays <> oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return newDays.HasValue && oldDays.HasValue && newDays != oldDays;
}");
    }

        [Fact]
        public async Task SimplifiesAlreadyCheckedNullableComparison_NotNothingAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays IsNot Nothing AndAlso oldDays IsNot Nothing AndAlso newDays = oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return newDays is not null && oldDays is not null && newDays == oldDays;
}");
        }

        [Fact]
        public async Task DoesNotSimplifyComparisonWhenNullChecksAreNotDefinitelyTrueAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return (newDays.HasValue AndAlso oldDays.HasValue OrElse True) AndAlso newDays > oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue && oldDays.HasValue || true ? newDays.HasValue && oldDays.HasValue ? newDays > oldDays : null : (bool?)false);
}");
        }

        [Fact]
        public async Task HalfSimplifiesComparisonWhenOneSideAlreadyNullCheckedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return newDays.HasValue AndAlso newDays < oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue ? oldDays.HasValue ? newDays < oldDays : null : (bool?)false);
}");
    }

        [Fact]
        public async Task DoesNotSimplifyComparisonWhenNullableChecksAreUncertainAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Private Function TestMethod(newDays As Integer?, oldDays As Integer?) As Boolean
    Return (newDays.HasValue OrElse oldDays.HasValue) AndAlso newDays <> oldDays
End Function
", @"
private bool TestMethod(int? newDays, int? oldDays)
{
    return (bool)(newDays.HasValue || oldDays.HasValue ? newDays.HasValue && oldDays.HasValue ? newDays != oldDays : null : (bool?)false);
}");
        }

        [Fact]
        public async Task SimplifiesNullableEnumIfEqualityCheckAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Enum PasswordStatus
    Expired
    Locked    
End Enum
Public Class TestForEnums
    Public Shared Sub WriteStatus(status As PasswordStatus?)
      If status = PasswordStatus.Locked Then
          Console.Write(""Locked"")
      End If
    End Sub
End Class
", @"using System;

public enum PasswordStatus
{
    Expired,
    Locked
}

public partial class TestForEnums
{
    public static void WriteStatus(PasswordStatus? status)
    {
        if (status.HasValue && status == PasswordStatus.Locked)
        {
            Console.Write(""Locked"");
        }
    }
}");
        }

        [Fact]
        public async Task SimplifiesNullableDateIfEqualityCheckAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestForDates
    Public Shared Sub WriteStatus(adminDate As DateTime?, chartingTimeAllowanceEnd As DateTime)
        If adminDate Is Nothing OrElse adminDate > chartingTimeAllowanceEnd Then
            adminDate = DateTime.Now
        End If
    End Sub
End Class
", @"using System;

public partial class TestForDates
{
    public static void WriteStatus(DateTime? adminDate, DateTime chartingTimeAllowanceEnd)
    {
        if (adminDate is null || adminDate > chartingTimeAllowanceEnd)
        {
            adminDate = DateTime.Now;
        }
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

        if (x.HasValue && y.HasValue && x == y)
            return;
        if (x.HasValue && y.HasValue && x != y)
            return;
        if (x.HasValue && y.HasValue && x > y)
            return;
        if (x.HasValue && y.HasValue && x >= y)
            return;
        if (x.HasValue && y.HasValue && x < y)
            return;
        if (x.HasValue && y.HasValue && x <= y)
            return;

        if (y.HasValue && a == y)
            return;
        if (y.HasValue && a != y)
            return;
        if (y.HasValue && a > y)
            return;
        if (y.HasValue && a >= y)
            return;
        if (y.HasValue && a < y)
            return;
        if (y.HasValue && a <= y)
            return;

        if (x.HasValue && x == a)
            return;
        if (x.HasValue && x != a)
            return;
        if (x.HasValue && x > a)
            return;
        if (x.HasValue && x >= a)
            return;
        if (x.HasValue && x < a)
            return;
        if (x.HasValue && x <= a)
            return;
    }
}");
        }

        [Fact]
        public async Task NullableBooleanComparedToNsormalBooleanAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Dim newDays As Integer?
Dim oldDays As Integer?

If (newDays.HasValue AndAlso Not oldDays.HasValue) _
                OrElse (newDays.HasValue AndAlso oldDays.HasValue AndAlso newDays <> oldDays) _
                OrElse (Not newDays.HasValue AndAlso oldDays.HasValue) Then

'Some code
End If", @"
internal partial class TestClass
{
    private void TestMethod()
    {sd
        bool? var1 = default;
        var a = var1.HasValue ? var1 == false : (bool?)null;
        var b = var1.HasValue ? var1 == true : (bool?)null;
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
        var a = var1.HasValue ? var1 == false : (bool?)null;
        var b = var1.HasValue ? var1 == true : (bool?)null;
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
        return (object)(var1 is var arg1 && arg1.GetValueOrDefault() ? true : !var2 is not { } arg2 ? null : arg2 ? true : arg1);
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