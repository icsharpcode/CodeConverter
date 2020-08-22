using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    public class TypeCastTests : ConverterTestBase
    {
        [Fact]
        public async Task NumericStringToEnumAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class NumericStringToEnum
    Public Shared Sub Main()
        MsgBox(NameOf(Main), ""1"", True)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class NumericStringToEnum
{
    public static void Main()
    {
        Interaction.MsgBox(nameof(Main), (MsgBoxStyle)Conversions.ToInteger(""1""), true);
    }
}" + Environment.NewLine);
        }

        [Fact]
        public async Task CIntObjectToIntegerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As Integer = CInt(o)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = 5;
        int i = Conversions.ToInteger(o);
    }
}" + Environment.NewLine);
        }

        [Fact]
        public async Task CDateAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class Class1
    Sub Foo()
        Dim x = CDate(""2019-09-04"")
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        DateTime x = Conversions.ToDate(""2019-09-04"");
    }
}");
        }

        [Fact]
        public async Task CastObjectToStringAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = CStr(o)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = Conversions.ToString(o);
    }
}" + Environment.NewLine);
        }
        [Fact]
        public async Task ImplicitCastObjectToStringAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = o
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = Conversions.ToString(o);
    }
}" + Environment.NewLine);
        }
        [Fact]
        public async Task CastArrayListAssignmentToStringAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim x As New ArrayList
        x.Add(""a"")

        Dim xs(1) As String

        xs(0) = x(0)
    End Sub
End Class" + Environment.NewLine, @"using System.Collections;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        var x = new ArrayList();
        x.Add(""a"");
        var xs = new string[2];
        xs[0] = Conversions.ToString(x[0]);
    }
}" + Environment.NewLine);
        }
        [Fact]
        public async Task ImplicitCastObjecStringToStringAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = o
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = Conversions.ToString(o);
    }
}" + Environment.NewLine);
        }
        [Fact]
        public async Task CTypeDoubleToIntAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim q = 2.37
        Dim j = CType(q, Integer)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        double q = 2.37;
        int j = Conversions.ToInteger(q);
    }
}");
        }

        [Fact]
        public async Task CastObjectToGenericListAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = New System.Collections.Generic.List(Of Integer)()
        Dim l As System.Collections.Generic.List(Of Integer) = CType(o, System.Collections.Generic.List(Of Integer))
    End Sub
End Class",
@"using System.Collections.Generic;

internal partial class Class1
{
    private void Test()
    {
        object o = new List<int>();
        List<int> l = (List<int>)o;
    }
}");
        }

        [Fact]
        public async Task CTypeObjectToIntegerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As System.Nullable(Of Integer) = CInt(o)
        Dim s As String = CType(o, Integer).ToString()
    End Sub
End Class",
@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Class1
{
    private void Test()
    {
        object o = 5;
        int? i = Conversions.ToInteger(o);
        string s = Conversions.ToInteger(o).ToString();
    }
}");
        }

        [Fact]
        public async Task TryCastObjectToGenericListAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = New System.Collections.Generic.List(Of Integer)()
        Dim l As System.Collections.Generic.List(Of Integer) = TryCast(o, System.Collections.Generic.List(Of Integer))
    End Sub
End Class",
@"using System.Collections.Generic;

internal partial class Class1
{
    private void Test()
    {
        object o = new List<int>();
        List<int> l = o as List<int>;
    }
}");
        }

        [Fact]
        public async Task CastConstantNumberToLongAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5L
    End Sub
End Class",
@"
internal partial class Class1
{
    private void Test()
    {
        object o = 5L;
    }
}");
        }

        [Fact]
        public async Task CastConstantNumberToFloatAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5F
    End Sub
End Class",
@"
internal partial class Class1
{
    private void Test()
    {
        object o = 5F;
    }
}");
        }

        [Fact]
        public async Task CastConstantNumberToDecimalAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class Class1
    Private Sub Test()
        Dim o As Object = 5.0D
    End Sub
End Class" + Environment.NewLine, @"
internal partial class Class1
{
    private void Test()
    {
        object o = 5.0M;
    }
}" + Environment.NewLine);
        }

        [Fact]
        public async Task CastConstantNumberToCharacterWAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Private Sub Test()
    Dim CR = ChrW(&HF)
