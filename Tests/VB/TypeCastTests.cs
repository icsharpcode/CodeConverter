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
            // The leading and trailing newlines check that surrounding trivia is selected as part of this (in the comments auto-testing)
            await TestConversionCSharpToVisualBasic(
                @"
void Test()
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
}", @"Private Sub Test()
    Dim o As Object = ""Test""
    Dim s As String = CStr(o)
End Sub");
        }

        [Fact]
        public async Task CastObjectToGenericList()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = new System.Collections.Generic.List<int>();
    System.Collections.Generic.List<int> l = (System.Collections.Generic.List<int>) o;
}", @"Private Sub Test()
    Dim o As Object = New List(Of Integer)()
    Dim l As List(Of Integer) = CType(o, List(Of Integer))
End Sub");
        }

        [Fact]
        public async Task TryCastObjectToInteger()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5;
    System.Nullable<int> i = o as int?;
}", @"Private Sub Test()
    Dim o As Object = 5
    Dim i As Integer? = o
End Sub");
        }

        [Fact]
        public async Task TryCastObjectToGenericList()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = new System.Collections.Generic.List<int>();
    System.Collections.Generic.List<int> l = o as System.Collections.Generic.List<int>;
}", @"Private Sub Test()
    Dim o As Object = New List(Of Integer)()
    Dim l As List(Of Integer) = TryCast(o, List(Of Integer))
End Sub");
        }

        [Fact]
        public async Task TryCastObjectToGenericType() {
            await TestConversionCSharpToVisualBasic(
@"T Test<T>() where T : class {
    return this as T;
}
",
@"Private Function Test(Of T As Class)() As T
    Return TryCast(Me, T)
End Function
");
        }

        [Fact]
        public async Task CastConstantNumberToLong()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5L;
}", @"Private Sub Test()
    Dim o As Object = 5L
End Sub");
        }

        [Fact]
        public async Task CastConstantNumberToFloat()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5.0f;
}", @"Private Sub Test()
    Dim o As Object = 5.0F
End Sub");
        }

        [Fact]
        public async Task CastConstantNumberToDecimal()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    object o = 5.0m;
}", @"Private Sub Test()
    Dim o As Object = 5.0D
End Sub");
        }

        [Fact]
        public async Task CastConstantNumberToCharacter()
        {
            await TestConversionCSharpToVisualBasic(
                @"void Test()
{
    char CR = (char)0xD;
}", @"Private Sub Test()
    Dim CR As Char = ChrW(&HD)
End Sub");
        }
        [Fact]
        public async Task CastCharacterToNumber() {
            await TestConversionCSharpToVisualBasic(
@"void Test() {
    byte a = (byte)'A';
    decimal b = (byte)'B';
}
",
@"Private Sub Test()
    Dim a As Byte = AscW(""A""c)
    Dim b As Decimal = AscW(""B""c)
End Sub
");
        }
        [Fact(Skip = "Many code generation")]
        public async Task CastCharacterIncrement() {
            await TestConversionCSharpToVisualBasic(
@"void Test() {
    char a = 'A';
    a++;
}
",
@"Private Sub Test()
    Dim a As Char = ""A""c
    a == ChrW(AscW(a) + 1)
End Sub");
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
