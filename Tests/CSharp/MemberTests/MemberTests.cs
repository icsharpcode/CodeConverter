using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

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
    public async Task TestDeclareMethodVisibilityInModuleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Module Module1
    Declare Sub External Lib ""lib.dll"" ()
End Module", @"using System.Runtime.InteropServices;

internal static partial class Module1
{
    [DllImport(""lib.dll"")]
    public static extern void External();
}");
    }

    [Fact]
    public async Task TestDeclareMethodVisibilityInClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class Class1
    Declare Sub External Lib ""lib.dll"" ()
End Class", @"using System.Runtime.InteropServices;

internal partial class Class1
{
    [DllImport(""lib.dll"")]
    public static extern void External();
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
        // VB doesn't infer the type of EnumVariable like you'd think, it just uses object
        // VB compiler uses Conversions rather than any plainer casting
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Public Enum TestEnum As Integer
        Test1
    End Enum

    Dim EnumVariable = TestEnum.Test1
    Public Sub AMethod()
        Dim t1 As Integer = EnumVariable
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public enum TestEnum : int
    {
        Test1
    }

    private object EnumVariable = TestEnum.Test1;
    public void AMethod()
    {
        int t1 = Conversions.ToInteger(EnumVariable);
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
    public async Task TestAbstractReadOnlyAndWriteOnlyPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"MustInherit Class TestClass
        Public MustOverride ReadOnly Property ReadOnlyProp As String
        Public MustOverride WriteOnly Property WriteOnlyProp As String
End Class

Class ChildClass
    Inherits TestClass

    Public Overrides ReadOnly Property ReadOnlyProp As String
    Public Overrides WriteOnly Property WriteOnlyProp As String
        Set
        End Set
    End Property
End Class
", @"
internal abstract partial class TestClass
{
    public abstract string ReadOnlyProp { get; }
    public abstract string WriteOnlyProp { set; }
}

internal partial class ChildClass : TestClass
{

    public override string ReadOnlyProp { get; }
    public override string WriteOnlyProp
    {
        set
        {
        }
    }
}");
    }

    [Fact]
    public async Task SetterProperty1053Async()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Property Prop(ByVal i As Integer) As String
    Get
        Static bGet As Boolean
        bGet = False
    End Get

    Set(ByVal s As String)
        Static bSet As Boolean
        bSet = False
    End Set
End Property
", @"
internal partial class SurroundingClass
{
    private bool _Prop_bGet;
    private bool _Prop_bSet;

    public string get_Prop(int i)
    {
        _Prop_bGet = false;
        return default;
    }

    public void set_Prop(int i, string value)
    {
        _Prop_bSet = false;
    }

}");
    }

    [Fact]
    public async Task StaticLocalsInPropertyGetterAndSetterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Property Prop As String
    Get
        Static b As Boolean
        b = True
    End Get

    Set(ByVal s As String)
        Static b As Boolean
        b = False
    End Set
End Property
", @"
internal partial class SurroundingClass
{
    private bool _Prop_b;
    private bool _Prop_b1;

    public string Prop
    {
        get
        {
            _Prop_b = true;
            return default;
        }

        set
        {
            _Prop_b1 = false;
        }
    }

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
    public async Task Issue681_OverloadsOverridesPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class C 
    Inherits B

    Public ReadOnly Overloads Overrides Property X()
        Get
            Return Nothing
        End Get
    End Property
End Class

Public Class B
    Public ReadOnly Overridable Property X()
        Get
            Return Nothing
        End Get
    End Property
End Class", @"public partial class C : B
{

    public override object X
    {
        get
        {
            return null;
        }
    }
}

public partial class B
{
    public virtual object X
    {
        get
        {
            return null;
        }
    }
}");
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
    public async Task FieldWithNonStaticInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class A
    Private x As Integer = 2
    Private y(x) As Integer
End Class", @"
public partial class A
{
    private int x = 2;
    private int[] y;

    public A()
    {
        y = new int[x + 1];
    }
}");
    }

    [Fact]
    public async Task FieldWithInstanceOperationOfDifferingTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class DoesNotNeedConstructor
    Private ReadOnly ClassVariable1 As New ParallelOptions With {.MaxDegreeOfParallelism = 5}
End Class", @"using System.Threading.Tasks;

public partial class DoesNotNeedConstructor
{
    private readonly ParallelOptions ClassVariable1 = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
}");
    }

    [Fact]
    public async Task Issue281FieldWithNonStaticLambdaInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.IO

Public Class Issue281
    Private lambda As System.Delegate = New ErrorEventHandler(Sub(a, b) Len(0))
    Private nonShared As System.Delegate = New ErrorEventHandler(AddressOf OnError)

    Sub OnError(s As Object, e As ErrorEventArgs)
    End Sub
