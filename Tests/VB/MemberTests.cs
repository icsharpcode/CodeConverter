using Xunit;

namespace CodeConverter.Tests.VB
{
    public class MemberTests : ConverterTestBase
    {
        [Fact]
        public void TestField()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    const int answer = 42;
    int value = 10;
    readonly int v = 15;
}", @"Friend Class TestClass
    Const answer As Integer = 42
    Private value As Integer = 10
    Private ReadOnly v As Integer = 15
End Class");
        }

        [Fact]
        public void TestMethod()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class");
        }

        [Fact]
        public void TestMethodWithReturnType()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public int TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        return 0;
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public Function TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) As Integer
        Return 0
    End Function
End Class");
        }

        [Fact]
        public void TestStaticMethod()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public static void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public Shared Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class");
        }

        [Fact]
        public void TestAbstractMethod()
        {
            TestConversionCSharpToVisualBasic(
                @"abstract class TestClass
{
    public abstract void TestMethod();
}", @"Friend MustInherit Class TestClass
    Public MustOverride Sub TestMethod()
End Class");
        }

        [Fact]
        public void TestNewMethodIsOverloadsNotShadows()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public void TestMethod()
    {
    }

    public void TestMethod(int i)
    {
    }
}

class TestSubclass : TestClass
{
    public new void TestMethod()
    {
        TestMethod(3);
        System.Console.WriteLine(""Shadowed implementation"");
    }
}", @"Friend Class TestClass
    Public Sub TestMethod()
    End Sub

    Public Sub TestMethod(ByVal i As Integer)
    End Sub
End Class

Friend Class TestSubclass
    Inherits TestClass

    Public Overloads Sub TestMethod()
        TestMethod(3)
        System.Console.WriteLine(""Shadowed implementation"")
    End Sub
End Class");
        }

        [Fact]
        public void TestSealedMethod()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public sealed void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class, new where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass
    Public NotOverridable Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class");
        }

        [Fact]
        public void TestExtensionMethod()
        {
            TestConversionCSharpToVisualBasic(
                @"static class TestClass
{
    public static void TestMethod(this String str)
    {
    }

    public static void TestMethod2Parameters(this String str, Action<string> _)
    {
    }
}", @"Imports System.Runtime.CompilerServices

Friend Module TestClass
    <Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub

    <Extension()>
    Sub TestMethod2Parameters(ByVal str As String, ByVal __ As Action(Of String))
    End Sub
End Module");
        }

        [Fact]
        public void TestExtensionMethodWithExistingImport()
        {
            TestConversionCSharpToVisualBasic(
                @"using System.Runtime.CompilerServices;

static class TestClass
{
    public static void TestMethod(this String str)
    {
    }
}", @"Imports System.Runtime.CompilerServices

Friend Module TestClass
    <Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub
End Module");
        }

        [Fact]
        public void TestProperty()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public int Test { get; set; }
    public int Test2 {
        get { return 0; }
    }
    int m_test3;
    public int Test3 {
        get { return this.m_test3; }
        set { this.m_test3 = value; }
    }
}", @"Friend Class TestClass
    Public Property Test As Integer

    Public ReadOnly Property Test2 As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Public Property Test3 As Integer
        Get
            Return Me.m_test3
        End Get
        Set(ByVal value As Integer)
            Me.m_test3 = value
        End Set
    End Property
End Class");
        }

        [Fact]
        public void TestPropertyWithExpressionBody()
        {
            TestConversionCSharpToVisualBasic(
                @"public class ConversionResult
{
    private string _sourcePathOrNull;
    
    public string SourcePathOrNull {
        get => _sourcePathOrNull;
        set => _sourcePathOrNull = string.IsNullOrWhiteSpace(value) ? null : value;
    }
}", @"Public Class ConversionResult
    Private _sourcePathOrNull As String

    Public Property SourcePathOrNull As String
        Get
            Return _sourcePathOrNull
        End Get
        Set(ByVal value As String)
            _sourcePathOrNull = If(String.IsNullOrWhiteSpace(value), Nothing, value)
        End Set
    End Property
End Class");
        }

        [Fact]
        public void TestOmmittedAccessorsReplacedWithExpressionBody()
        {
            TestConversionCSharpToVisualBasic(
                @"class MyFavColor  
{  
    private string[] favColor => new string[] {""Red"", ""Green""};
    public string this[int index] => favColor[index];
}  
", @"Friend Class MyFavColor
    Private ReadOnly Property favColor As String()
        Get
            Return New String() {""Red"", ""Green""}
        End Get
    End Property

    Default Public ReadOnly Property Item(ByVal index As Integer) As String
        Get
            Return favColor(index)
        End Get
    End Property
End Class");
        }

        [Fact]
        public void TestPropertyWithExpressionBodyThatCanBeStatement()
        {
            TestConversionCSharpToVisualBasic(
                @"public class ConversionResult
{
    private int _num;
    
    public string Num {
        set => _num++;
    }

    public string Blanket {
        set => throw new Exception();
    }
}", @"Public Class ConversionResult
    Private _num As Integer

    Public WriteOnly Property Num As String
        Set(ByVal value As String)
            _num += 1
        End Set
    End Property

    Public WriteOnly Property Blanket As String
        Set(ByVal value As String)
            Throw New Exception()
        End Set
    End Property
End Class");
        }

        [Fact]
        public void TestPropertyWithAttribute()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    int value { get; set; }
}", @"Friend Class TestClass
    <DatabaseGenerated(DatabaseGeneratedOption.None)>
    Private Property value As Integer
End Class
");
        }

        [Fact]
        public void TestConstructor()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass<T, T2, T3> where T : class, new where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}", @"Imports System.Runtime.InteropServices

