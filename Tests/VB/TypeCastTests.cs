using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB;

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
", @"
Private Sub Test()
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
    Dim s = CStr(o)
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
    Dim l = CType(o, List(Of Integer))
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
    Dim CR = Microsoft.VisualBasic.ChrW(&HD)
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
    Public Sub TestMethod(o As Object)
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
    Public Sub TestMethod(o As Object)
        TryCast(o, Test).TestMethod()
    End Sub
End Class");
    }

    [Fact]
    public async Task NativeIntegerTypesAsync() {
        await TestConversionCSharpToVisualBasicAsync(
            @"public class Test {
    private nint _signed;
    private nuint _unsigned;
    public nuint TestMethod(nint value, int i, uint u) {
        _signed = value;
        _signed = (nint)i;
        _unsigned = (nuint)u;
        return _unsigned;
    }
}",
            @"Public Class Test
    Private _signed As IntPtr
    Private _unsigned As UIntPtr
    Public Function TestMethod(value As IntPtr, i As Integer, u As UInteger) As UIntPtr
        _signed = value
        _signed = CType(i, IntPtr)
        _unsigned = CType(u, UIntPtr)
        Return _unsigned
    End Function
End Class");
    }

    [Fact]
    public async Task IntPtrArgumentAsync() {
        await TestConversionCSharpToVisualBasicAsync(
            @"using System;
using System.Runtime.InteropServices;

class NativeMethods {
    [DllImport(""user32.dll"", EntryPoint = ""GetWindowLongPtr"")]
    static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
    public static long GetWindowLong(IntPtr hWnd, int nIndex) {
        return GetWindowLongPtr64(hWnd, nIndex).ToInt64();
    }
}",
            @"Imports System
Imports System.Runtime.InteropServices

Friend Class NativeMethods
    <DllImport(""user32.dll"", EntryPoint:=""GetWindowLongPtr"")>
    Private Shared Function GetWindowLongPtr64(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function
    Public Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Long
        Return GetWindowLongPtr64(hWnd, nIndex).ToInt64()
    End Function
End Class");
    }

    [Fact]
    public async Task NativeIntegerTypeContextsAsync() {
        await TestConversionCSharpToVisualBasicAsync(
            @"using System;
using System.Collections.Generic;

public class Test {
    public nint[] Array = new nint[1];
    public List<nuint> List = new List<nuint>();
    public nint? Nullable;
    public Func<nint, nuint> Convert;

    public Type TestMethod() {
        nint local = 5;
        var inferred = (nint)local;
        nint max = nint.MaxValue;
        nint zero = default(nint);
        nint none = default;
        Nullable = local;
        return typeof(nuint);
    }
}",
            @"Imports System
Imports System.Collections.Generic

Public Class Test
    Public Array As IntPtr() = New IntPtr(0) {}
    Public List As List(Of UIntPtr) = New List(Of UIntPtr)()
    Public Nullable As IntPtr?
    Public Convert As Func(Of IntPtr, UIntPtr)

    Public Function TestMethod() As Type
        Dim local As IntPtr = 5
        Dim inferred = local
        Dim max = IntPtr.MaxValue
        Dim zero As IntPtr = Nothing
        Dim none As IntPtr = Nothing
        Nullable = local
        Return GetType(UIntPtr)
    End Function
End Class");
    }

    [Fact]
    public async Task UserTypeNamedNintAsync() {
        await TestConversionCSharpToVisualBasicAsync(
            @"using System;

class nint { }

class Test {
    private nint _custom = new nint();
    private IntPtr _pointer = IntPtr.Zero;
    private System.UIntPtr _unsigned = System.UIntPtr.Zero;
}",
            @"Imports System

Friend Class nint
End Class

Friend Class Test
    Private _custom As nint = New nint()
    Private _pointer As IntPtr = IntPtr.Zero
    Private _unsigned As UIntPtr = UIntPtr.Zero
End Class");
    }
}