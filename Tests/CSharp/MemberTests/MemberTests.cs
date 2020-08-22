using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests
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
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
    public A()
    {
        y = new int[x + 1];
    }

    private int x = 2;
    private int[] y;
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
    public Issue281()
    {
        nonShared = new ErrorEventHandler(OnError);
    }

    private Delegate lambda = new ErrorEventHandler((a, b) => Strings.Len(0));
    private Delegate nonShared;

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

        [Fact]
        public async Task Issue443_FixCaseForIntefaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function FooDifferentCase(<Out> ByRef str2 As String) As Integer
End Interface

Public Class Foo
    Implements IFoo
    Function fooDifferentCase(<Out> ByRef str2 As String) As Integer Implements IFoo.FOODIFFERENTCASE
        str2 = 2.ToString()
        Return 3
    End Function
End Class", @"
public partial interface IFoo
{
    int FooDifferentCase(out string str2);
}

public partial class Foo : IFoo
{
    public int FooDifferentCase(out string str2)
    {
        str2 = 2.ToString();
        return 3;
    }
}
");
        }

        [Fact]
        public async Task Issue444_FixNameForRenamedInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function FooDifferentName(ByRef str As String, i As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Function BarDifferentName(ByRef str As String, i As Integer) As Integer Implements IFoo.FooDifferentName
        Return 4
    End Function
End Class", @"
public partial interface IFoo
{
    int FooDifferentName(ref string str, int i);
}

public partial class Foo : IFoo
{
    public int FooDifferentName(ref string str, int i)
    {
        return 4;
    }

    public int BarDifferentName(ref string str, int i) => FooDifferentName(ref str, i);
}
");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Private Function ExplicitFunc(ByRef str As String, i As Integer) As Integer Implements IFoo.ExplicitFunc
        Return 5
    End Function
    
    Private Property ExplicitProp(str As String) As Integer Implements IFoo.ExplicitProp
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class", @"
public partial interface IFoo
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial class Foo : IFoo
{
    int IFoo.ExplicitFunc(ref string str, int i)
    {
        return 5;
    }

    int IFoo.get_ExplicitProp(string str)
    {
        return 5;
    }

    void IFoo.set_ExplicitProp(string str, int value)
    {
    }
}
");
        }

        [Fact]
        public async Task Issue444_InternalMemberDoesNotRequireDelegatingMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function FooDifferentName(ByRef str As String, i As Integer) As Integer
End Interface

Friend Class Foo
    Implements IFoo

    Function BarDifferentName(ByRef str As String, i As Integer) As Integer Implements IFoo.FooDifferentName
        Return 4
    End Function
End Class", @"
public partial interface IFoo
{
    int FooDifferentName(ref string str, int i);
}

internal partial class Foo : IFoo
{
    public int FooDifferentName(ref string str, int i)
    {
        return 4;
    }
}
");
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
        public async Task Issue420_RenameClashingEnumMemberAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Enum MyEnum
    MyEnumFirst
End Enum", @"
internal enum MyEnumType
{
    MyEnumFirst
}
");
        }
    }
}
