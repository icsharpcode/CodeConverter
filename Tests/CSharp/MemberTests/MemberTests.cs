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
        public async Task TestHoistedOutParameterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Class ClassWithProperties
   Public Property Property1 As String
End Class

Public Class VisualBasicClass
   Public Sub New()
       Dim x As New Dictionary(Of String, String)()
       Dim y As New ClassWithProperties()
       
       If (x.TryGetValue(""x"", y.Property1)) Then
          Debug.Print(y.Property1)
       End If
   End Sub
End Class", @"using System.Collections.Generic;
using System.Diagnostics;

public partial class ClassWithProperties
{
    public string Property1 { get; set; }
}

public partial class VisualBasicClass
{
    public VisualBasicClass()
    {
        var x = new Dictionary<string, string>();
        var y = new ClassWithProperties();
        bool localTryGetValue() { string argvalue = y.Property1; var ret = x.TryGetValue(""x"", out argvalue); y.Property1 = argvalue; return ret; }

        if (localTryGetValue())
        {
            Debug.Print(y.Property1);
        }
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
        public async Task TestRefExtensionMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Imports System
Imports System.Runtime.CompilerServices ' Removed since the extension attribute is removed

Public Module MyExtensions
    <Extension()>
    Public Sub Add(Of T)(ByRef arr As T(), item As T)
        Array.Resize(arr, arr.Length + 1)
        arr(arr.Length - 1) = item
    End Sub
End Module

Public Module UsagePoint
    Public Sub Main()
        Dim arr = New Integer() {1, 2, 3}
        arr.Add(4)
        System.Console.WriteLine(arr(3))
    End Sub
End Module", @"using System;

public static partial class MyExtensions
{
    public static void Add<T>(ref T[] arr, T item)
    {
        Array.Resize(ref arr, arr.Length + 1);
        arr[arr.Length - 1] = item;
    }
}

public static partial class UsagePoint
{
    public static void Main()
    {
        var arr = new int[] { 1, 2, 3 };
        MyExtensions.Add(ref arr, 4);
        Console.WriteLine(arr[3]);
    }
}");
        }

        [Fact]
        public async Task TestExtensionWithinExtendedTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
  Sub TestExtensionConsumer()
    TestExtension()
  End Sub
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
    public void TestExtensionConsumer()
    {
        this.TestExtension();
    }
}");
        }

        [Fact]
        public async Task TestExtensionWithinTypeDerivedFromExtendedTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
End Class

Class DerivedClass
    Inherits ExtendedClass

  Sub TestExtensionConsumer()
    TestExtension()
  End Sub
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
}

internal partial class DerivedClass : ExtendedClass
{
    public void TestExtensionConsumer()
    {
        this.TestExtension();
    }
}");
        }

        [Fact]
        public async Task TestExtensionWithinNestedExtendedTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As NestingClass.ExtendedClass)
    End Sub
End Module

Class NestingClass
    Class ExtendedClass
      Sub TestExtensionConsumer()
        TestExtension()
      End Sub        
    End Class
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this NestingClass.ExtendedClass extendedClass)
    {
    }
}

internal partial class NestingClass
{
    public partial class ExtendedClass
    {
        public void TestExtensionConsumer()
        {
            this.TestExtension();
        }
    }
}");
        }

        [Fact]
        public async Task TestExtensionWithMeWithinExtendedTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Module Extensions
    <Extension()>
    Sub TestExtension(extendedClass As ExtendedClass)
    End Sub
End Module

Class ExtendedClass
  Sub TestExtensionConsumer()
    Me.TestExtension()
  End Sub
End Class", @"
internal static partial class Extensions
{
    public static void TestExtension(this ExtendedClass extendedClass)
    {
    }
}

internal partial class ExtendedClass
{
    public void TestExtensionConsumer()
    {
        this.TestExtension();
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
End Class", @"
public partial class C : B
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
        public async Task Issue443_FixCaseForInterfaceMembersAsync()
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
    int IFoo.FooDifferentCase(out string str2)
    {
        str2 = 2.ToString();
        return 3;
    }

    public int fooDifferentCase(out string str2) => ((IFoo)this).FooDifferentCase(out str2);
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
    int IFoo.FooDifferentName(ref string str, int i)
    {
        return 4;
    }

    public int BarDifferentName(ref string str, int i) => ((IFoo)this).FooDifferentName(ref str, i);
}
");
        }

        [Fact]
        public async Task IdenticalInterfaceMethodsWithRenamedInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Interface IBar
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Function Foo(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFooBar
        Return 4
    End Function

    Function Bar(ByRef str As String, i As Integer) As Integer Implements IBar.DoFooBar
        Return 2
    End Function

End Class", @"
public partial interface IFoo
{
    int DoFooBar(ref string str, int i);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i);
}

