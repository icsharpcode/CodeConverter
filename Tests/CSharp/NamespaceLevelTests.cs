using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class NamespaceLevelTests : ConverterTestBase
    {
        [Fact]
        public void TestNamespace()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Test
End Namespace", @"namespace Test
{
}");
        }

        [Fact]
        public void TestLongNamespace()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Test1.Test2.Test3
End Namespace", @"namespace Test1.Test2.Test3
{
}");
        }

        [Fact]
        public void TestGlobalNamespace()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Global.Test
End Namespace", @"namespace Test
{
}");
        }

        [Fact]
        public void TestTopLevelAttribute()
        {
            TestConversionVisualBasicToCSharp(
                @"<Assembly: CLSCompliant(True)>",
                @"using System;

[assembly: CLSCompliant(true)]");
        }

        [Fact]
        public void AliasedImports()
        {
            TestConversionVisualBasicToCSharp(
                @"Imports tr = System.IO.TextReader

Public Class Test
    Private aliased As tr
End Class",
                @"using tr = System.IO.TextReader;

public class Test
{
    private tr aliased;
}");
        }

        [Fact]
        public void UnaliasedImports()
        {
            TestConversionVisualBasicToCSharp(
                @"Imports UnrecognizedNamespace",
                @"using UnrecognizedNamespace;");
        }

        [Fact]
        public void TestClass()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    Class TestClass(Of T)
    End Class
End Namespace", @"namespace Test.@class
{
    class TestClass<T>
    {
    }
}");
        }

        [Fact]
        public void TestInternalStaticClass()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    Friend Module TestClass
        Sub Test()
        End Sub

        Private Sub Test2()
        End Sub
    End Module
End Namespace", @"namespace Test.@class
{
    internal static class TestClass
    {
        public static void Test()
        {
        }

        private static void Test2()
        {
        }
    }
}");
        }

        [Fact]
        public void TestAbstractClass()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    MustInherit Class TestClass
    End Class
End Namespace", @"namespace Test.@class
{
    abstract class TestClass
    {
    }
}");
        }

        [Fact]
        public void TestSealedClass()
        {
            TestConversionVisualBasicToCSharp(@"Namespace Test.[class]
    NotInheritable Class TestClass
    End Class
End Namespace", @"namespace Test.@class
{
    sealed class TestClass
    {
    }
}");
        }

        [Fact]
        public void TestInterface()
        {
            TestConversionVisualBasicToCSharp(
@"Interface ITest
    Inherits System.IDisposable

    Sub Test()
End Interface", @"interface ITest : System.IDisposable
{
    void Test();
}");
        }

        [Fact]
        public void TestEnum()
        {
            TestConversionVisualBasicToCSharp(
@"Friend Enum ExceptionResource
    Argument_ImplementIComparable
    ArgumentOutOfRange_NeedNonNegNum
    ArgumentOutOfRange_NeedNonNegNumRequired
    Arg_ArrayPlusOffTooSmall
End Enum", @"internal enum ExceptionResource
{
    Argument_ImplementIComparable,
    ArgumentOutOfRange_NeedNonNegNum,
    ArgumentOutOfRange_NeedNonNegNumRequired,
    Arg_ArrayPlusOffTooSmall
}");
        }

        [Fact]
        public void TestClassInheritanceList()
        {
            TestConversionVisualBasicToCSharp(
@"MustInherit Class ClassA
    Implements System.IDisposable

    Protected MustOverride Sub Test()
End Class", @"abstract class ClassA : System.IDisposable
{
    protected abstract void Test();
}");

            TestConversionVisualBasicToCSharp(
@"MustInherit Class ClassA
    Inherits System.EventArgs
    Implements System.IDisposable

    Protected MustOverride Sub Test()
End Class", @"abstract class ClassA : System.EventArgs, System.IDisposable
{
    protected abstract void Test();
}");
        }

        [Fact]
        public void TestStruct()
        {
            TestConversionVisualBasicToCSharp(
@"Structure MyType
    Implements System.IComparable(Of MyType)

    Private Sub Test()
    End Sub
End Structure", @"struct MyType : System.IComparable<MyType>
{
    private void Test()
    {
    }
}");
        }

        [Fact]
        public void TestDelegate()
        {
            TestConversionVisualBasicToCSharp(
                @"Public Delegate Sub Test()",
                @"public delegate void Test();");
            TestConversionVisualBasicToCSharp(
                @"Public Delegate Function Test() As Integer",
                @"public delegate int Test();");
            TestConversionVisualBasicToCSharp(
                @"Public Delegate Sub Test(ByVal x As Integer)",
                @"public delegate void Test(int x);");
            TestConversionVisualBasicToCSharp(
                @"Public Delegate Sub Test(ByRef x As Integer)",
                @"public delegate void Test(ref int x);");
        }

        [Fact]
        public void ClassImplementsInterface()
        {
            TestConversionVisualBasicToCSharp(@"Class test
    Implements IComparable
End Class",
                @"using System;

class test : IComparable
{
}");
        }

        [Fact]
        public void ClassImplementsInterface2()
        {
            TestConversionVisualBasicToCSharp(@"Class test
    Implements System.IComparable
End Class",
                @"class test : System.IComparable
{
}");
        }

        [Fact]
        public void ClassInheritsClass()
        {
            TestConversionVisualBasicToCSharp(@"Imports System.IO

Class test
    Inherits InvalidDataException
End Class",
                @"using System.IO;

class test : InvalidDataException
{
}");
        }

        [Fact]
        public void ClassInheritsClass2()
        {
            TestConversionVisualBasicToCSharp(@"Class test
    Inherits System.IO.InvalidDataException
End Class",
                @"class test : System.IO.InvalidDataException
{
}");
        }

        [Fact]
        public void MultilineDocComment()
        {
            TestConversionVisualBasicToCSharp(@"Public Class MyTestClass
    ''' <summary>
    ''' Returns empty
    ''' </summary>
    Private Function MyFunc() As String
        Return """"
    End Function
End Class",
                @"public class MyTestClass
{
    /// <summary>
    /// Returns empty
    /// </summary>
    private string MyFunc()
    {
        return """";
    }
}");
        }
    }
}
