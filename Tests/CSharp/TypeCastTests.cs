using System;
using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class TypeCastTests : ConverterTestBase
    {
        [Fact]
        public async Task CIntObjectToInteger()
        {
            await TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As Integer = CInt(o)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices;

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
        public async Task CDate()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Class Class1
    Sub Foo()
        Dim x = CDate(""2019-09-04"")
End Class", @"using Microsoft.VisualBasic.CompilerServices;

public partial class Class1
{
    public void Foo()
    {
        var x = Conversions.ToDate(""2019-09-04"");
    }
}");
        }

        [Fact]
        public async Task CastObjectToString()
        {
            await TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim o As Object = ""Test""
        Dim s As String = CStr(o)
    End Sub
End Class" + Environment.NewLine, @"using Microsoft.VisualBasic.CompilerServices;

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
        public async Task CTypeDoubleToInt()
        {
            await TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim q = 2.37
        Dim j = CType(q, Integer)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

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
        public async Task CastObjectToGenericList()
        {
            await TestConversionVisualBasicToCSharp(
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
        var l = (List<int>)o;
    }
}");
        }

        [Fact]
        public async Task CTypeObjectToInteger()
        {
            await TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As System.Nullable(Of Integer) = CInt(o)
        Dim s As String = CType(o, Integer).ToString()
    End Sub
End Class",
@"using Microsoft.VisualBasic.CompilerServices;

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
        public async Task TryCastObjectToGenericList()
        {
            await TestConversionVisualBasicToCSharp(
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
        var l = o as List<int>;
    }
}");
        }

        [Fact]
        public async Task CastConstantNumberToLong()
        {
            await TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5L
    End Sub
End Class",
@"internal partial class Class1
{
    private void Test()
    {
        object o = 5L;
    }
}");
        }

        [Fact]
        public async Task CastConstantNumberToFloat()
        {
            await TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5F
    End Sub
End Class",
@"internal partial class Class1
{
    private void Test()
    {
        object o = 5F;
    }
}");
        }

        [Fact]
        public async Task CastConstantNumberToDecimal()
        {
            await TestConversionVisualBasicToCSharp(
                @"Class Class1
    Private Sub Test()
        Dim o As Object = 5.0D
    End Sub
End Class" + Environment.NewLine, @"internal partial class Class1
{
    private void Test()
    {
        object o = 5.0M;
    }
}" + Environment.NewLine);
        }

        [Fact]
        public async Task CastConstantNumberToCharacterW()
        {
            await TestConversionVisualBasicToCSharp(
                @"Private Sub Test()
    Dim CR = ChrW(&HD)
End Sub
", @"private void Test()
{
    char CR = (char)0xD;
}
");
        }

        [Fact]
        public async Task CastConstantNumberToCharacter()
        {
            await TestConversionVisualBasicToCSharp(
                @"Private Sub Test()
    Dim CR As Char = Chr(&HD)
End Sub
", @"private void Test()
{
    char CR = (char)0xD;
}
");
        }

        [Fact]
        public async Task TestSingleCharacterStringLiteralBecomesCharWhenNeeded()
        {
            await TestConversionVisualBasicToCSharp(
                @"Class CharTestClass
    Private Function QuoteSplit(ByVal text As String) As String()
        Return text.Split("""""""")
    End Function
End Class", @"internal partial class CharTestClass
{
    private string[] QuoteSplit(string text)
    {
        return text.Split('""');
    }
}");
        }
    }
}