public partial class FooBar : IFoo, IBar
{
    int IFoo.DoFooBar(ref string str, int i)
    {
        return 4;
    }

    public int Foo(ref string str, int i) => ((IFoo)this).DoFooBar(ref str, i);

    int IBar.DoFooBar(ref string str, int i)
    {
        return 2;
    }

    public int Bar(ref string str, int i) => ((IBar)this).DoFooBar(ref str, i);
}
");
        }

        [Fact]
        public async Task RenamedMethodImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Interface IBar
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Function Foo(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFooBar, IBar.DoFooBar
        Return 4
    End Function

End Class", @"
public partial interface IFoo
{
    int DoFooBar(ref string str, int i);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i);
}

public partial class FooBar : IFoo, IBar
{
    int IFoo.DoFooBar(ref string str, int i)
    {
        return 4;
    }

    int IBar.DoFooBar(ref string str, int i) => ((IFoo)this).DoFooBar(ref str, i);
    public int Foo(ref string str, int i) => ((IFoo)this).DoFooBar(ref str, i);
}");
        }

        [Fact]
        public async Task IdenticalInterfacePropertiesWithRenamedInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
        Property FooBarProp As Integer
    End Interface

Public Interface IBar
    Property FooBarProp As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Property Foo As Integer Implements IFoo.FooBarProp

    Property Bar As Integer Implements IBar.FooBarProp
    
End Class", @"
public partial interface IFoo
{
    int FooBarProp { get; set; }
}

public partial interface IBar
{
    int FooBarProp { get; set; }
}

public partial class FooBar : IFoo, IBar
{
    int IFoo.FooBarProp { get; set; }
    public int Foo
    {
        get => ((IFoo)this).FooBarProp;
        set => ((IFoo)this).FooBarProp = value;
    }
    int IBar.FooBarProp { get; set; }
    public int Bar
    {
        get => ((IBar)this).FooBarProp;
        set => ((IBar)this).FooBarProp = value;
    }
}");
        }

        [Fact]
        public async Task RenamedInterfaceMethodFullyQualifiedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace TestNamespace
    Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface
End Namespace

Public Class Foo
    Implements TestNamespace.IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements TestNamespace.IFoo.DoFoo
        Return 4
    End Function
End Class", @"
namespace TestNamespace
{
    public partial interface IFoo
    {
        int DoFoo(ref string str, int i);
    }
}

public partial class Foo : TestNamespace.IFoo
{
    int TestNamespace.IFoo.DoFoo(ref string str, int i)
    {
        return 4;
    }

    public int DoFooRenamed(ref string str, int i) => ((TestNamespace.IFoo)this).DoFoo(ref str, i);
}");
        }

        [Fact]
        public async Task RenamedInterfacePropertyFullyQualifiedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace TestNamespace
    Public Interface IFoo
        Property FooProp As Integer
    End Interface
End Namespace

Public Class Foo
    Implements TestNamespace.IFoo

    Property FooPropRenamed As Integer Implements TestNamespace.IFoo.FooProp
    
End Class", @"
namespace TestNamespace
{
    public partial interface IFoo
    {
        int FooProp { get; set; }
    }
}

public partial class Foo : TestNamespace.IFoo
{
    int TestNamespace.IFoo.FooProp { get; set; }
    public int FooPropRenamed
    {
        get => ((TestNamespace.IFoo)this).FooProp;
        set => ((TestNamespace.IFoo)this).FooProp = value;
    }
}");
        }

        [Fact]
        public async Task RenamedInterfaceMethodConsumerCasingRenamedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Return foo.DOFOORENAMED(str, i)
    End Function
End Class", @"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{
    int IFoo.DoFoo(ref string str, int i)
    {
        return 4;
    }

    public int DoFooRenamed(ref string str, int i) => ((IFoo)this).DoFoo(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        return foo.DoFooRenamed(ref str, i);
    }
}");
        }

        [Fact]
        public async Task RenamedInterfacePropertyConsumerCasingRenamedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp
    
End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Return foo.FOOPROPRENAMED
    End Function
