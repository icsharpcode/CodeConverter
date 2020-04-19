using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB
{
    public class TypeCastTests : ConverterTestBase
    {
        [Fact]
        public async Task CastObjectToIntegerAsync()
        {
            // The leading and trailing newlines check that surrounding trivia is selected as part of this (in the comments auto-testing)
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task CastObjectToStringAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task CastObjectToGenericListAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task TryCastObjectToIntegerAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task TryCastObjectToGenericListAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task TryCastObjectToGenericTypeAsync() {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task CastConstantNumberToLongAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"void Test()
{
    object o = 5L;
}", @"Private Sub Test()
    Dim o As Object = 5L
End Sub");
        }

        [Fact]
        public async Task CastConstantNumberToFloatAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"void Test()
{
    object o = 5.0f;
}", @"Private Sub Test()
    Dim o As Object = 5.0F
End Sub");
        }

        [Fact]
        public async Task CastConstantNumberToDecimalAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"void Test()
{
    object o = 5.0m;
}", @"Private Sub Test()
    Dim o As Object = 5.0D
End Sub");
        }

        [Fact]
        public async Task CastConstantNumberToCharacterAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
@"void Test() {
    char CR = (char)0xD;
}",
@"Private Sub Test()
    Dim CR As Char = Microsoft.VisualBasic.ChrW(&HD)
End Sub", conversionOptions: EmptyNamespaceOptionStrictOff);
        }
        [Fact]
        public async Task CastCharacterToNumberAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"void Test() {
    byte a = (byte)'A';
    decimal b = (byte)'B';
}",
@"Private Sub Test()
    Dim a As Byte = Microsoft.VisualBasic.AscW(""A""c)
    Dim b As Decimal = Microsoft.VisualBasic.AscW(""B""c)
End Sub", conversionOptions: EmptyNamespaceOptionStrictOff);
        }
        [Fact(Skip = "Many code generation")]
        public async Task CastCharacterIncrementAsync() {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task MethodInvocationAsync() {
            await TestConversionCSharpToVisualBasicAsync(
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
        public async Task MethodInvocation_TryCastAsync() {
            await TestConversionCSharpToVisualBasicAsync(
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
