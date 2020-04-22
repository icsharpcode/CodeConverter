using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    public class MemberTests : ConverterTestBase
    {
        [Fact]
        public async Task TestFieldAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Const answer As Integer = 42
    Private value As Integer = 10
    ReadOnly v As Integer = 15
End Class", @"
internal partial class TestClass
{
    private const int answer = 42;
    private int value = 10;
    private readonly int v = 15;
}");
        }

        [Fact]
        public async Task TestMultiArrayFieldAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Dim Parts(), Taxes(), Deposits()(), Prepaid()(), FromDate, ToDate As String
End Class", @"
internal partial class TestClass
{
    private string[] Parts, Taxes;
    private string[][] Deposits, Prepaid;
    private string FromDate, ToDate;
}");
        }

        [Fact]
        public async Task TestConstantFieldInModuleAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Module TestModule
    Const answer As Integer = 42
End Module", @"
internal static partial class TestModule
{
    private const int answer = 42;
}");
        }

        [Fact]
        public async Task TestConstructorVisibilityAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class Class1
    Sub New(x As Boolean)
    End Sub
End Class", @"
internal partial class Class1
{
    public Class1(bool x)
    {
    }
}");
        }

        [Fact]
        public async Task TestModuleConstructorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Module Module1
    Sub New()
        Dim someValue As Integer = 0
    End Sub
End Module", @"
internal static partial class Module1
{
    static Module1()
    {
        int someValue = 0;
    }
}");
        }

        [Fact]
        public async Task TestTypeInferredConstAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        public async Task TestTypeInferredVarAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        public async Task TestMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        argument2 = default;
        argument3 = default;
        Console.WriteLine(Enumerable.Empty<string>());
    }
}");
        }

        [Fact]
        public async Task TestMethodAssignmentReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
End Class", @"
internal partial class Class1
{
    public int TestMethod(int x)
    {
        int TestMethodRet = default;
        if (x == 1)
        {
            TestMethodRet = 1;
        }
        else if (x == 2)
        {
            TestMethodRet = 2;
            return TestMethodRet;
        }
        else if (x == 3)
        {
            TestMethodRet = TestMethod(1);
        }

        return TestMethodRet;
    }
}");
        }

        [Fact]
        public async Task TestPropertyAssignmentReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
            string FooRet = default;
            FooRet = """";
            return FooRet;
        }
    }

    public string X
    {
        get
        {
            string XRet = default;
            XRet = 4.ToString();
            XRet = (Conversions.ToDouble(XRet) * 2).ToString();
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
            {
                Y = """";
            }
            else
            {
                _y = """";
            }
        }
    }
}
1 target compilation errors:
CS0103: The name 'string' does not exist in the current context");
        }

        [Fact]
        public async Task TestMethodAssignmentReturn293Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        string FooRet = default;
        MyEvent += (_, __) => Foo();
        FooRet = FooRet + """";
        FooRet += nameof(Foo);
        return FooRet;
    }
}
1 source compilation errors:
BC30284: function 'Foo' cannot be declared 'Overrides' because it does not override a function in a base class.
1 target compilation errors:
CS0115: 'Class1.Foo()': no suitable method found to override");
        }

        [Fact]
        public async Task TestMethodAssignmentAdditionReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
End Class", @"
internal partial class Class1
{
    public int TestMethod(int x)
    {
        int TestMethodRet = default;
        if (x == 1)
        {
            TestMethodRet += 1;
        }
        else if (x == 2)
        {
            TestMethodRet -= 2;
            return TestMethodRet;
        }
        else if (x == 3)
        {
            TestMethodRet *= TestMethod(1);
        }

        return TestMethodRet;
    }
}");
        }

        [Fact]
        public async Task TestMethodMissingReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class Class1
    Function TestMethod() As Integer

    End Function