End Class", @"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{
    int IFoo.FooProp { get; set; }
    public int FooPropRenamed
    {
        get => ((IFoo)this).FooProp;
        set => ((IFoo)this).FooProp = value;
    }
}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        return foo.FooPropRenamed;
    }
}");
        }

        [Fact]
        public async Task InterfaceMethodCasingRenamedConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function dofoo(str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(str As String, i As Integer) As Integer
        Dim foo As New Foo
        Return foo.dofoo(str, i)
    End Function
End Class", @"
public partial interface IFoo
{
    int DoFoo(string str, int i);
}

public partial class Foo : IFoo
{
    int IFoo.DoFoo(string str, int i)
    {
        return 4;
    }

    public int dofoo(string str, int i) => ((IFoo)this).DoFoo(str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(string str, int i)
    {
        var foo = new Foo();
        return foo.dofoo(str, i);
    }
}");
        }

        [Fact]
        public async Task InterfacePropertyCasingRenamedConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property fooprop As Integer Implements IFoo.FooProp
    
End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Return foo.fooprop
    End Function
End Class", @"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{
    int IFoo.FooProp { get; set; }
    public int fooprop
    {
        get => ((IFoo)this).FooProp;
        set => ((IFoo)this).FooProp = value;
    }
}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        return foo.fooprop;
    }
}");
        }

        [Fact]
        public async Task InterfaceRenamedMethodConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Return foo.DoFooRenamed(str, i)
    End Function
End Class", @"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{
    int IFoo.DoFoo(ref string str, int i)
    {
        return 4;
    }

    public int DoFooRenamed(ref string str, int i) => ((IFoo)this).DoFoo(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        return foo.DoFooRenamed(ref str, i);
    }
}");
        }

        [Fact]
        public async Task InterfaceRenamedPropertyConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp
    
End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Return foo.FooPropRenamed
    End Function
End Class", @"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{
    int IFoo.FooProp { get; set; }
    public int FooPropRenamed
    {
        get => ((IFoo)this).FooProp;
        set => ((IFoo)this).FooProp = value;
    }
}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        return foo.FooPropRenamed;
    }
}");
        }

        [Fact]
        public async Task PartialInterfaceRenamedMethodConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Partial Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Return foo.DoFooRenamed(str, i)
    End Function
End Class", @"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{
    int IFoo.DoFoo(ref string str, int i)
    {
        return 4;
    }

    public int DoFooRenamed(ref string str, int i) => ((IFoo)this).DoFoo(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        return foo.DoFooRenamed(ref str, i);
    }
}");
        }

        [Fact]
        public async Task PartialInterfaceRenamedPropertyConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Partial Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp
    
End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Return foo.FooPropRenamed
    End Function
End Class", @"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{
    int IFoo.FooProp { get; set; }
    public int FooPropRenamed
    {
        get => ((IFoo)this).FooProp;
        set => ((IFoo)this).FooProp = value;
    }
}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        return foo.FooPropRenamed;
    }
}");
        }

        [Fact]
        public async Task RenamedInterfaceMethodMyClassConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Overridable Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo ' Comment ends up out of order, but attached to correct method
        Return 4
    End Function

    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Return MyClass.DoFooRenamed(str, i)
    End Function
End Class", @"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{
    public int MyClassDoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    public virtual int DoFooRenamed(ref string str, int i) => ((IFoo)this).DoFoo(ref str, i);
    int IFoo.DoFoo(ref string str, int i) => MyClassDoFooRenamed(ref str, i); // Comment ends up out of order, but attached to correct method

    public int DoFooRenamedConsumer(ref string str, int i)
    {
        return MyClassDoFooRenamed(ref str, i);
    }
}");
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
        public async Task PrivateAutoPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp As Integer
End Interface

Public Interface IBar
    Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    Private Property ExplicitProp As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
End Class", @"
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}

public partial class Foo : IFoo, IBar
{
    int IFoo.ExplicitProp { get; set; }
    int IBar.ExplicitProp
    {
        get => ((IFoo)this).ExplicitProp;
        set => ((IFoo)this).ExplicitProp = value;
    }
}");
        }

        [Fact]
        public async Task PrivatePropertyAccessorBlocksImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp As Integer
End Interface

Public Interface IBar
    Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    Private Property ExplicitProp As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp ' Comment moves because this line gets split
        Get
          Return 5
        End Get
        Set
        End Set
    End Property
End Class", @"
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}

public partial class Foo : IFoo, IBar
{
    int IFoo.ExplicitProp
    {
        get
        {
            return 5;
        }

        set
        {
        }
    }