Friend Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class");
        }

        [Fact]
        public void TestConstructorCallingBase()
        {
            TestConversionCSharpToVisualBasic(
                @"public class MyBaseClass
{
    public MyBaseClass(object o)
    {
    }
}

public sealed class MyClass 
 : MyBaseClass 
{
	 public MyClass(object o)
	  : base(o)
	{
	}
}", @"Public Class MyBaseClass
    Public Sub New(ByVal o As Object)
    End Sub
End Class

Public NotInheritable Class [MyClass]
    Inherits MyBaseClass

    Public Sub New(ByVal o As Object)
        MyBase.New(o)
    End Sub
End Class");
        }

        [Fact]
        public void TestDestructor()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    ~TestClass()
    {
    }
}", @"Friend Class TestClass
    Protected Overrides Sub Finalize()
    End Sub
End Class");
        }

        [Fact]
        public void TestEvent()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    public event EventHandler MyEvent;
}", @"Friend Class TestClass
    Public Event MyEvent As EventHandler
End Class");
        }

        [Fact]
        public void TestCustomEvent()
        {
            TestConversionCSharpToVisualBasic(
                @"using System;

class TestClass
{
    EventHandler backingField;

    public event EventHandler MyEvent {
        add {
            this.backingField += value;
        }
        remove {
            this.backingField -= value;
        }
    }
}", @"Imports System

Friend Class TestClass
    Private backingField As EventHandler

    Public Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler Me.backingField, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler Me.backingField, value
        End RemoveHandler
    End Event
End Class");
        }

        [Fact]
        public void TestIndexer()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    private int[] _Items;

    public int this[int index]
    {
        get
        {
            return _Items[index];
        }
        set
        {
            _Items[index] = value;
        }
    }

    public int this[string index]
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int this[double index]
    {
        get
        {
            return this.m_test3;
        }
        set
        {
            this.m_test3 = value;
        }
    }
}", @"Friend Class TestClass
    Private _Items As Integer()

    Default Public Property Item(ByVal index As Integer) As Integer
        Get
            Return _Items(index)
        End Get
        Set(ByVal value As Integer)
            _Items(index) = value
        End Set
    End Property

    Default Public ReadOnly Property Item(ByVal index As String) As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Default Public Property Item(ByVal index As Double) As Integer
        Get
            Return Me.m_test3
        End Get
        Set(ByVal value As Integer)
            Me.m_test3 = value
        End Set
    End Property
End Class");
        }
    }
}