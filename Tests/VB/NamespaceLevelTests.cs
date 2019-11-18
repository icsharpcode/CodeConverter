using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class NamespaceLevelTests : ConverterTestBase
    {
        [Fact]
        public async Task TestNamespace()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test
{

}", @"Namespace Test
End Namespace");
        }

        [Fact]
        public async Task TestTopLevelAttribute()
        {
            await TestConversionCSharpToVisualBasic(
                @"[assembly: CLSCompliant(true)]",
                @"
<Assembly: CLSCompliant(True)>");
        }

        [Fact]
        public async Task NamedImport()
        {
            await TestConversionCSharpToVisualBasic(
                @"using s = System.String;

public class X
{
    s GetStr()
    {
        return s.Empty;
    }
}",
                @"Imports s = System.String

Public Class X
    Private Function GetStr() As s
        Return s.Empty
    End Function
End Class");
        }

        [Fact]
        public async Task TestClass()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    class TestClass<T>
    {
    }
}", @"Namespace Test.class
    Friend Class TestClass(Of T)
    End Class
End Namespace");
        }

        [Fact]
        public async Task TestInternalStaticClass()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    internal static class TestClass
    {
        public static void Test() {}
        static void Test2() {}
    }
}", @"Namespace Test.class
    Friend Module TestClass
        Sub Test()
        End Sub

        Private Sub Test2()
        End Sub
    End Module
End Namespace");
        }

        [Fact]
        public async Task TestAbstractClass()
        {
            await TestConversionCSharpToVisualBasic(
@"namespace Test.@class
{
    public abstract class TestClass
    {
    }
}
namespace Test
{
    public class Test1 : @class.TestClass
    {
    }
}
",
@"Namespace Test.class
    Public MustInherit Class TestClass
    End Class
End Namespace

Namespace Test
    Public Class Test1
        Inherits [class].TestClass
    End Class
End Namespace");
        }

        [Fact]
        public async Task TestSealedClass()
        {
            await TestConversionCSharpToVisualBasic(@"namespace Test.@class
{
    sealed class TestClass
    {
    }
}", @"Namespace Test.class
    Friend NotInheritable Class TestClass
    End Class
End Namespace");
        }

        [Fact]
        public async Task TestInterface()
        {
            await TestConversionCSharpToVisualBasic(
                @"interface ITest : System.IDisposable
{
    void Test ();
}", @"Friend Interface ITest
    Inherits IDisposable

    Sub Test()
End Interface");
        }

        [Fact]
        public async Task TestInterfaceWithTwoMembers()
        {
            await TestConversionCSharpToVisualBasic(
                @"interface ITest : System.IDisposable
{
    void Test ();
    void Test2 ();
}", @"Friend Interface ITest
    Inherits IDisposable

    Sub Test()
    Sub Test2()
End Interface");
        }

        [Fact]
        public async Task TestEnum()
        {
            await TestConversionCSharpToVisualBasic(
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
        public async Task TestEnumWithExplicitBaseType()
        {
            await TestConversionCSharpToVisualBasic(
    @"public enum ExceptionResource : byte
{
    Argument_ImplementIComparable
}", @"Public Enum ExceptionResource As Byte
    Argument_ImplementIComparable
End Enum");
        }

        [Fact]
        public async Task TestClassInheritanceList()
        {
            await TestConversionCSharpToVisualBasic(
    @"abstract class ClassA : System.IDisposable
{
    protected abstract void Test();
}", @"Friend MustInherit Class ClassA
    Implements IDisposable

    Protected MustOverride Sub Test()
End Class");

            await TestConversionCSharpToVisualBasic(
                @"abstract class ClassA : System.EventArgs, System.IDisposable
{
    protected abstract void Test();
}", @"Friend MustInherit Class ClassA
    Inherits EventArgs
    Implements IDisposable

    Protected MustOverride Sub Test()
End Class");
        }

        [Fact]
        public async Task TestStruct()
        {
            await TestConversionCSharpToVisualBasic(
    @"struct MyType : System.IComparable<MyType>
{
    void Test() {}
}", @"Friend Structure MyType
    Implements IComparable(Of MyType)

    Private Sub Test()
    End Sub
End Structure");
        }

        [Fact]
        public async Task TestDelegate()
        {
            await TestConversionCSharpToVisualBasic(
                @"public delegate void Test();",
                @"Public Delegate Sub Test()");
            await TestConversionCSharpToVisualBasic(
                @"public delegate int Test();",
                @"Public Delegate Function Test() As Integer");
            await TestConversionCSharpToVisualBasic(
                @"public delegate void Test(int x);",
                @"Public Delegate Sub Test(ByVal x As Integer)");
            await TestConversionCSharpToVisualBasic(
                @"public delegate void Test(ref int x);",
                @"Public Delegate Sub Test(ByRef x As Integer)");
        }

        [Fact]
        public async Task MoveImportsStatement()
        {
            await TestConversionCSharpToVisualBasic("namespace test { using SomeNamespace; }",
                        @"Imports SomeNamespace

Namespace test
End Namespace");
        }

        [Fact]
        public async Task ClassImplementsInterface()
        {
            await TestConversionCSharpToVisualBasic(@"public class ToBeDisplayed : iDisplay
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
        public async Task ClassExplicitlyImplementsInterface()
        {
            await TestConversionCSharpToVisualBasic(
@"public class ToBeDisplayed : iDisplay
{
    string iDisplay.Name { get; set; }

    private void iDisplay.DisplayName()
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

    Private Property Name As String Implements iDisplay.Name

    Private Sub DisplayName() Implements iDisplay.DisplayName
    End Sub
End Class

Public Interface iDisplay
    Property Name As String
    Sub DisplayName()
End Interface");
        }
        [Fact]
        public async Task ClassImplementsInterface2()
        {
            await TestConversionCSharpToVisualBasic("class test : System.IComparable { }",
@"Friend Class test
    Implements IComparable
End Class");
        }

        [Fact]
        public async Task ClassInheritsClass()
        {
            await TestConversionCSharpToVisualBasic("using System.IO; class test : InvalidDataException { }",
@"Imports System.IO

Friend Class test
    Inherits InvalidDataException
End Class");
        }

        [Fact]
        public async Task ClassInheritsClass2()
        {
            await TestConversionCSharpToVisualBasic("class test : System.IO.InvalidDataException { }",
@"Friend Class test
    Inherits InvalidDataException
End Class");
        }
    }
}