End Sub
", @"private void Test()
{
    char CR = '\u000f';
}
");
        }

        [Fact]
        public async Task CastConstantNumberToCharacterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Private Sub Test()
    Dim CR As Char = Chr(&HF)
End Sub
", @"private void Test()
{
    char CR = '\u000f';
}
");
        }

        [Fact]
        public async Task TestSingleCharacterStringLiteralBecomesCharWhenNeededAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class CharTestClass
    Private Function QuoteSplit(ByVal text As String) As String()
        Return text.Split("""""""")
    End Function
End Class", @"
internal partial class CharTestClass
{
    private string[] QuoteSplit(string text)
    {
        return text.Split('""');
    }
}");
        }

        [Fact]
        public async Task TestSingleCharacterStringLiteralBecomesChar_WhenExplictCastAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class ExplicitCastClass
    Dim wordArray As String() = 1.ToString().Split(CChar("",""))
End Class", @"
internal partial class ExplicitCastClass
{
    private string[] wordArray = 1.ToString().Split(',');
}");
        }

        [Fact]
        public async Task TestCastHasBracketsWhenElementAccessAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class TestCastHasBracketsWhenElementAccess
    Private Function Casting(ByVal sender As Object) As Integer
        Return CInt(DirectCast(sender, Object())(0))
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestCastHasBracketsWhenElementAccess
{
    private int Casting(object sender)
    {
        return Conversions.ToInteger(((object[])sender)[0]);
    }
}");
        }

        [Fact]
        public async Task MultipleNestedCastsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Class MultipleCasts
    Public Shared Function ToGenericParameter(Of T)(Value As Object) As T
        If Value Is Nothing Then
            Return Nothing
        End If
        Dim reflectedType As Global.System.Type = GetType(T)
        If Global.System.Type.Equals(reflectedType, GetType(Global.System.Int16)) Then
            Return DirectCast(CObj(CShort(Value)), T)
        ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.UInt64)) Then
            Return DirectCast(CObj(CULng(Value)), T)
        Else
            Return DirectCast(Value, T)
        End If
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class MultipleCasts
{
    public static T ToGenericParameter<T>(object Value)
    {
        if (Value is null)
        {
            return default;
        }

        var reflectedType = typeof(T);
        if (Equals(reflectedType, typeof(short)))
        {
            return (T)(object)Conversions.ToShort(Value);
        }
        else if (Equals(reflectedType, typeof(ulong)))
        {
            return (T)(object)Conversions.ToULong(Value);
        }
        else
        {
            return (T)Value;
        }
    }
}");
        }

        /// <summary>
        /// We just use ConditionalCompareObjectEqual to make it a bool, but VB emits a late binding call something like this:
        /// array[0] = Operators.CompareObjectEqual(left, right, false);
        /// array[1] = "Identical values stored in objects should be equal";
        /// NewLateBinding.LateCall(this, null, "AssertTrue", array, null, null, null, true);
        /// This will likely be the same in the vast majority of cases
        /// </summary>
        [Fact]
        public async Task ObjectComparisonIsConvertedToBoolRatherThanLateBoundAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Class CopiedFromTheSelfVerifyingBooleanTests
    Public Sub VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
        Dim a1 As Object = 3
        Dim a2 As Object = 3
        AssertTrue(a1 = a2, ""Identical values stored in objects should be equal"")
    End Sub

    Private Sub AssertTrue(v1 As Nullable(Of Boolean), v2 As String)
    End Sub

    Private Sub AssertTrue(v1 As Boolean, v2 As String)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class CopiedFromTheSelfVerifyingBooleanTests
{
    public void VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
    {
        object a1 = 3;
        object a2 = 3;
        AssertTrue(Operators.ConditionalCompareObjectEqual(a1, a2, false), ""Identical values stored in objects should be equal"");
    }

    private void AssertTrue(bool? v1, string v2)
    {
    }

    private void AssertTrue(bool v1, string v2)
    {
    }
}");
        }
    }
}
