using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class MemberTests : ConverterTestBase
    {
        [Fact]
        public void TestField()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Const answer As Integer = 42
    Private value As Integer = 10
    ReadOnly v As Integer = 15
End Class", @"class TestClass
{
    const int answer = 42;
    private int value = 10;
    private readonly int v = 15;
}");
        }

        [Fact]
        public void TestMethod()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
        Console.WriteLine(Enumerable.Empty(Of String))
    End Sub
End Class", @"using System;
using System.Linq;

class TestClass
{
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
        Console.WriteLine(Enumerable.Empty<string>());
    }
}");
        }

        [Fact]
        public void TestMethodXmlDoc()
        {
            TestConversionVisualBasicToCSharp(
                @"Class TestClass
    ''' <summary>Xml doc</summary>
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"class TestClass
{
    /// <summary>Xml doc</summary>
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}");
        }

        [Fact]
        public void TestMethodWithReturnType()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Function TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) As Integer
        Return 0
    End Function
End Class", @"class TestClass
{
    public int TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        return 0;
    }
}");
        }

        [Fact]
        public void TestStaticMethod()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Shared Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"class TestClass
{
    public static void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}");
        }

        [Fact]
        public void TestAbstractMethod()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public MustOverride Sub TestMethod()
End Class", @"class TestClass
{
    public abstract void TestMethod();
}");
        }

        [Fact]
        public void TestSealedMethod()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public NotOverridable Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"class TestClass
{
    public sealed void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default(T2);
        argument3 = default(T3);
    }
}");
        }

        [Fact]
        public void TestShadowedMethod()
        {
            TestConversionVisualBasicToCSharp(
                @"Class TestClass
    Public Sub TestMethod()
    End Sub

    Public Sub TestMethod(i as Integer)
    End Sub
End Class

Class TestSubclass
    Inherits TestClass

    Public Shadows Sub TestMethod()
        ' Not possible: TestMethod(3)
        System.Console.WriteLine(""New implementation"")
    End Sub
End Class", @"class TestClass
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
        // Not possible: TestMethod(3)
        System.Console.WriteLine(""New implementation"");
    }
}");
        }

        [Fact]
        public void TestExtensionMethod()
        {
            TestConversionVisualBasicToCSharp(
@"Module TestClass
    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub

    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod2Parameters(ByVal str As String, other As String)
    End Sub
End Module", @"static class TestClass
{
    public static void TestMethod(this string str)
    {
    }

    public static void TestMethod2Parameters(this string str, string other)
    {
    }
}");
        }

        [Fact]
        public void TestExtensionMethodWithExistingImport()
        {
            TestConversionVisualBasicToCSharp(
@"Imports System.Runtime.CompilerServices

Module TestClass
    <Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub
End Module", @"
static class TestClass
{
    public static void TestMethod(this string str)
    {
    }
}");
        }

        [Fact]
        public void TestProperty()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
@"Class TestClass
    Public Property Test As Integer

    Public Property Test2 As Integer
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
End Class", @"class TestClass
{
    public int Test { get; set; }

    public int Test2
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int Test3
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
}");
        }

        [Fact]
        public void TestConstructor()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class", @"class TestClass<T, T2, T3>
    where T : class, new()
    where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}");
        }

        [Fact]
        public void TestDestructor()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Protected Overrides Sub Finalize()
    End Sub
End Class", @"class TestClass
{
    ~TestClass()
    {
    }
}");
        }

        [Fact]
        public void TestEvent()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Event MyEvent As EventHandler
End Class", @"using System;

class TestClass
{
    public event EventHandler MyEvent;
}");
        }

        [Fact(Skip = "Not implemented!")]
        public void TestCustomEvent()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Private backingField As EventHandler

    Public Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler Me.backingField, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler Me.backingField, value
        End RemoveHandler
    End Event
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

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
}");
        }

        [Fact]
        public void SynthesizedBackingFieldAccess()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Shared Property First As Integer

    Private Second As Integer = _First
End Class", @"class TestClass
{
    private static int First { get; set; }

    private int Second = First;
}");
        }

        [Fact]
        public void PropertyInitializers()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private ReadOnly Property First As New List(Of String)
    Private Property Second As Integer = 0
End Class", @"using System.Collections.Generic;

class TestClass
{
    private List<string> First { get; } = new List<string>();
    private int Second { get; set; } = 0;
}");
        }

        [Fact]
        public void PartialFriendClassWithOverloads()
        {
            TestConversionVisualBasicToCSharp(
@"Partial Friend MustInherit Class TestClass1
    Public Shared Sub CreateStatic()
    End Sub

    Public Sub CreateInstance()
    End Sub

    Public MustOverride Sub CreateAbstractInstance()

    Public Overridable Sub CreateVirtualInstance()
    End Sub
End Class

Friend Class TestClass2
    Inherits TestClass1
    Public Overloads Shared Sub CreateStatic()
    End Sub

    Public Overloads Sub CreateInstance()
    End Sub

    Public Overrides Sub CreateAbstractInstance()
    End Sub

    Public Overrides Sub CreateVirtualInstance()
    End Sub
End Class", 
@"internal abstract partial class TestClass1
{
    public static void CreateStatic()
    {
    }

    public void CreateInstance()
    {
    }

    public abstract void CreateAbstractInstance();

    public virtual void CreateVirtualInstance()
    {
    }
}

internal class TestClass2 : TestClass1
{
    public new static void CreateStatic()
    {
    }

    public new void CreateInstance()
    {
    }

    public override void CreateAbstractInstance()
    {
    }

    public override void CreateVirtualInstance()
    {
    }
}");
        }

        [Fact]
        public void TestNarrowingWideningConversionOperator()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class MyInt
    Public Shared Narrowing Operator CType(i As Integer) As MyInt
        Return New MyInt()
    End Operator
    Public Shared Widening Operator CType(myInt As MyInt) As Integer
        Return 1
    End Operator
End Class"
                , @"public class MyInt
{
    public static explicit operator MyInt(int i)
    {
        return new MyInt();
    }
    public static implicit operator int(MyInt myInt)
    {
        return 1;
    }
}");
        }

        [Fact]
        public void OperatorOverloads()
        {
            // Note a couple map to the same thing in C# so occasionally the result won't compile. The user can manually decide what to do in such scenarios.
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class AcmeClass
    Public Shared Operator +(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator &(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator -(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Not(ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator *(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator /(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator \(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Mod(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <<(ac As AcmeClass, i As Integer) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator >>(ac As AcmeClass, i As Integer) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator =(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <>(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator >(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator <=(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator >=(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator And(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Or(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class", @"public class AcmeClass
{
    public static AcmeClass operator +(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator +(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator -(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator !(AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator *(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator /(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator /(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator %(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator <<(AcmeClass ac, int i)
    {
        return ac;
    }
    public static AcmeClass operator >>(AcmeClass ac, int i)
    {
        return ac;
    }
    public static AcmeClass operator ==(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator !=(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator <(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator >(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator <=(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator >=(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator &(string s, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator |(string s, AcmeClass ac)
    {
        return ac;
    }
}");
        }

        [Fact(Skip = "No obvious C# equivalent")]
        public void OperatorOverloadsWithNoCSharpEquivalent()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class AcmeClass
    Public Shared Operator ^(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Like(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class", @"public class AcmeClass" + /* not valid C# - to implement this you'd need to create a new method, and convert all callers to use it*/ @"
{
    public static AcmeClass operator ^(int i, AcmeClass ac)
    {
        return ac;
    }
    public static AcmeClass operator Like(string s, AcmeClass ac)
    {
        return ac;
    }
}");
        }

        [Fact]
        public void ClassWithGloballyQualifiedAttribute()
        {
            TestConversionVisualBasicToCSharp(@"<Global.System.Diagnostics.DebuggerDisplay(""Hello World"")>
Class TestClass
End Class", @"[global::System.Diagnostics.DebuggerDisplay(""Hello World"")]
class TestClass
{
}");
        }

        [Fact]
        public void FieldWithAttribute()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    <ThreadStatic>
    Private Shared First As Integer
End Class", @"using System;

class TestClass
{
    [ThreadStatic]
    private static int First;
}");
        }

        [Fact]
        public void ParamArray()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub SomeBools(ParamArray anyName As Boolean())
    End Sub
End Class", @"class TestClass
{
    private void SomeBools(params bool[] anyName)
    {
    }
}");
        }

        [Fact]
        public void ParamNamedBool()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub SomeBools(ParamArray bool As Boolean())
    End Sub
End Class", @"class TestClass
{
    private void SomeBools(params bool[] @bool)
    {
    }
}");
        }

        [Fact]
        public void MethodWithNameArrayParameter()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Sub DoNothing(ByVal strs() As String)
        Dim moreStrs() As String
    End Sub
End Class",
@"class TestClass
{
    public void DoNothing(string[] strs)
    {
        string[] moreStrs;
    }
}");
        }

        [Fact]
        public void UntypedParameters()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Sub DoNothing(obj, objs())
    End Sub
End Class",
@"class TestClass
{
    public void DoNothing(object obj, object[] objs)
    {
    }
}");
        }

        [Fact]
        public void NestedClass()
        {
            TestConversionVisualBasicToCSharp(@"Class ClA
    Public Shared Sub MA()
        ClA.ClassB.MB()
        MyClassC.MC()
    End Sub

    Public Class ClassB
        Public Shared Function MB() as ClassB
            ClA.MA()
            MyClassC.MC()
            Return ClA.ClassB.MB()
        End Function
    End Class
End Class

Class MyClassC
    Public Shared Sub MC()
        ClA.MA()
        ClA.ClassB.MB()
    End Sub
End Class", @"class ClA
{
    public static void MA()
    {
        ClA.ClassB.MB();
        MyClassC.MC();
    }

    public class ClassB
    {
        public static ClassB MB()
        {
            ClA.MA();
            MyClassC.MC();
            return ClA.ClassB.MB();
        }
    }
}

class MyClassC
{
    public static void MC()
    {
        ClA.MA();
        ClA.ClassB.MB();
    }
}");
        }

        [Fact(Skip = "Not implemented!")]
        public void TestIndexer()
        {
            TestConversionVisualBasicToCSharp(
@"Class TestClass
    private _Items As Integer()

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
End Class", @"class TestClass
{
    private int[] _items;

    public int this[int index]
    {
        get { return _items[index]; }
        set { _items[index] = value; }
    }

    public int this[string index]
    {
        get { return 0; }
    }

    private int m_test3;

    public int this[double index]
    {
        get { return this.m_test3; }
        set { this.m_test3 = value; }
    }
}");
        }
    }
}