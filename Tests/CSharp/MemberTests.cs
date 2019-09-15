using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class MemberTests : ConverterTestBase
    {
        [Fact]
        public async Task TestField()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Const answer As Integer = 42
    Private value As Integer = 10
    ReadOnly v As Integer = 15
End Class", @"internal partial class TestClass
{
    private const int answer = 42;
    private int value = 10;
    private readonly int v = 15;
}");
        }

        [Fact]
        public async Task TestConstantFieldInModule()
        {
            await TestConversionVisualBasicToCSharp(
@"Module TestModule
    Const answer As Integer = 42
End Module", @"internal partial static class TestModule
{
    private const int answer = 42;
}");
        }

        [Fact]
        public async Task TestModuleConstructor()
        {
            await TestConversionVisualBasicToCSharp(
@"Module Module1
    Sub New()
        Dim someValue As Integer = 0
    End Sub
End Module", @"internal partial static class Module1
{
    static Module1()
    {
        int someValue = 0;
    }
}");
        }

        [Fact]
        public async Task TestTypeInferredConst()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Const someConstField = 42
    Sub TestMethod()
        Const someConst = System.DateTimeKind.Local
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private const int someConstField = 42;
    public void TestMethod()
    {
        const DateTimeKind someConst = DateTimeKind.Local;
    }
}");
        }

        [Fact]
        public async Task TestTypeInferredVar()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Enum TestEnum As Integer
        Test1
    End Enum

    Dim EnumVariable = TestEnum.Test1
    Public Sub AMethod()
        Dim t1 As Integer = EnumVariable
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

internal partial class TestClass
{
    public enum TestEnum : int
    {
        Test1
    }

    private object EnumVariable = TestEnum.Test1;" /* VB doesn't infer the type like you'd think, it just uses object */ + @"
    public void AMethod()
    {
        int t1 = Conversions.ToInteger(EnumVariable);" /* VB compiler uses Conversions rather than any plainer casting */ + @"
    }
}");
        }

        [Fact]
        public async Task TestMethod()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
        Console.WriteLine(Enumerable.Empty(Of String))
    End Sub
End Class", @"using System;
using System.Linq;

internal partial class TestClass
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
        public async Task TestMethodAssignmentReturn()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class Class1
    Function TestMethod(x As Integer) As Integer
        If x = 1 Then
            TestMethod = 1
        ElseIf x = 2 Then
            TestMethod = 2
            Exit Function
        ElseIf x = 3 Then
            TestMethod = TestMethod(1)
        End If
    End Function
End Class", @"internal partial class Class1
{
    public int TestMethod(int x)
    {
        int TestMethodRet = default(int);
        if (x == 1)
            TestMethodRet = 1;
        else if (x == 2)
        {
            TestMethodRet = 2;
            return TestMethodRet;
        }
        else if (x == 3)
            TestMethodRet = TestMethod(1);
        return TestMethodRet;
    }
}");
        }

        [Fact]
        public async Task TestPropertyAssignmentReturn()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Class Class1
    Public ReadOnly Property Foo() As String
        Get
            Foo = """"
        End Get
    End Property
    Public ReadOnly Property X As String
        Get
            X = 4
            X = X * 2
            Dim y = ""random variable to check it isn't just using the value of the last statement""
        End Get
    End Property
    Public _y As String
    Public WriteOnly Property Y As String
        Set(value As String)
            If value <> """" Then
                Y = """"
            Else
                _y = """"
            End If
        End Set
    End Property
End Class", @"using Microsoft.VisualBasic.CompilerServices;