End Class", @"using System;
using System.IO;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Issue281
{
    private Delegate lambda = new ErrorEventHandler((a, b) => Strings.Len(0));
    private Delegate nonShared;

    public Issue281()
    {
        nonShared = new ErrorEventHandler(OnError);
    }

    public void OnError(object s, ErrorEventArgs e)
    {
    }
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
    public async Task Issue1097_PartialMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Partial Private Sub DummyMethod()
    End Sub", @"partial void DummyMethod();");
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
    public async Task TestAsyncFunctionExitReturnsDefaultAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Threading.Tasks

Class AsyncExit
    Public Async Function AsyncFuncExit() As Task(Of Integer)
        Await Task.Delay(1)
        Exit Function
    End Function
End Class", @"using System.Threading.Tasks;

internal partial class AsyncExit
{
    public async Task<int> AsyncFuncExit()
    {
        await Task.Delay(1);
        return default;
    }
}");
    }

    [Fact]
    public async Task TestAsyncTaskFunctionNoImplicitReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Threading.Tasks

Class AsyncNoReturn
    Public Async Function DoAsync() As Task
        Await Task.Delay(1)
    End Function
End Class", @"using System.Threading.Tasks;

internal partial class AsyncNoReturn
{
    public async Task DoAsync()
    {
        await Task.Delay(1);
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
private static extern nint OpenProcess(AccessMask dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

1 source compilation errors:
BC30002: Type 'AccessMask' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'AccessMask' could not be found (are you missing a using directive or an assembly reference?)");
    }

    [Fact]
    public async Task Issue420_RenameClashingClassMemberAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Main
    Sub Main()
    End Sub
End Module", @"
internal static partial class MainType
{
    public static void Main()
    {
    }
}
");
    }

    [Fact]
    public async Task Issue420_DoNotRenameClashingEnumMemberAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Enum MyEnum
    MyEnumFirst
    MyEnum
End Enum", @"
internal enum MyEnum
{
    MyEnumFirst,
    MyEnum
}
");
    }

    [Fact]
    public async Task Issue420_DoNotRenameClashingEnumMemberForPublicEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Enum MyEnum
    MyEnumFirst
    MyEnum
End Enum", @"
public enum MyEnum
{
    MyEnumFirst,
    MyEnum
}
");
    }

    [Fact]
    public async Task TestPropertyStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Readonly Property OtherName() As Integer
        Get
            Static sPrevPosition As Integer = 3 ' Comment moves with declaration
            Console.WriteLine(sPrevPosition)
            Return sPrevPosition
        End Get
    End Property
    Readonly Property OtherName(x As Integer) as Integer
        Get
            Static sPrevPosition As Integer
            sPrevPosition += 1
            Return sPrevPosition
        End Get
    End Property
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public int OtherName
    {
        get
        {
            Console.WriteLine(_OtherName_sPrevPosition);
            return _OtherName_sPrevPosition;
        }
    }

    private int _OtherName_sPrevPosition1 = default;
    public int get_OtherName(int x)
    {
        _OtherName_sPrevPosition1 += 1;
        return _OtherName_sPrevPosition1;
    }
}");
    }

    [Fact]
    public async Task TestStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Sub OtherName(x As Boolean)
        Static sPrevPosition As Integer = 3 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Function OtherName(x As Integer) as Integer
        Static sPrevPosition As Integer
        Return sPrevPosition
    End Function
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public void OtherName(bool x)
    {
        Console.WriteLine(_OtherName_sPrevPosition);
    }

    private int _OtherName_sPrevPosition1 = default;
    public int OtherName(int x)
    {
        return _OtherName_sPrevPosition1;
    }
}");
    }

    [Fact]
    public async Task TestStaticLocalWithoutDefaultInitializerConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Function OtherName() as Integer
        Static sPrevPosition As Integer
        sPrevPosition = 23
        Console.WriteLine(sPrevPosition)
        Return sPrevPosition
    End Function
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition;
    public int OtherName()
    {
        _OtherName_sPrevPosition = 23;
        Console.WriteLine(_OtherName_sPrevPosition);
        return _OtherName_sPrevPosition;
    }
}");
    }

    [Fact]
    public async Task TestModuleStaticLocalConvertedToStaticFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module StaticLocalConvertedToField
    Sub OtherName(x As Boolean)
        Static sPrevPosition As Integer = 3 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Function OtherName(x As Integer) as Integer
        Static sPrevPosition As Integer ' Comment also moves with declaration
        Return sPrevPosition
    End Function
End Module", @"using System;

internal static partial class StaticLocalConvertedToField
{
    private static int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public static void OtherName(bool x)
    {
        Console.WriteLine(_OtherName_sPrevPosition);
    }

