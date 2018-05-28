using Xunit;

namespace CodeConverter.Tests.VB
{
    public class NamespaceLevelTests : ConverterTestBase
    {
        [Fact]
        public void TestNamespace()
        {
            TestConversionCSharpToVisualBasic(@"namespace Test
{
    
}", @"Namespace Test
End Namespace");
        }

        [Fact]
        public void TestTopLevelAttribute()
        {
            TestConversionCSharpToVisualBasic(
                @"[assembly: CLSCompliant(true)]",
                @"
<Assembly: CLSCompliant(True)>");
        }

        [Fact]
        public void TestImports()
        {
            TestConversionCSharpToVisualBasic(
                @"using SomeNamespace;
using VB = Microsoft.VisualBasic;",
                @"Imports SomeNamespace
Imports VB = Microsoft.VisualBasic");
        }

        [Fact]
        public void TestClass()
        {
            TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    class TestClass<T>
    {
    }
}", @"Namespace Test.[class]
    Friend Class TestClass(Of T)
    End Class
End Namespace");
        }

        [Fact]
        public void TestInternalStaticClass()
        {
            TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    internal static class TestClass
    {
        public static void Test() {}
        static void Test2() {}
    }
}", @"Namespace Test.[class]
    Friend Module TestClass
        Sub Test()
        End Sub

        Private Sub Test2()
        End Sub
    End Module
End Namespace");
        }

        [Fact]
        public void TestAbstractClass()
        {
            TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    abstract class TestClass
    {
    }
}", @"Namespace Test.[class]
    Friend MustInherit Class TestClass
    End Class
End Namespace");
        }

        [Fact]
        public void TestSealedClass()
        {
            TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    sealed class TestClass
    {
    }
}", @"Namespace Test.[class]
    Friend NotInheritable Class TestClass
    End Class
End Namespace");
        }

        [Fact]
        public void TestInterface()
        {
            TestConversionCSharpToVisualBasic(
                @"interface ITest : System.IDisposable
{
    void Test ();
}", @"Friend Interface ITest
    Inherits System.IDisposable

    Sub Test()
End Interface");
        }

        [Fact]
        public void TestInterfaceWithTwoMembers()
        {
            TestConversionCSharpToVisualBasic(
                @"interface ITest : System.IDisposable
{
    void Test ();
    void Test2 ();
}", @"Friend Interface ITest
    Inherits System.IDisposable

    Sub Test()
    Sub Test2()
End Interface");
        }

        [Fact]
        public void TestEnum()
        {
            TestConversionCSharpToVisualBasic(
    @"internal enum ExceptionResource
{
    Argument_ImplementIComparable,
    ArgumentOutOfRange_NeedNonNegNum,
    ArgumentOutOfRange_NeedNonNegNumRequired,
    Arg_ArrayPlusOffTooSmall
}", @"Friend Enum ExceptionResource
    Argument_ImplementIComparable
    ArgumentOutOfRange_NeedNonNegNum
    ArgumentOutOfRange_NeedNonNegNumRequired
    Arg_ArrayPlusOffTooSmall
End Enum");
        }

        [Fact]
        public void TestClassInheritanceList()
        {
            TestConversionCSharpToVisualBasic(
    @"abstract class ClassA : System.IDisposable
{
    protected abstract void Test();
}", @"Friend MustInherit Class ClassA
    Implements System.IDisposable

    Protected MustOverride Sub Test()
End Class");

            TestConversionCSharpToVisualBasic(
                @"abstract class ClassA : System.EventArgs, System.IDisposable
{
    protected abstract void Test();
}", @"Friend MustInherit Class ClassA
    Inherits System.EventArgs
    Implements System.IDisposable

    Protected MustOverride Sub Test()
End Class");
        }

        [Fact]
        public void TestStruct()
        {
            TestConversionCSharpToVisualBasic(
    @"struct MyType : System.IComparable<MyType>
{
    void Test() {}
}", @"Friend Structure MyType
    Implements System.IComparable(Of MyType)

    Private Sub Test()
    End Sub
End Structure");
        }

        [Fact]
        public void TestDelegate()
        {
            TestConversionCSharpToVisualBasic(
                @"public delegate void Test();",
                @"Public Delegate Sub Test()");
            TestConversionCSharpToVisualBasic(
                @"public delegate int Test();",
                @"Public Delegate Function Test() As Integer");
            TestConversionCSharpToVisualBasic(
                @"public delegate void Test(int x);",
                @"Public Delegate Sub Test(ByVal x As Integer)");
            TestConversionCSharpToVisualBasic(
                @"public delegate void Test(ref int x);",
                @"Public Delegate Sub Test(ByRef x As Integer)");
        }

        [Fact]
        public void MoveImportsStatement()
        {
            TestConversionCSharpToVisualBasic("namespace test { using SomeNamespace; }",
                        @"Imports SomeNamespace

Namespace test
End Namespace");
        }

        [Fact]
        public void ClassImplementsInterface()
        {
            TestConversionCSharpToVisualBasic(@"public class ToBeDisplayed : iDisplay
{
    public string Name { get; set; }

    public void DisplayName()
    {
    }
}

public interface iDisplay
{
    string Name { get; set; }
    void DisplayName();
}",
@"Public Class ToBeDisplayed
    Implements iDisplay

    Public Property Name As String Implements iDisplay.Name

    Public Sub DisplayName() Implements iDisplay.DisplayName
    End Sub
End Class

Public Interface iDisplay
    Property Name As String
    Sub DisplayName()
End Interface");
        }

        [Fact]
        public void ClassImplementsInterface2()
        {
            TestConversionCSharpToVisualBasic("class test : System.IComparable { }",
@"Friend Class test
    Implements System.IComparable
End Class");
        }

        [Fact]
        public void ClassInheritsClass()
        {
            TestConversionCSharpToVisualBasic("using System.IO; class test : InvalidDataException { }",
@"Imports System.IO

Friend Class test
    Inherits InvalidDataException
End Class");
        }

        [Fact]
        public void ClassInheritsClass2()
        {
            TestConversionCSharpToVisualBasic("class test : System.IO.InvalidDataException { }",
@"Friend Class test
    Inherits System.IO.InvalidDataException
End Class");
        }
    }
}
