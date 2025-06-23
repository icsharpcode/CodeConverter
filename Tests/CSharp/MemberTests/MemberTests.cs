using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class MemberTests : ConverterTestBase
{

    [Fact]
    public async Task TestFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private const int answer = 42;
    private int value = 10;
    private readonly int v = 15;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMultiArrayFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private string[] Parts, Taxes;
    private string[][] Deposits, Prepaid;
    private string FromDate, ToDate;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestConstantFieldInModuleAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class TestModule
{
    private const int answer = 42;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestDeclareMethodVisibilityInModuleAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Runtime.InteropServices;

internal static partial class Module1
{
    [DllImport(""lib.dll"")]
    public static extern void External();
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestDeclareMethodVisibilityInClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Runtime.InteropServices;

internal partial class Class1
{
    [DllImport(""lib.dll"")]
    public static extern void External();
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestTypeInferredConstAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private const int someConstField = 42;
    public void TestMethod()
    {
        const DateTimeKind someConst = DateTimeKind.Local;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestTypeInferredVarAsync()
    {
        // VB doesn't infer the type of EnumVariable like you'd think, it just uses object
        // VB compiler uses Conversions rather than any plainer casting
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodAssignmentReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodAssignmentReturn293Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
CS0115: 'Class1.Foo()': no suitable method found to override", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodAssignmentAdditionReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodMissingReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class Class1
{
    public int TestMethod()
    {
        return default;

    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodWithOutParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

internal partial class TestClass
{
    public bool TryGet(out List<string> strs)
    {
        strs = new List<string>();
        return false;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMethodWithReturnTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestFunctionWithNoReturnTypeSpecifiedAsync()
    {
        // Note: "Inferred" type is always object except with local variables
        // https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/variables/local-type-inference
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private object TurnFirstToUp(string Text)
    {
        string firstCharacter = Text.Substring(0, 1).ToUpper();
        return firstCharacter + Text.Substring(1);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestFunctionReturningTypeRequiringConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public string Four()
    {
        return 4.ToString();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestStaticMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestAbstractMethodAndPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal abstract partial class TestClass
{
    public abstract void TestMethod();
    public abstract string AbstractProperty { get; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestAbstractReadOnlyAndWriteOnlyPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SetterProperty1053Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StaticLocalsInPropertyGetterAndSetterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestSealedMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
CS0238: 'TestClass.TestMethod<T, T2, T3>(out T, ref T2, T3)' cannot be sealed because it is not an override", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestShadowedMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task TestDestructorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    ~TestClass()
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue681_OverloadsOverridesPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public partial class C : B
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PartialFriendClassWithOverloadsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ClassWithGloballyQualifiedAttributeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Diagnostics;

[DebuggerDisplay(""Hello World"")]
internal partial class TestClass
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task FieldWithAttributeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    [ThreadStatic]
    private static int First;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task FieldWithNonStaticInitializerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class A
{
    private int x = 2;
    private int[] y;

    public A()
    {
        y = new int[x + 1];
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task FieldWithInstanceOperationOfDifferingTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Threading.Tasks;

public partial class DoesNotNeedConstructor
{
    private readonly ParallelOptions ClassVariable1 = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue281FieldWithNonStaticLambdaInitializerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ParamArrayAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void SomeBools(params bool[] anyName)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ParamNamedBoolAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void SomeBools(params bool[] @bool)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MethodWithNameArrayParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public void DoNothing(string[] strs)
    {
        string[] moreStrs;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UntypedParametersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public void DoNothing(object obj, object[] objs)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PartialClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NestedClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LessQualifiedNestedClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestAsyncMethodsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Threading.Tasks;

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
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestAsyncMethodsWithNoReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExternDllImportAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"[DllImport(""kernel32.dll"", SetLastError = true)]
private static extern IntPtr OpenProcess(AccessMask dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

1 source compilation errors:
BC30002: Type 'AccessMask' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'AccessMask' could not be found (are you missing a using directive or an assembly reference?)", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue420_RenameClashingClassMemberAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class MainType
{
    public static void Main()
    {
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue420_DoNotRenameClashingEnumMemberAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal enum MyEnum
{
    MyEnumFirst,
    MyEnum
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue420_DoNotRenameClashingEnumMemberForPublicEnumAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public enum MyEnum
{
    MyEnumFirst,
    MyEnum
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestPropertyStaticLocalConvertedToFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestStaticLocalConvertedToFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestStaticLocalWithoutDefaultInitializerConvertedToFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition;
    public int OtherName()
    {
        _OtherName_sPrevPosition = 23;
        Console.WriteLine(_OtherName_sPrevPosition);
        return _OtherName_sPrevPosition;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestModuleStaticLocalConvertedToStaticFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestStaticLocalConvertedToStaticFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestOmittedArgumentsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Runtime.InteropServices;

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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestRefConstArgumentAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestRefFunctionCallNoParenthesesArgumentAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestMissingByRefArgumentWithNoExplicitDefaultValueAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Runtime.InteropServices;

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
CS7036: There is no argument given that corresponds to the required formal parameter 'str1' of 'MissingByRefArgumentWithNoExplicitDefaultValue.ByRefNoDefault(ref string)'
", extension: "cs")
            );
        }
    }
}