    private static int _OtherName_sPrevPosition1 = default;
    public static int OtherName(int x) // Comment also moves with declaration
    {
        return _OtherName_sPrevPosition1;
    }
}");
    }

    [Fact]
    public async Task TestStaticLocalConvertedToStaticFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Shared Sub OtherName(x As Boolean)
        Static sPrevPosition As Integer ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Sub OtherName(x As Integer)
        Static sPrevPosition As Integer = 5 ' Comment also moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Shared ReadOnly Property StaticTestProperty() As Integer
        Get
            Static sPrevPosition As Integer = 5 ' Comment also moves with declaration
            Return sPrevPosition + 1
        End Get
    End Property
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private static int _OtherName_sPrevPosition = default;
    public static void OtherName(bool x) // Comment moves with declaration
    {
        Console.WriteLine(_OtherName_sPrevPosition);
    }
    private int _OtherName_sPrevPosition1 = 5; // Comment also moves with declaration
    public void OtherName(int x)
    {
        Console.WriteLine(_OtherName_sPrevPosition1);
    }
    private static int _StaticTestProperty_sPrevPosition = 5; // Comment also moves with declaration
    public static int StaticTestProperty
    {
        get
        {
            return _StaticTestProperty_sPrevPosition + 1;
        }
    }
}");
    }

    [Fact]
    public async Task TestOmittedArgumentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class OmittedArguments
    Sub M(Optional a As String = ""a"", ByRef Optional b As String = ""b"")
        Dim s As String = """"

        M() 'omitted implicitly
        M(,) 'omitted explicitly

        M(s) 'omitted implicitly
        M(s,) 'omitted explicitly

        M(a:=s) 'omitted implicitly
        M(a:=s, ) 'omitted explicitly
    End Sub
End Class", @"using System.Runtime.InteropServices;

internal partial class OmittedArguments
{
    public void M([Optional, DefaultParameterValue(""a"")] string a, [Optional, DefaultParameterValue(""b"")] ref string b)
    {
        string s = """";

        string argb = ""b"";
        M(b: ref argb); // omitted implicitly
        string argb1 = ""b"";
        M(b: ref argb1); // omitted explicitly

        string argb2 = ""b"";
        M(s, b: ref argb2); // omitted implicitly
        string argb3 = ""b"";
        M(s, b: ref argb3); // omitted explicitly

        string argb4 = ""b"";
        M(a: s, b: ref argb4); // omitted implicitly
        string argb5 = ""b"";
        M(a: s, b: ref argb5); // omitted explicitly
    }
}");
    }

    [Fact]
    public async Task TestRefConstArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class RefConstArgument
    Const a As String = ""a""
    Sub S()
        Const b As String = ""b""
        MO(a)
        MS(b)
    End Sub
    Sub MO(ByRef s As Object) : End Sub
    Sub MS(ByRef s As String) : End Sub
End Class", @"
internal partial class RefConstArgument
{
    private const string a = ""a"";
    public void S()
    {
        const string b = ""b"";
        object args = a;
        MO(ref args);
        string args1 = b;
        MS(ref args1);
    }
    public void MO(ref object s)
    {
    }
    public void MS(ref string s)
    {
    }
}");
    }

    [Fact]
    public async Task TestRefFunctionCallNoParenthesesArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class RefFunctionCallArgument
    Sub S(ByRef o As Object)
        S(GetI)
    End Sub
    Function GetI() As Integer : End Function
End Class", @"
internal partial class RefFunctionCallArgument
{
    public void S(ref object o)
    {
        object argo = GetI();
        S(ref argo);
    }
    public int GetI()
    {
        return default;
    }
}");
    }

    [Fact]
    public async Task TestMissingByRefArgumentWithNoExplicitDefaultValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Runtime.InteropServices

Class MissingByRefArgumentWithNoExplicitDefaultValue
    Sub S()
        ByRefNoDefault()
        OptionalByRefNoDefault()
        OptionalByRefWithDefault()
    End Sub

    Private Sub ByRefNoDefault(ByRef str1 As String) : End Sub
    Private Sub OptionalByRefNoDefault(<[Optional]> ByRef str2 As String) : End Sub
    Private Sub OptionalByRefWithDefault(<[Optional], DefaultParameterValue(""a"")> ByRef str3 As String) : End Sub
End Class", @"using System.Runtime.InteropServices;

internal partial class MissingByRefArgumentWithNoExplicitDefaultValue
{
    public void S()
    {
        ByRefNoDefault();
        string argstr2 = default;
        OptionalByRefNoDefault(str2: ref argstr2);
        string argstr3 = ""a"";
        OptionalByRefWithDefault(str3: ref argstr3);
    }

    private void ByRefNoDefault(ref string str1)
    {
    }
    private void OptionalByRefNoDefault([Optional] ref string str2)
    {
    }
    private void OptionalByRefWithDefault([Optional][DefaultParameterValue(""a"")] ref string str3)
    {
    }
}
3 source compilation errors:
BC30455: Argument not specified for parameter 'str1' of 'Private Sub ByRefNoDefault(ByRef str1 As String)'.
BC30455: Argument not specified for parameter 'str2' of 'Private Sub OptionalByRefNoDefault(ByRef str2 As String)'.
BC30455: Argument not specified for parameter 'str3' of 'Private Sub OptionalByRefWithDefault(ByRef str3 As String)'.
1 target compilation errors:
CS7036: There is no argument given that corresponds to the required parameter 'str1' of 'MissingByRefArgumentWithNoExplicitDefaultValue.ByRefNoDefault(ref string)'
");
    }
}