End Class", @"
internal partial class Class1
{
    public int TestMethod()
    {
        return default;
    }
}");
        }

        [Fact]
        public async Task TestGetIteratorDoesNotGainReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        public async Task TestMethodXmlDocAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class TestClass
    ''' <summary>Xml doc</summary>
    Public Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"
internal partial class TestClass
{
    /// <summary>Xml doc</summary>
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}");
        }

        [Fact]
        public async Task TestMethodWithOutParameterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        public async Task TestMethodWithReturnTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Function TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) As Integer
        Return 0
    End Function
End Class", @"
internal partial class TestClass
{
    public int TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        return 0;
    }
}
1 target compilation errors:
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method");
        }

        [Fact]
        public async Task TestFunctionWithNoReturnTypeSpecifiedAsync()
        {
            // Note: "Inferred" type is always object except with local variables
            // https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/variables/local-type-inference
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Private Function TurnFirstToUp(ByVal Text As String)
        Dim firstCharacter = Text.Substring(0, 1).ToUpper()
        Return firstCharacter + Text.Substring(1)
    End Function
End Class", @"
internal partial class TestClass
{
    private object TurnFirstToUp(string Text)
    {
        string firstCharacter = Text.Substring(0, 1).ToUpper();
        return firstCharacter + Text.Substring(1);
    }
}");
        }

        [Fact]
        public async Task TestFunctionReturningTypeRequiringConversionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Function Four() As String
        Return 4
    End Function
End Class", @"
internal partial class TestClass
{
    public string Four()
    {
        return 4.ToString();
    }
}");
        }

        [Fact]
        public async Task TestStaticMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Shared Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"
internal partial class TestClass
{
    public static void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}");
        }

        [Fact]
        public async Task TestAbstractMethodAndPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"MustInherit Class TestClass
    Public MustOverride Sub TestMethod()
    Public MustOverride ReadOnly Property AbstractProperty As String
End Class", @"
internal abstract partial class TestClass
{
    public abstract void TestMethod();

    public abstract string AbstractProperty { get; }
}");
        }

        [Fact]
        public async Task TestSealedMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public NotOverridable Sub TestMethod(Of T As {Class, New}, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
        argument = Nothing
        argument2 = Nothing
        argument3 = Nothing
    End Sub
End Class", @"
internal partial class TestClass
{
    public sealed void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3)
        where T : class, new()
        where T2 : struct
    {
        argument = null;
        argument2 = default;
        argument3 = default;
    }
}
1 source compilation errors:
BC31088: 'NotOverridable' cannot be specified for methods that do not override another method.
1 target compilation errors:
CS0238: 'TestClass.TestMethod<T, T2, T3>(out T, ref T2, T3)' cannot be sealed because it is not an override");
        }

        [Fact]
        public async Task TestShadowedMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        public async Task TestExtensionMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Module TestClass
    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub

    <System.Runtime.CompilerServices.Extension()>
    Sub TestMethod2Parameters(ByVal str As String, other As String)
    End Sub
End Module", @"
internal static partial class TestClass
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
        public async Task TestExtensionMethodWithExistingImportAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Imports System.Runtime.CompilerServices ' Removed by simplifier

Module TestClass
    <Extension()>
    Sub TestMethod(ByVal str As String)
    End Sub
End Module", @"
internal static partial class TestClass
{
    public static void TestMethod(this string str)
    {
    }
}");
        }

        [Fact]
        public async Task TestPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
            If 7 = Integer.Parse(""7"") Then Exit Property
            Return Me.m_test3
        End Get
        Set(ByVal value As Integer)
            If 7 = Integer.Parse(""7"") Then Exit Property
            Me.m_test3 = value
        End Set
    End Property
End Class", @"
internal partial class TestClass
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
            if (7 == int.Parse(""7""))
                return default;
            return m_test3;
        }

        set
        {
            if (7 == int.Parse(""7""))
                return;
            m_test3 = value;
        }
    }
}
1 source compilation errors:
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.");
        }

         [Fact]
        public async Task TestParameterizedPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
End Class", @"
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool lastNameFirst, bool isFirst)
    {
        if (lastNameFirst)
        {
            return LastName + "" "" + FirstName;
        }
        else
        {
            return FirstName + "" "" + LastName;
        }
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
}", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for parameterized property
        }

        [Fact]
        public async Task TestParameterizedPropertyAndGenericInvocationAndEnumEdgeCasesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        return blah.ToString();
    }

    public void set_MyProp(int blah, string value)
    {
    }

    public void ReturnWhatever(MyEnum m)
    {
        var enumerableThing = Enumerable.Empty<string>();
        switch (m)
        {
            case (MyEnum)(-1):
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
}", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for parameterized property
        }

        [Fact]
        public async Task PropertyWithMissingTypeDeclarationAsync()//TODO Check object is the inferred type
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class MissingPropertyType
                ReadOnly Property Max
                    Get
                        Dim mx As Double = 0
                        Return mx
                    End Get
                End Property
End Class", @"
internal partial class MissingPropertyType
{
    public object Max
    {
        get
        {
            double mx = 0;
            return mx;
        }
    }
}");
        }

        [Fact]
        public async Task TestReadWriteOnlyInterfacePropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Interface Foo
    ReadOnly Property P1() As String
    WriteOnly Property P2() As String
End Interface", @"
public partial interface Foo
{
    string P1 { get; }
    string P2 { set; }
}");
        }

        [Fact]
        public async Task TestConstructorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class", @"
internal partial class TestClass<T, T2, T3>
    where T : class, new()
    where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}
1 target compilation errors:
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method");
        }

        [Fact]
        public async Task TestConstructorWithImplicitPublicAccessibilityAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Sub New()
End Sub", @"public SurroundingClass()
{
}");
        }

        [Fact]
        public async Task TestStaticConstructorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Shared Sub New()
End Sub", @"static SurroundingClass()
{
}");
        }

        [Fact]
        public async Task TestDestructorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Protected Overrides Sub Finalize()
    End Sub
End Class", @"
internal partial class TestClass
{
    ~TestClass()
    {
    }
}");
        }

        [Fact]
        public async Task TestEventAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Event MyEvent As EventHandler
End Class", @"using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;
}");
        }

        [Fact]
        public async Task TestEventWithNoDeclaredTypeOrHandlersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class TestEventWithNoType
    Public Event OnCakeChange

    Public Sub RaisingFlour()
        RaiseEvent OnCakeChange
    End Sub
End Class", @"
public partial class TestEventWithNoType
{
    public event OnCakeChangeEventHandler OnCakeChange;

    public delegate void OnCakeChangeEventHandler();

    public void RaisingFlour()
    {
        OnCakeChange?.Invoke();
    }
}
1 target compilation errors:
CS1547: Keyword 'void' cannot be used in this context");
        }

        [Fact]
        public async Task TestModuleHandlesWithEventsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
    ' Comment bug: This comment moves due to the Handles transformation
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

internal static partial class Module1
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
                // Comment bug: This comment moves due to the Handles transformation
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
}
1 target compilation errors:
CS1547: Keyword 'void' cannot be used in this context");
        }

        [Fact]
        public async Task TestWithEventsWithoutInitializerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
1 target compilation errors:
CS1547: Keyword 'void' cannot be used in this context
");
        }

        [Fact]
        public async Task TestClassHandlesWithEventsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class MyEventClass
    Public Event TestEvent()

    Sub RaiseEvents()
        RaiseEvent TestEvent()
    End Sub
End Class

Class Class1
    Shared WithEvents SharedEventClassInstance As New MyEventClass
    WithEvents NonSharedEventClassInstance As New MyEventClass

    Public Sub New(num As Integer)
    End Sub

    Public Sub New(obj As Object)
        MyClass.New()
    End Sub

    Shared Sub PrintTestMessage2() Handles SharedEventClassInstance.TestEvent, NonSharedEventClassInstance.TestEvent
    End Sub

    Sub PrintTestMessage3() Handles NonSharedEventClassInstance.TestEvent
    End Sub
End Class

Public Class ShouldNotGainConstructor
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

    public Class1()
    {
        NonSharedEventClassInstance = new MyEventClass();
    }

    public Class1(int num)
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
}

public partial class ShouldNotGainConstructor
{
    static ShouldNotGainConstructor()
    {
        SharedEventClassInstance = new MyEventClass();
    }
}
1 source compilation errors:
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
2 target compilation errors:
CS1547: Keyword 'void' cannot be used in this context
CS0103: The name 'SharedEventClassInstance' does not exist in the current context", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for events
        }

        [Fact]
        public async Task TestPartialClassHandlesWithEventsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
}
1 target compilation errors:
CS1547: Keyword 'void' cannot be used in this context", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for events
        }

        [Fact]
        public async Task TestInitializeComponentAddsEventHandlersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports Microsoft.VisualBasic.CompilerServices

<DesignerGenerated>
Partial Public Class TestHandlesAdded

    Sub InitializeComponent()
        '
        'POW_btnV2DBM
        '
        Me.POW_btnV2DBM.Location = New System.Drawing.Point(207, 15)
        Me.POW_btnV2DBM.Name = ""POW_btnV2DBM""
        Me.POW_btnV2DBM.Size = New System.Drawing.Size(42, 23)
        Me.POW_btnV2DBM.TabIndex = 3
        Me.POW_btnV2DBM.Text = "">>""
        Me.POW_btnV2DBM.UseVisualStyleBackColor = True
    End Sub

End Class

Partial Public Class TestHandlesAdded
    Dim WithEvents POW_btnV2DBM As Button

    Public Sub POW_btnV2DBM_Click() Handles POW_btnV2DBM.Click

    End Sub
End Class", @"using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices;

[DesignerGenerated]
public partial class TestHandlesAdded
{
    public TestHandlesAdded()
    {
        InitializeComponent();
    }

    public void InitializeComponent()
    {
        // 
        // POW_btnV2DBM
        // 
        _POW_btnV2DBM.Location = new System.Drawing.Point(207, 15);
        _POW_btnV2DBM.Name = ""POW_btnV2DBM"";
        _POW_btnV2DBM.Size = new System.Drawing.Size(42, 23);
        _POW_btnV2DBM.TabIndex = 3;
        _POW_btnV2DBM.Text = "">>"";
        _POW_btnV2DBM.UseVisualStyleBackColor = true;
    }
}

public partial class TestHandlesAdded
{
    private Button _POW_btnV2DBM;

    private Button POW_btnV2DBM
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _POW_btnV2DBM;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_POW_btnV2DBM != null)
            {
                _POW_btnV2DBM.Click -= POW_btnV2DBM_Click;
            }

            _POW_btnV2DBM = value;
            if (_POW_btnV2DBM != null)
            {
                _POW_btnV2DBM.Click += POW_btnV2DBM_Click;
            }
        }
    }

    public void POW_btnV2DBM_Click()
    {
    }
}
2 source compilation errors:
BC30002: Type 'Button' is not defined.
BC30590: Event 'Click' cannot be found.
1 target compilation errors:
CS0246: The type or namespace name 'Button' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task SynthesizedBackingFieldAccessAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Shared Property First As Integer

    Private Second As Integer = _First
End Class", @"
internal partial class TestClass
{
    private static int First { get; set; }

    private int Second = First;
}");
        }

        [Fact]
        public async Task PropertyInitializersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private ReadOnly Property First As New List(Of String)
    Private Property Second As Integer = 0
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private List<string> First { get; private set; } = new List<string>();
    private int Second { get; set; } = 0;
}
1 target compilation errors:
CS0273: The accessibility modifier of the 'TestClass.First.set' accessor must be more restrictive than the property or indexer 'TestClass.First'");
        }

        [Fact]
        public async Task PartialFriendClassWithOverloadsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Partial Friend MustInherit Class TestClass1
    Public Shared Sub CreateStatic()
    End Sub

    Public Overloads Sub CreateInstance()
    End Sub

    Public Overloads Sub CreateInstance(o As Object)
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
    
    Public Overloads Sub CreateVirtualInstance(o As Object)
    End Sub
End Class",
@"
internal abstract partial class TestClass1
{
    public static void CreateStatic()
    {
    }

    public void CreateInstance()
    {
    }

    public void CreateInstance(object o)
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

    public void CreateVirtualInstance(object o)
    {
    }
}");
        }

        [Fact]
        public async Task TestNarrowingWideningConversionOperatorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class MyInt
    Public Shared Narrowing Operator CType(i As Integer) As MyInt
        Return New MyInt()
    End Operator
    Public Shared Widening Operator CType(myInt As MyInt) As Integer
        Return 1
    End Operator
End Class"
                , @"
public partial class MyInt
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
        public async Task OperatorOverloadsAsync()
        {
            // Note a couple map to the same thing in C# so occasionally the result won't compile. The user can manually decide what to do in such scenarios.
            await TestConversionVisualBasicToCSharpAsync(@"Public Class AcmeClass
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
End Class", @"
public partial class AcmeClass
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
}
1 target compilation errors:
CS0111: Type 'AcmeClass' already defines a member called 'op_Division' with the same parameter types");
        }

        [Fact]// The stack trace displayed will change from time to time. Feel free to update this characterization test appropriately.
        public async Task OperatorOverloadsWithNoCSharpEquivalentShowErrorInlineCharacterizationAsync()
        {
            // No valid conversion to C# - to implement this you'd need to create a new method, and convert all callers to use it.
            var convertedCode = await ConvertAsync<VBToCSConversion>(@"Public Class AcmeClass
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
            Assert.Contains("Public Shared Operator ^(i As Integer,", convertedCode);
            Assert.Contains("_failedMemberConversionMarker2", convertedCode);
            Assert.Contains("Public Shared Operator Like(s As String,", convertedCode);
        }

        [Fact]
        public async Task ClassWithGloballyQualifiedAttributeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"<Global.System.Diagnostics.DebuggerDisplay(""Hello World"")>
Class TestClass
End Class", @"using System.Diagnostics;

[DebuggerDisplay(""Hello World"")]
internal partial class TestClass
{
}");
        }

        [Fact]
        public async Task FieldWithAttributeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
        public async Task ParamArrayAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub SomeBools(ParamArray anyName As Boolean())
    End Sub
End Class", @"
internal partial class TestClass
{
    private void SomeBools(params bool[] anyName)
    {
    }
}");
        }

        [Fact]
        public async Task ParamNamedBoolAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub SomeBools(ParamArray bool As Boolean())
    End Sub
End Class", @"
internal partial class TestClass
{
    private void SomeBools(params bool[] @bool)
    {
    }
}");
        }

        [Fact]
        public async Task MethodWithNameArrayParameterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Sub DoNothing(ByVal strs() As String)
        Dim moreStrs() As String
    End Sub
End Class",
@"
internal partial class TestClass
{
    public void DoNothing(string[] strs)
    {
        string[] moreStrs;
    }
}");
        }

        [Fact]
        public async Task UntypedParametersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Sub DoNothing(obj, objs())
    End Sub
End Class",
@"
internal partial class TestClass
{
    public void DoNothing(object obj, object[] objs)
    {
    }
}");
        }

        [Fact]
        public async Task PartialClassAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
        public async Task NestedClassAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class ClA
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
End Class", @"
internal partial class ClA
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
        public async Task LessQualifiedNestedClassAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class ClA
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
End Class", @"
internal partial class ClA
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
        public async Task TestIndexerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
End Class", @"
internal partial class TestClass
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
        public async Task TestWriteOnlyPropertiesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Interface TestInterface
    WriteOnly Property Items As Integer()
End Interface", @"
internal partial interface TestInterface
{
    int[] Items { set; }
}");
        }

        [Fact]
        public async Task TestImplicitPrivateSetterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class SomeClass
    Public ReadOnly Property SomeValue As Integer

    Public Sub SetValue(value1 As Integer, value2 As Integer)
        _SomeValue = value1 + value2
    End Sub
End Class", @"
public partial class SomeClass
{
    public int SomeValue { get; private set; }

    public void SetValue(int value1, int value2)
    {
        SomeValue = value1 + value2;
    }
}");
        }

        [Fact]
        public async Task TestSetWithNamedParameterPropertiesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
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
End Class", @"
internal partial class TestClass
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

        [Fact]
        public async Task TestAsyncMethodsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"    Class AsyncCode
        Public Sub NotAsync()
            Dim a1 = Async Function() 3
            Dim a2 = Async Function()
                         Return Await Task (Of Integer).FromResult(3)
                     End Function
            Dim a3 = Async Sub() Await Task.CompletedTask
            Dim a4 = Async Sub()
                        Await Task.CompletedTask
                    End Sub
        End Sub

        Public Async Function AsyncFunc() As Task(Of Integer)
            Return Await Task (Of Integer).FromResult(3)
        End Function
        Public Async Sub AsyncSub()
            Await Task.CompletedTask
        End Sub
    End Class", @"using System.Threading.Tasks;

internal partial class AsyncCode
{
    public void NotAsync()
    {
        async Task<int> a1() => 3;

        async Task<int> a2() => await Task.FromResult(3);

        async void a3() => await Task.CompletedTask;

        async void a4() => await Task.CompletedTask;
    }

    public async Task<int> AsyncFunc()
    {
        return await Task.FromResult(3);
    }

    public async void AsyncSub()
    {
        await Task.CompletedTask;
    }
}
");
        }

        [Fact]
        public async Task TestAsyncMethodsWithNoReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Friend Partial Module TaskExtensions
    <Extension()>
    Async Function [Then](Of T)(ByVal task As Task, ByVal f As Func(Of Task(Of T))) As Task(Of T)
        Await task
        Return Await f()
    End Function

    <Extension()>
    Async Function [Then](ByVal task As Task, ByVal f As Func(Of Task)) As Task
        Await task
        Await f()
    End Function

    <Extension()>
    Async Function [Then](Of T, U)(ByVal task As Task(Of T), ByVal f As Func(Of T, Task(Of U))) As Task(Of U)
        Return Await f(Await task)
    End Function

    <Extension()>
    Async Function [Then](Of T)(ByVal task As Task(Of T), ByVal f As Func(Of T, Task)) As Task
        Await f(Await task)
    End Function

    <Extension()>
    Async Function [ThenExit](Of T)(ByVal task As Task(Of T), ByVal f As Func(Of T, Task)) As Task
        Await f(Await task)
        Exit Function
    End Function
End Module", @"using System;
using System.Threading.Tasks;

internal static partial class TaskExtensions
{
    public async static Task<T> Then<T>(this Task task, Func<Task<T>> f)
    {
        await task;
        return await f();
    }

    public async static Task Then(this Task task, Func<Task> f)
    {
        await task;
        await f();
    }

    public async static Task<U> Then<T, U>(this Task<T> task, Func<T, Task<U>> f)
    {
        return await f(await task);
    }

    public async static Task Then<T>(this Task<T> task, Func<T, Task> f)
    {
        await f(await task);
    }

    public async static Task ThenExit<T>(this Task<T> task, Func<T, Task> f)
    {
        await f(await task);
        return;
    }
}");
        }

        [Fact]
        public async Task TestExternDllImportAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"<DllImport(""kernel32.dll"", SetLastError:=True)>
Private Shared Function OpenProcess(ByVal dwDesiredAccess As AccessMask, ByVal bInheritHandle As Boolean, ByVal dwProcessId As UInteger) As IntPtr
End Function", @"[DllImport(""kernel32.dll"", SetLastError = true)]
private static extern IntPtr OpenProcess(AccessMask dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

1 source compilation errors:
BC30002: Type 'AccessMask' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'AccessMask' could not be found (are you missing a using directive or an assembly reference?)");
        }
    }
}
