using System;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class TypeCastTests : ConverterTestBase
    {
        [Fact]
        public void CastObjectToInteger()
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
        public void DirectCastDoubleToInt()
        {
            TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim q = 2.37
        Dim j = DirectCast(q, Integer)
    End Sub
End Class", @"class Class1
{
    private void Test()
    {
        var q = 2.37;
        var j = (int)q;
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
        public void TryCastObjectToInteger()
        {
            TestConversionVisualBasicToCSharp(
@"Class Class1
    Private Sub Test()
        Dim o As Object = 5
        Dim i As System.Nullable(Of Integer) = TryCast(o, Integer)
    End Sub
End Class",
@"class Class1
{
    private void Test()
    {
        object o = 5;
        System.Nullable<int> i = o as int;
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
    }
}
