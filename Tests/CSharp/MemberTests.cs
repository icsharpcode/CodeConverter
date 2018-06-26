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
        public void TestConstantFieldInModule()
        {
            TestConversionVisualBasicToCSharp(
@"Module TestModule
    Const answer As Integer = 42
End Module", @"static class TestModule
{
    const int answer = 42;
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
@"MustInherit Class TestClass
    Public MustOverride Sub TestMethod()
End Class", @"abstract class TestClass
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

        [Fact]
        public void TestModuleHandlesWithEvents()
        {
            // Too much auto-generated code to auto-test comments
            TestConversionVisualBasicToCSharpWithoutComments(
@"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Module Module1
    WithEvents EventClassInstance, EventClassInstance2 As New MyEventClass

    Sub PrintTestMessage2() Handles EventClassInstance.TestEvent, EventClassInstance2.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles EventClassInstance.TestEvent
    End Sub
End Module", @"using System.Runtime.CompilerServices;

class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

static class Module1
{
    static Module1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }

    private static MyEventClass _EventClassInstance, _EventClassInstance2;

    private static MyEventClass EventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _EventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_EventClassInstance != null)
            {
                _EventClassInstance.TestEvent -= PrintTestMessage2;
                _EventClassInstance.TestEvent -= PrintTestMessage3;
            }

            _EventClassInstance = value;
            if (_EventClassInstance != null)
            {
                _EventClassInstance.TestEvent += PrintTestMessage2;
                _EventClassInstance.TestEvent += PrintTestMessage3;
            }
        }
    }

    private static MyEventClass EventClassInstance2
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _EventClassInstance2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_EventClassInstance2 != null)
            {
                _EventClassInstance2.TestEvent -= PrintTestMessage2;
            }

            _EventClassInstance2 = value;
            if (_EventClassInstance2 != null)
            {
                _EventClassInstance2.TestEvent += PrintTestMessage2;
            }
        }
    }

    public static void PrintTestMessage2()
    {
    }

    public static void PrintTestMessage3()
    {
    }
}");
        }

        [Fact]
        public void TestWitheventsWithoutInitializer()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
@"Class MyEventClass
    Public Event TestEvent()
End Class
Class Class1
    WithEvents MyEventClassInstance As MyEventClass
    Sub EventClassInstance_TestEvent() Handles MyEventClassInstance.TestEvent
    End Sub
End Class", @"using System.Runtime.CompilerServices;

class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();
}

class Class1
{
    private MyEventClass _MyEventClassInstance;

    private MyEventClass MyEventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _MyEventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_MyEventClassInstance != null)
            {
                _MyEventClassInstance.TestEvent -= EventClassInstance_TestEvent;
            }

            _MyEventClassInstance = value;
            if (_MyEventClassInstance != null)
            {
                _MyEventClassInstance.TestEvent += EventClassInstance_TestEvent;
            }
        }
    }

    public void EventClassInstance_TestEvent()
    {
    }
}
");
        }

        [Fact]
        public void TestClassHandlesWithEvents()
        {
            // Too much auto-generated code to auto-test comments
            TestConversionVisualBasicToCSharpWithoutComments(
@"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Class Class1
    Shared WithEvents SharedEventClassInstance As New MyEventClass
    WithEvents NonSharedEventClassInstance As New MyEventClass

    Public Sub New()
    End Sub

    Public Sub New(num As Integer)
    End Sub

    Public Sub New(obj As Object)
        MyClass.New()
    End Sub

    Shared Sub PrintTestMessage2() Handles SharedEventClassInstance.TestEvent, NonSharedEventClassInstance.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles NonSharedEventClassInstance.TestEvent
    End Sub
End Class", @"using System.Runtime.CompilerServices;

class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

class Class1
{
    static Class1()
    {
        SharedEventClassInstance = new MyEventClass();
    }

    public Class1(int num)
    {
        NonSharedEventClassInstance = new MyEventClass();
    }

    public Class1()
    {
        NonSharedEventClassInstance = new MyEventClass();
    }
    private static MyEventClass _SharedEventClassInstance;

    private static MyEventClass SharedEventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _SharedEventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_SharedEventClassInstance != null)
            {
                _SharedEventClassInstance.TestEvent -= PrintTestMessage2;
            }

            _SharedEventClassInstance = value;
            if (_SharedEventClassInstance != null)
            {
                _SharedEventClassInstance.TestEvent += PrintTestMessage2;
            }
        }
    }

    private MyEventClass _NonSharedEventClassInstance;

    private MyEventClass NonSharedEventClassInstance
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _NonSharedEventClassInstance;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_NonSharedEventClassInstance != null)
            {
                _NonSharedEventClassInstance.TestEvent -= PrintTestMessage2;
                _NonSharedEventClassInstance.TestEvent -= PrintTestMessage3;
            }

            _NonSharedEventClassInstance = value;
            if (_NonSharedEventClassInstance != null)
            {
                _NonSharedEventClassInstance.TestEvent += PrintTestMessage2;
                _NonSharedEventClassInstance.TestEvent += PrintTestMessage3;
            }
        }
    }

    public Class1(object obj) : this()
    {
    }

    public static void PrintTestMessage2()
    {
    }

    public void PrintTestMessage3()
    {
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
        public void PartialClass()
        {
            // Can't auto test comments when there are already manual comments used
            TestConversionVisualBasicToCSharpWithoutComments(
@"Partial Class TestClass
    Private Sub DoNothing()
        Console.WriteLine(""Hello"")
    End Sub
End Class

Class TestClass ' VB doesn't require partial here (when just a single class omits it)
    Partial Private Sub DoNothing()
    End Sub
End Class",
@"using System;

partial class TestClass
{
    partial void DoNothing()
    {
        Console.WriteLine(""Hello"");
    }
}

partial class TestClass // VB doesn't require partial here (when just a single class omits it)
{
    partial void DoNothing();
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

        [Fact]
        public void TestIndexer()
        {   // BUG: Comments aren't properly transferred to the property statement because the line ends in a square bracket
            TestConversionVisualBasicToCSharpWithoutComments(
@"Class TestClass
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
End Class", @"class TestClass
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
}");
        }
    }
}