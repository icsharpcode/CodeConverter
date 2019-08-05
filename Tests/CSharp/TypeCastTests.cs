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
End Class" + Environment.NewLine, @"class Class1
{
    private void Test()
    {
        object o = 5;
        int i = System.Convert.ToInt32(o);
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

public class Class1
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
End Class" + Environment.NewLine, @"class Class1
{
    private void Test()
    {
        object o = ""Test"";
        string s = System.Convert.ToString(o);
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
End Class", @"class Class1
{
    private void Test()
    {
        var q = 2.37;
        var j = System.Convert.ToInt32(q);
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
@"class Class1
{
    private void Test()
    {
        object o = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> l = (System.Collections.Generic.List<int>)o;
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
@"class Class1
{
    private void Test()
    {
        object o = 5;
        int? i = System.Convert.ToInt32(o);
        string s = System.Convert.ToInt32(o).ToString();
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
@"class Class1
{
    private void Test()
    {
        object o = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> l = o as System.Collections.Generic.List<int>;
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
@"class Class1
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
@"class Class1
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
End Class" + Environment.NewLine, @"class Class1
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
    var CR = (char)0xD;
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
    var CR = (char)0xD;
}
");
        }
    }
}
