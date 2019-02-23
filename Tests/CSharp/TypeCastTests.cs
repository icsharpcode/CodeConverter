using System;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class TypeCastTests : ConverterTestBase
    {
        [Fact]
        public void CIntObjectToInteger()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CastObjectToString()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CTypeDoubleToInt()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CastObjectToGenericList()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CTypeObjectToInteger()
        {
            TestConversionVisualBasicToCSharp(
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
        System.Nullable<int> i = System.Convert.ToInt32(o);
        string s = System.Convert.ToInt32(o).ToString();
    }
}");
        }

        [Fact]
        public void TryCastObjectToGenericList()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CastConstantNumberToLong()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CastConstantNumberToFloat()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CastConstantNumberToDecimal()
        {
            TestConversionVisualBasicToCSharp(
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
        public void CastConstantNumberToCharacterW()
        {
            TestConversionVisualBasicToCSharp(
                @"Private Sub Test()
    Dim CR As Char = ChrW(&HD)
End Sub
", @"private void Test()
{
    char CR = (char)0xD;
}
");
        }

        [Fact]
        public void CastConstantNumberToCharacter()
        {
            TestConversionVisualBasicToCSharp(
                @"Private Sub Test()
    Dim CR As Char = Chr(&HD)
End Sub
", @"private void Test()
{
    char CR = (char)0xD;
}
");
        }
    }
}
