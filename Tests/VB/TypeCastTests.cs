using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class TypeCastTests : ConverterTestBase
    {
        [Fact]
        public async Task CastObjectToInteger()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5;
    int i = (int) o;
}
", @"Private Sub Test()
    Dim o As Object = 5
    Dim i As Integer = o
End Sub
");
        }

        [Fact]
        public async Task CastObjectToString()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = ""Test"";
    string s = (string) o;
}
", @"Private Sub Test()
    Dim o As Object = ""Test""
    Dim s = CStr(o)
End Sub
");
        }

        [Fact]
        public async Task CastObjectToGenericList()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = new System.Collections.Generic.List<int>();
    System.Collections.Generic.List<int> l = (System.Collections.Generic.List<int>) o;
}
", @"Private Sub Test()
    Dim o As Object = New System.Collections.Generic.List(Of Integer)()
    Dim l = CType(o, System.Collections.Generic.List(Of Integer))
End Sub
");
        }

        [Fact]
        public async Task TryCastObjectToInteger()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5;
    System.Nullable<int> i = o as int?;
}
", @"Private Sub Test()
    Dim o As Object = 5
    Dim i As Integer? = o
End Sub
");
        }

        [Fact]
        public async Task TryCastObjectToGenericList()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = new System.Collections.Generic.List<int>();
    System.Collections.Generic.List<int> l = o as System.Collections.Generic.List<int>;
}
", @"Private Sub Test()
    Dim o As Object = New System.Collections.Generic.List(Of Integer)()
    Dim l = TryCast(o, System.Collections.Generic.List(Of Integer))
End Sub
");
        }

        [Fact]
        public async Task CastConstantNumberToLong()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5L;
}
", @"Private Sub Test()
    Dim o As Object = 5L
End Sub
");
        }

        [Fact]
        public async Task CastConstantNumberToFloat()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5.0f;
}
", @"Private Sub Test()
    Dim o As Object = 5.0F
End Sub
");
        }

        [Fact]
        public async Task CastConstantNumberToDecimal()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5.0m;
}
", @"Private Sub Test()
    Dim o As Object = 5.0D
End Sub
");
        }

        [Fact]
        public async Task CastConstantNumberToCharacter()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    char CR = (char)0xD;
}
", @"Private Sub Test()
    Dim CR = ChrW(&HD)
End Sub
");
        }
        [Fact]
        public async Task MethodInvocation() {
            await TestConversionCSharpToVisualBasic(
@"public class Test {
    public void TestMethod() { }
}
public class Test2 {
    public void TestMethod(object o) {
        ((Test)o).TestMethod();
    }
}",
@"Public Class Test
    Public Sub TestMethod()
    End Sub
End Class

Public Class Test2
    Public Sub TestMethod(ByVal o As Object)
        CType(o, Test).TestMethod()
    End Sub
End Class");
        }
        [Fact]
        public async Task MethodInvocation_TryCast() {
            await TestConversionCSharpToVisualBasic(
@"public class Test {
    public void TestMethod() { }
}
public class Test2 {
    public void TestMethod(object o) {
        (o as Test).TestMethod();
    }
}",
@"Public Class Test
    Public Sub TestMethod()
    End Sub
End Class

Public Class Test2
    Public Sub TestMethod(ByVal o As Object)
        TryCast(o, Test).TestMethod()
    End Sub
End Class");
        }
    }
}
