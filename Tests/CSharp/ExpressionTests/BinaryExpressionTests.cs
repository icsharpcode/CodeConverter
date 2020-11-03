using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
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
}