    int IBar.ExplicitProp
    {
        get => ((IFoo)this).ExplicitProp;
        set => ((IFoo)this).ExplicitProp = value; // Comment moves because this line gets split
    }
}");
        }

        [Fact]
        public async Task ReadonlyRenamedPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    ReadOnly Property ExplicitProp As Integer
End Interface

Public Interface IBar
    ReadOnly Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    ReadOnly Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
End Class", @"
public partial interface IFoo
{
    int ExplicitProp { get; }
}

public partial interface IBar
{
    int ExplicitProp { get; }
}

public partial class Foo : IFoo, IBar
{
    int IFoo.ExplicitProp { get; }
    int IBar.ExplicitProp
    {
        get => ((IFoo)this).ExplicitProp;
    }
    public int ExplicitPropRenamed
    {
        get => ((IFoo)this).ExplicitProp;
    }
}");
        }

        [Fact]
        public async Task WriteonlyPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    WriteOnly Property ExplicitProp As Integer
End Interface

Public Interface IBar
    WriteOnly Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    WriteOnly Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp ' Comment moves because this line gets split
        Set
        End Set        
    End Property
End Class", @"
public partial interface IFoo
{
    int ExplicitProp { set; }
}

public partial interface IBar
{
    int ExplicitProp { set; }
}

public partial class Foo : IFoo, IBar
{
    int IFoo.ExplicitProp
    {
        set
        {
        }
    }

    int IBar.ExplicitProp
    {
        set => ((IFoo)this).ExplicitProp = value;
    }
    public int ExplicitPropRenamed
    {
        set => ((IFoo)this).ExplicitProp = value; // Comment moves because this line gets split
    }
}");
        }

        [Fact]
        public async Task PrivateMethodAndParameterizedPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Interface IBar
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    Private Function ExplicitFunc(ByRef str As String, i As Integer) As Integer Implements IFoo.ExplicitFunc, IBar.ExplicitFunc
        Return 5
    End Function
    
    Private Property ExplicitProp(str As String) As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
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

public partial interface IBar
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial class Foo : IFoo, IBar
{
    int IFoo.ExplicitFunc(ref string str, int i)
    {
        return 5;
    }

    int IBar.ExplicitFunc(ref string str, int i) => ((IFoo)this).ExplicitFunc(ref str, i);

    int IFoo.get_ExplicitProp(string str)
    {
        return 5;
    }

    void IFoo.set_ExplicitProp(string str, int value)
    {
    }

    int IBar.get_ExplicitProp(string str) => ((IFoo)this).get_ExplicitProp(str);
    void IBar.set_ExplicitProp(string str, int value) => ((IFoo)this).set_ExplicitProp(str, value);
}");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationOptionalParametersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
  Property ExplicitProp(Optional str As String = """") As Integer
  Function ExplicitFunc(Optional str2 As String = """", Optional i2 As Integer = 1) As Integer
End Interface

Public Class Foo
  Implements IFoo

  Private Function ExplicitFunc(Optional str As String = """", Optional i2 As Integer = 1) As Integer Implements IFoo.ExplicitFunc
    Return 5
  End Function
    
  Private Property ExplicitProp(Optional str As String = """") As Integer Implements IFoo.ExplicitProp
    Get
      Return 5
    End Get
    Set(value As Integer)
    End Set
  End Property
End Class", @"
public partial interface IFoo
{
    int get_ExplicitProp(string str = """");
    void set_ExplicitProp(string str = """", int value = default);
    int ExplicitFunc(string str2 = """", int i2 = 1);
}

public partial class Foo : IFoo
{
    int IFoo.ExplicitFunc(string str, int i2)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Issue444_InternalMemberDelegatingMethodAsync()
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
    int IFoo.FooDifferentName(ref string str, int i)
    {
        return 4;
    }

    public int BarDifferentName(ref string str, int i) => ((IFoo)this).FooDifferentName(ref str, i);
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

        [Fact]
        public async Task TestConstructorStaticLocalConvertedToFieldAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class StaticLocalConvertedToField
    Sub New(x As Boolean)
        Static sPrevPosition As Integer = 7 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Sub New(x As Integer)
        Static sPrevPosition As Integer
        Console.WriteLine(sPrevPosition)
    End Sub
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _sPrevPosition = 7; // Comment moves with declaration

    public StaticLocalConvertedToField(bool x)
    {
        Console.WriteLine(_sPrevPosition);
    }

    private int _sPrevPosition1 = default;

    public StaticLocalConvertedToField(int x)
    {
        Console.WriteLine(_sPrevPosition1);
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
}");
        }
    }
}