public partial class Class1
{
    public string Foo
    {
        get
        {
            string FooRet = default(string);
            FooRet = """";
            return FooRet;
        }
    }
    public string X
    {
        get
        {
            string XRet = default(string);
            XRet = Conversions.ToString(4);
            XRet = Conversions.ToString(Conversions.ToDouble(XRet) * 2);
            string y = ""random variable to check it isn't just using the value of the last statement"";
            return XRet;
        }
    }
    public string _y;
    public string Y
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
                Y = """";
            else
                _y = """";
        }
    }
}");
        }

        [Fact]
        public async Task TestMethodAssignmentReturn293()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Class Class1
    Public Event MyEvent As EventHandler
    Protected Overrides Function Foo() As String
        AddHandler MyEvent, AddressOf Foo
        Foo = Foo & """"
        Foo += NameOf(Foo)
    End Function
End Class", @"using System;

public partial class Class1
{
    public event EventHandler MyEvent;
    protected override string Foo()
    {
        string FooRet = default(string);
        MyEvent += Foo;
        FooRet = FooRet + """";
        FooRet += nameof(Foo);
        return FooRet;
    }
}");
        }

        [Fact]
        public async Task TestMethodAssignmentAdditionReturn()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class Class1
    Function TestMethod(x As Integer) As Integer
        If x = 1 Then
            TestMethod += 1
        ElseIf x = 2 Then
            TestMethod -= 2
            Exit Function
        ElseIf x = 3 Then
            TestMethod *= TestMethod(1)
        End If
    End Function
End Class", @"internal partial class Class1
{
    public int TestMethod(int x)
    {
        int TestMethodRet = default(int);
        if (x == 1)
            TestMethodRet += 1;
        else if (x == 2)
        {
            TestMethodRet -= 2;
            return TestMethodRet;
        }
        else if (x == 3)
            TestMethodRet *= TestMethod(1);
        return TestMethodRet;
    }
}");
        }

        [Fact]
        public async Task TestMethodMissingReturn()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class Class1
    Function TestMethod() As Integer

    End Function
End Class", @"internal partial class Class1
{
    public int TestMethod()
    {
        return default(int);
    }
}");
        }

        [Fact]
        public async Task TestGetIteratorDoesNotGainReturn()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Class VisualBasicClass
  Public Shared ReadOnly Iterator Property SomeObjects As IEnumerable(Of Object())
    Get
      Yield New Object(2) {}
      Yield New Object(2) {}
    End Get
  End Property
End Class", @"using System.Collections.Generic;

public partial class VisualBasicClass
{
    public static IEnumerable<object[]> SomeObjects
    {
        get
        {
            yield return new object[3];
            yield return new object[3];
        }
    }
}");
        }

        [Fact]
        public async Task TestMethodXmlDoc()
        {
            await TestConversionVisualBasicToCSharp(
                @"Class TestClass
    ''' <summary>Xml doc</summary>
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"internal partial class TestClass
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
        public async Task TestMethodWithOutParameter()
        {
            await TestConversionVisualBasicToCSharp(
                @"Class TestClass
    Public Function TryGet(<System.Runtime.InteropServices.Out> ByRef strs As List(Of String)) As Boolean
        strs = New List(Of String)
        Return False
    End Function
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    public bool TryGet(out List<string> strs)
    {
        strs = new List<string>();
        return false;
    }
}");
        }

        [Fact]
        public async Task TestMethodWithReturnType()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Function TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) As Integer
        Return 0
    End Function
End Class", @"internal partial class TestClass
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
        public async Task TestFunctionWithNoReturnTypeSpecified()
        {
            // Note: "Inferred" type is always object except with local variables
            // https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/variables/local-type-inference
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Private Function TurnFirstToUp(ByVal Text As String)
        Dim firstCharacter = Text.Substring(0, 1).ToUpper()
        Return firstCharacter + Text.Substring(1)
    End Function
End Class", @"internal partial class TestClass
{
    private object TurnFirstToUp(string Text)
    {
        string firstCharacter = Text.Substring(0, 1).ToUpper();
        return firstCharacter + Text.Substring(1);
    }
}");
        }

        [Fact]
        public async Task TestStaticMethod()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Shared Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"internal partial class TestClass
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
        public async Task TestAbstractMethod()
        {
            await TestConversionVisualBasicToCSharp(
@"MustInherit Class TestClass
    Public MustOverride Sub TestMethod()
End Class", @"internal abstract partial class TestClass
{
    public abstract void TestMethod();
}");
        }

        [Fact]
        public async Task TestSealedMethod()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public NotOverridable Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"internal partial class TestClass
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
        public async Task TestShadowedMethod()
        {
            await TestConversionVisualBasicToCSharp(
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
End Class", @"using System;

internal partial class TestClass
{
    public void TestMethod()
    {
    }

    public void TestMethod(int i)
    {
    }
}

internal partial class TestSubclass : TestClass
{
    public new void TestMethod()
    {
        // Not possible: TestMethod(3)
        Console.WriteLine(""New implementation"");
    }
}");
        }

        [Fact]
        public async Task TestExtensionMethod()
        {
            await TestConversionVisualBasicToCSharp(
@"Module TestClass
    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub

    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod2Parameters(ByVal str As String, other As String)
    End Sub
End Module", @"internal partial static class TestClass
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
        public async Task TestExtensionMethodWithExistingImport()
        {
            await TestConversionVisualBasicToCSharp(
@"Imports System.Runtime.CompilerServices

Module TestClass
    <Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub
End Module", @"internal partial static class TestClass
{
    public static void TestMethod(this string str)
    {
    }
}");
        }

        [Fact]
        public async Task TestProperty()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
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
End Class", @"internal partial class TestClass
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
            return m_test3;
        }
        set
        {
            m_test3 = value;
        }
    }
}");
        }

        [Fact]
        public async Task TestParameterizedProperty()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class TestClass
    Public Property FirstName As String
    Public Property LastName As String
    
    Public Property FullName(ByVal lastNameFirst As Boolean, ByVal isFirst As Boolean) As String
        Get
            If lastNameFirst Then
                Return LastName & "" "" & FirstName
            Else
                Return FirstName & "" "" & LastName
            End If
        End Get

        Friend Set
            If isFirst Then FirstName = Value
        End Set
    End Property

    Public Overrides Function ToString() As String
        FullName(False, True) = ""hello""
        Return FullName(False, True)
    End Function
End Class", @"internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool lastNameFirst, bool isFirst)
    {
        if (lastNameFirst)
            return LastName + "" "" + FirstName;
        else
            return FirstName + "" "" + LastName;
    }

    internal void set_FullName(bool lastNameFirst, bool isFirst, string value)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(false, true, ""hello"");
        return get_FullName(false, true);
    }
}");
        }

        [Fact]
        public async Task TestParameterizedPropertyAndGenericInvocationAndEnumEdgeCases()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Class ParameterizedPropertiesAndEnumTest
    Public Enum MyEnum
        First
    End Enum
    
    Public Property MyProp(ByVal blah As Integer) As String
        Get
            Return blah
        End Get
        Set
        End Set
    End Property
            

    Public Sub ReturnWhatever(ByVal m As MyEnum)
        Dim enumerableThing = Enumerable.Empty(Of String)
        Select Case m
            Case -1
                Exit Sub
            Case MyEnum.First
                Exit Sub
            Case 3
                Me.MyProp(4) = enumerableThing.ToArray()(m)
                Exit Sub
        End Select
    End Sub
End Class", @"using System.Linq;

public partial class ParameterizedPropertiesAndEnumTest
{
    public enum MyEnum
    {
        First
    }

    public string get_MyProp(int blah)
    {
        return blah;
    }

    public void set_MyProp(int blah, string value)
    {
    }


    public void ReturnWhatever(MyEnum m)
    {
        var enumerableThing = Enumerable.Empty<string>();
        switch (m)
        {
            case (MyEnum)(-1
           ):
                {
                    return;
                }

            case MyEnum.First:
                {
                    return;
                }

            case (MyEnum)3:
                {
                    set_MyProp(4, enumerableThing.ToArray()[(int)m]);
                    return;
                }
        }
    }
}");
        }

        [Fact]
        public async Task TestReadWriteOnlyInterfaceProperty()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Interface Foo
    ReadOnly Property P1() As String
    WriteOnly Property P2() As String
End Interface", @"public partial interface Foo
{
    string P1 { get; }
    string P2 { set; }
}");
        }

        [Fact]
        public async Task TestConstructor()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class", @"internal partial class TestClass<T, T2, T3>
    where T : class, new()
    where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}");
        }

        [Fact]
        public async Task TestConstructorWithImplicitPublicAccessibility()
        {
            await TestConversionVisualBasicToCSharp(
@"Sub New()
End Sub", @"SurroundingClass()
{
}");
        }

        [Fact]
        public async Task TestStaticConstructor()
        {
            await TestConversionVisualBasicToCSharp(
@"Shared Sub New()
End Sub", @"static SurroundingClass()
{
}");
        }

        [Fact]
        public async Task TestDestructor()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Protected Overrides Sub Finalize()
    End Sub
End Class", @"internal partial class TestClass
{
    ~TestClass()
    {
    }
}");
        }

        [Fact]
        public async Task TestEvent()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Event MyEvent As EventHandler
End Class", @"using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;
}");
        }

        [Fact]
        public async Task TestModuleHandlesWithEvents()
        {
            // Too much auto-generated code to auto-test comments
            await TestConversionVisualBasicToCSharpWithoutComments(
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

internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

internal partial static class Module1
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
        public async Task TestWithEventsWithoutInitializer()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class MyEventClass
    Public Event TestEvent()
End Class
Class Class1
    WithEvents MyEventClassInstance As MyEventClass
    Sub EventClassInstance_TestEvent() Handles MyEventClassInstance.TestEvent
    End Sub
End Class", @"using System.Runtime.CompilerServices;

internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();
}

internal partial class Class1
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
        public async Task TestClassHandlesWithEvents()
        {
            // Too much auto-generated code to auto-test comments
            await TestConversionVisualBasicToCSharpWithoutComments(
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

internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

internal partial class Class1
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
        public async Task TestPartialClassHandlesWithEvents()
        {
            // Too much auto-generated code to auto-test comments
            await TestConversionVisualBasicToCSharpWithoutComments(
                @"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Partial Class Class1
    WithEvents EventClassInstance, EventClassInstance2 As New MyEventClass

    Public Sub New()
    End Sub

    Public Sub New(num As Integer)
    End Sub

    Public Sub New(obj As Object)
        MyClass.New()
    End Sub
End Class

Public Partial Class Class1
    Sub PrintTestMessage2() Handles EventClassInstance.TestEvent, EventClassInstance2.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles EventClassInstance.TestEvent
    End Sub
End Class", @"using System.Runtime.CompilerServices;

internal partial class MyEventClass
{
    public event TestEventEventHandler TestEvent;

    public delegate void TestEventEventHandler();

    public void RaiseEvents()
    {
        TestEvent?.Invoke();
    }
}

public partial class Class1
{
    public Class1(int num)
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }

    public Class1()
    {
        EventClassInstance = new MyEventClass();
        EventClassInstance2 = new MyEventClass();
    }
    private MyEventClass _EventClassInstance, _EventClassInstance2;

    private MyEventClass EventClassInstance
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

    private MyEventClass EventClassInstance2
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

    public Class1(object obj) : this()
    {
    }
}

public partial class Class1
{
    public void PrintTestMessage2()
    {
    }

    public void PrintTestMessage3()
    {
    }
}");
        }

        [Fact]
        public async Task SynthesizedBackingFieldAccess()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Shared Property First As Integer

    Private Second As Integer = _First
End Class", @"internal partial class TestClass
{
    private static int First { get; set; }

    private int Second = First;
}");
        }

        [Fact]
        public async Task PropertyInitializers()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private ReadOnly Property First As New List(Of String)
    Private Property Second As Integer = 0
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private List<string> First { get; private set; } = new List<string>();
    private int Second { get; set; } = 0;
}");
        }

        [Fact]
        public async Task PartialFriendClassWithOverloads()
        {
            await TestConversionVisualBasicToCSharp(
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

internal partial class TestClass2 : TestClass1
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
        public async Task TestNarrowingWideningConversionOperator()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class MyInt
    Public Shared Narrowing Operator CType(i As Integer) As MyInt
        Return New MyInt()
    End Operator
    Public Shared Widening Operator CType(myInt As MyInt) As Integer
        Return 1
    End Operator
End Class"
                , @"public partial class MyInt
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
        public async Task OperatorOverloads()
        {
            // Note a couple map to the same thing in C# so occasionally the result won't compile. The user can manually decide what to do in such scenarios.
            await TestConversionVisualBasicToCSharpWithoutComments(@"Public Class AcmeClass
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
End Class", @"public partial class AcmeClass
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

        [Fact]// The stack trace displayed will change from time to time. Feel free to update this characterization test appropriately.
        public async Task OperatorOverloadsWithNoCSharpEquivalentShowErrorInlineCharacterization()
        {
            // No valid conversion to C# - to implement this you'd need to create a new method, and convert all callers to use it.
            var convertedCode = await GetConvertedCodeOrErrorString<VBToCSConversion>(@"Public Class AcmeClass
    Public Shared Operator ^(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Like(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class");

            Assert.Contains("Cannot convert", convertedCode);
            Assert.Contains("#error", convertedCode);
            Assert.Contains("_failedMemberConversionMarker1", convertedCode);
            Assert.Contains("Public Shared Operator ^(i As Integer, ac As AcmeClass) As AcmeClass", convertedCode);
            Assert.Contains("_failedMemberConversionMarker2", convertedCode);
            Assert.Contains("Public Shared Operator Like(s As String, ac As AcmeClass) As AcmeClass", convertedCode);
        }

        [Fact]
        public async Task ClassWithGloballyQualifiedAttribute()
        {
            await TestConversionVisualBasicToCSharp(@"<Global.System.Diagnostics.DebuggerDisplay(""Hello World"")>
Class TestClass
End Class", @"using System.Diagnostics;

[DebuggerDisplay(""Hello World"")]
internal partial class TestClass
{
}");
        }

        [Fact]
        public async Task FieldWithAttribute()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    <ThreadStatic>
    Private Shared First As Integer
End Class", @"using System;

internal partial class TestClass
{
    [ThreadStatic]
    private static int First;
}");
        }

        [Fact]
        public async Task ParamArray()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub SomeBools(ParamArray anyName As Boolean())
    End Sub
End Class", @"internal partial class TestClass
{
    private void SomeBools(params bool[] anyName)
    {
    }
}");
        }

        [Fact]
        public async Task ParamNamedBool()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub SomeBools(ParamArray bool As Boolean())
    End Sub
End Class", @"internal partial class TestClass
{
    private void SomeBools(params bool[] @bool)
    {
    }
}");
        }

        [Fact]
        public async Task MethodWithNameArrayParameter()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Sub DoNothing(ByVal strs() As String)
        Dim moreStrs() As String
    End Sub
End Class",
@"internal partial class TestClass
{
    public void DoNothing(string[] strs)
    {
        string[] moreStrs;
    }
}");
        }

        [Fact]
        public async Task UntypedParameters()
        {
            await TestConversionVisualBasicToCSharp(
@"Class TestClass
    Public Sub DoNothing(obj, objs())
    End Sub
End Class",
@"internal partial class TestClass
{
    public void DoNothing(object obj, object[] objs)
    {
    }
}");
        }

        [Fact]
        public async Task PartialClass()
        {
            // Can't auto test comments when there are already manual comments used
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Public Partial Class TestClass
    Private Sub DoNothing()
        Console.WriteLine(""Hello"")
    End Sub
End Class

Class TestClass ' VB doesn't require partial here (when just a single class omits it)
    Partial Private Sub DoNothing()
    End Sub
End Class",
@"using System;

public partial class TestClass
{
    partial void DoNothing()
    {
        Console.WriteLine(""Hello"");
    }
}

public partial class TestClass // VB doesn't require partial here (when just a single class omits it)
{
    partial void DoNothing();
}");
        }

        [Fact]
        public async Task NestedClass()
        {
            await TestConversionVisualBasicToCSharp(@"Class ClA
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
End Class", @"internal partial class ClA
{
    public static void MA()
    {
        ClassB.MB();
        MyClassC.MC();
    }

    public partial class ClassB
    {
        public static ClassB MB()
        {
            MA();
            MyClassC.MC();
            return MB();
        }
    }
}

internal partial class MyClassC
{
    public static void MC()
    {
        ClA.MA();
        ClA.ClassB.MB();
    }
}");
        }

        [Fact]
        public async Task LessQualifiedNestedClass()
        {
            await TestConversionVisualBasicToCSharp(@"Class ClA
    Public Shared Sub MA()
        ClassB.MB()
        MyClassC.MC()
    End Sub

    Public Class ClassB
        Public Shared Function MB() as ClassB
            MA()
            MyClassC.MC()
            Return MB()
        End Function
    End Class
End Class

Class MyClassC
    Public Shared Sub MC()
        ClA.MA()
        ClA.ClassB.MB()
    End Sub
End Class", @"internal partial class ClA
{
    public static void MA()
    {
        ClassB.MB();
        MyClassC.MC();
    }

    public partial class ClassB
    {
        public static ClassB MB()
        {
            MA();
            MyClassC.MC();
            return MB();
        }
    }
}

internal partial class MyClassC
{
    public static void MC()
    {
        ClA.MA();
        ClA.ClassB.MB();
    }
}");
        }

        [Fact]
        public async Task TestIndexer()
        {   // BUG: Comments aren't properly transferred to the property statement because the line ends in a square bracket
            await TestConversionVisualBasicToCSharpWithoutComments(
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
End Class", @"internal partial class TestClass
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
            return m_test3;
        }
        set
        {
            m_test3 = value;
        }
    }
}");
        }

        [Fact]
        public async Task TestWriteOnlyProperties()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Interface TestInterface
    WriteOnly Property Items As Integer()
End Interface", @"internal partial interface TestInterface
{
    int[] Items { set; }
}");
        }

        [Fact]
        public async Task TestImplicitPrivateSetter()
        {
            await TestConversionVisualBasicToCSharp(
@"Public Class SomeClass
    Public ReadOnly Property SomeValue As Integer

    Public Sub SetValue(value1 As Integer, value2 As Integer)
        _SomeValue = value1 + value2
    End Sub
End Class", @"public partial class SomeClass
{
    public int SomeValue { get; private set; }

    public void SetValue(int value1, int value2)
    {
        SomeValue = value1 + value2;
    }
}");
        }

        [Fact]
        public async Task TestSetWithNamedParameterProperties()
        {
            await TestConversionVisualBasicToCSharpWithoutComments(
@"Class TestClass
    Private _Items As Integer()
    Property Items As Integer()
        Get
            Return _Items
        End Get
        Set(v As Integer())
            _Items = v
        End Set
    End Property
End Class", @"internal partial class TestClass
{
    private int[] _Items;
    public int[] Items
    {
        get
        {
            return _Items;
        }
        set
        {
            _Items = value;
        }
    }
}");
        }
    }
}