using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task ComparingStringsUsesCoerceToNonNullOnlyWhenNeededAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(string a)
    {
        bool result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = string.IsNullOrEmpty(a);
        result = a is null;
        result = a is not null;
        result = a is null;
        result = (a ?? """") == (a ?? """");
        result = a == ""test"";
        result = ""test"" == a;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DynamicTestAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public bool IsPointWithinBoundaryBox(double dblLat, double dblLon, object boundbox)
    {
        if (boundbox is not null)
        {
            bool boolInLatBounds = Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectLessEqual(dblLat, ((dynamic)boundbox).north, false), Operators.ConditionalCompareObjectGreaterEqual(dblLat, ((dynamic)boundbox).south, false))); // Less then highest (northmost) lat, AND more than lowest (southmost) lat
            bool boolInLonBounds = Conversions.ToBoolean(Operators.AndObject(Operators.ConditionalCompareObjectGreaterEqual(dblLon, ((dynamic)boundbox).west, false), Operators.ConditionalCompareObjectLessEqual(dblLon, ((dynamic)boundbox).east, false))); // More than lowest (westmost) lat, AND less than highest (eastmost) lon
            return boolInLatBounds & boolInLonBounds;
        }
        else
        {
            // Throw New Exception(""boundbox is null."")
        }
        return false;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DynamicAccessAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class TestDynamicUsage
{
    public int Prop { get; set; }

    public void S()
    {
        object o;
        o = new TestDynamicUsage();
        ((dynamic)o).Prop = 1; // Must not cast to object here
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DynamicBoolAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public bool IsHybridApp()
    {
        return Conversions.ToBoolean(((dynamic)new object()).Session(""hybrid"") is not null && Operators.ConditionalCompareObjectEqual(((dynamic)new object()).Session(""hybrid""), 1, false));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConversionOfNotUsesParensIfNeededAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool rslt = !(1 == 2);
        bool rslt2 = !true;
        bool rslt3 = !(new object() is bool);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DateLiteralsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class TestClass
{
    private void TestMethod([Optional, DateTimeConstant(599266080000000000L/* #1/1/1900# */)] DateTime pDate)
    {
        var rslt = DateTime.Parse(""1900-01-01"");
        var rslt2 = DateTime.Parse(""2002-08-13 12:14:00"");
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ImplicitCastToDoubleLiteralAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class DoubleLiteral
{
    private double Test(double myDouble)
    {
        return Test(2.37d) + Test(255d); // VB: D means decimal, C#: D means double
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DateConstsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public partial class Issue213
{
    private static DateTime x = DateTime.Parse(""1990-01-01"");

    private void Y([Optional, DateTimeConstant(627667488000000000L/* Global.Issue213.x */)] DateTime opt)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MethodCallWithImplicitConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        Bar(Conversions.ToInteger(true));
        Bar(Conversions.ToInteger(""4""));
        var ss = new string[2];
        string y = ss[Conversions.ToInteger(""0"")];
    }

    public void Bar(int x)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue580_EnumCastsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class EnumToString
{
    public enum Tes : short
    {
        None = 0,
        TEST2 = 2
    }
    private void TEest2(Tes aEnum)
    {
        string sxtr_Tmp = ""Use"" + ((short)aEnum).ToString();
        short si_Txt = (short)Math.Round(Math.Pow(2d, (double)Tes.TEST2));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IntToEnumArgAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo(TriState arg)
    {
    }

    public void Main()
    {
        Foo(0);
    }
}", extension: "cs")
            );
        }
    }

    [Fact] // https://github.com/icsharpcode/CodeConverter/issues/636
    public async Task CharacterizeCompilationErrorsWithLateBoundImplicitObjectNarrowingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class VisualBasicClass
{
    public void Rounding()
    {
        object o = 3.0f;
        var x = Math.Round(o, 2);
    }
}
1 target compilation errors:
CS1503: Argument 1: cannot convert from 'object' to 'double'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EnumToIntCastAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class MyTest
{
    public enum TestEnum : int
    {
        Test1 = 0,
        Test2 = 1
    }

    public void Main()
    {
        var EnumVariable = TestEnum.Test1;
        int t1 = (int)EnumVariable;
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task FlagsEnumAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

[Flags()]
public enum FilePermissions : int
{
    None = 0,
    Create = 1,
    Read = 2,
    Update = 4,
    Delete = 8
}

public partial class MyTest
{
    public FilePermissions MyEnum = (FilePermissions)((int)FilePermissions.None + (int)FilePermissions.Create);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EnumSwitchAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Class1
{
    public enum E
    {
        A
    }

    public void Main()
    {
        var e1 = E.A;
        var e2 = default(int);
        switch (e1)
        {
            case 0:
                {
                    break;
                }
        }

        switch (e2)
        {
            case (int)E.A:
                {
                    break;
                }
        }

    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DuplicateCaseDiscardedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal static partial class Module1
{
    public static void Main()
    {
        switch (1)
        {
            case 1:
                {
                    Console.WriteLine(""a"");
                    break;
                }

            case var @case when @case == 1:
                {
                    Console.WriteLine(""b"");
                    break;
                }

        }

    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
        //BUG: Correct textual output, but requires var pattern syntax construct not available before CodeAnalysis 3
    }

    [Fact]
    public async Task MethodCallWithoutParensAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Class1
{
    public void Foo()
    {
        int w = Bar();
        int x = Bar();
        int y = Baz;
        int z = Baz;
    }

    public int Bar()
    {
        return 1;
    }
    public int Baz { get; set; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConversionOfCTypeUsesParensIfNeededAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string rslt = true.ToString();
        object rslt2 = true;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DateKeywordAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private DateTime DefaultDate = default;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IfNothingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass
{
    private object SomeDate = """";
    private DateTime? SomeDateDateNothing;
    private object isNotNothing;
    private object isSomething;

    public VisualBasicClass()
    {
        SomeDateDateNothing = string.IsNullOrEmpty(Conversions.ToString(SomeDate)) ? default : DateTime.Parse(Conversions.ToString(SomeDate));
        isNotNothing = SomeDateDateNothing is not null;
        isSomething = new DateTime() is var arg1 && SomeDateDateNothing.HasValue ? SomeDateDateNothing.Value == arg1 : (bool?)null;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task CTypeNothingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class VisualBasicClass
{
    private string SomeDate = ""2022-01-01"";
    private DateTime? SomeDateDateParsed;

    public VisualBasicClass()
    {
        SomeDateDateParsed = string.IsNullOrEmpty(SomeDate) ? default(DateTime?) : DateTime.Parse(SomeDate);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NullConditionalIndexer_Issue993Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class VisualBasicClass
{

    private bool TestMethod(object[] testArray)
    {
        return !string.IsNullOrWhiteSpace(testArray?[0]?.ToString());
    }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task GenericComparisonAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class GenericComparison
{
    public void m<T>(T p)
    {
        if (p is null)
            return;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AccessSharedThroughInstanceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class A
{
    public static int x = 2;
    public void Test()
    {
        var tmp = this;
        int y = x;
        int z = x;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EmptyArrayExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;

public partial class Issue495AndIssue713
{
    public int[] Empty()
    {
        IEnumerable<int> emptySingle = Array.Empty<int>();
        IEnumerable<int> initializedSingle = new[] { 1 };
        int[][] emptyNested = Array.Empty<int[]>();
        var initializedNested = new int[2][];
        int[,] empty2d = new int[,] { { } };
        int[,] initialized2d = new[,] { { 1 } };
        return Array.Empty<int>();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task InitializedArrayExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;

public partial class Issue713
{
    public int[] Empty()
    {
        IEnumerable<int> initializedSingle = new[] { 1 };
        int[,] initialized2d = new[,] { { 1 } };
        return Array.Empty<int>();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EmptyArrayParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void s()
    {
        if (Validate(Array.Empty<short>()))
        {
        }
    }
    private bool Validate(IEnumerable<short> w)
    {
        return true;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Empty2DArrayExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Empty2DArray
{
    private double[,] data = new double[,] { };
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ReducedTypeParametersInferrableAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ReducedTypeParametersNonInferrableAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select<string, object>(x => x);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EnumNullableConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Class1
{
    public void Main()
    {
        var x = DayOfWeek.Monday;
        Foo(x);
    }

    public void Foo(DayOfWeek? x)
    {

    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UninitializedVariableAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Class1
{
    public Class1()
    {
        var needsInitialization = default(int);
        int notUsed;
        int y = needsInitialization;
    }

    public void Foo()
    {
        var needsInitialization = default(int);
        int notUsed;
        int y = needsInitialization;
    }

    public void Bar()
    {
        var i = default(int);
        string temp = string.Empty;
        i += 1;
    }

    public void Bar2()
    {
        var i = default(int);
        string temp = string.Empty;
        i = i + 1;
    }

    public void Bar3()
    {
        var i = default(int);
        string temp = string.Empty;
        int k = i + 1;
    }

    public void Bar4()
    {
        var i = default(int);
        string temp = string.Empty;
        int k = i + 1;
        i = 1;
    }

    public int State
    {
        get
        {
            var needsInitialization = default(int);
            int notUsed;
            int y = needsInitialization;
            return y;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task FullyTypeInferredEnumerableCreationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string[] strings = new[] { ""1"", ""2"" };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task GetTypeExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        var typ = typeof(string);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NullableIntegerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public int? Bar(string value)
    {
        int result;
        if (int.TryParse(value, out result))
        {
            return result;
        }
        else
        {
            return default;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NothingInvokesDefaultForValueTypesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    public void Bar()
    {
        int number;
        number = default;
        DateTime dat;
        dat = default;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConditionalExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        bool result = string.IsNullOrEmpty(str) ? true : false;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConditionalExpressionInStringConcatAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class ConditionalExpressionInStringConcat
{
    private void TestMethod(string str)
    {
        int appleCount = 42;
        Console.WriteLine(""I have "" + appleCount + (appleCount == 1 ? "" apple"" : "" apples""));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConditionalExpressionInUnaryExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        bool result = !(string.IsNullOrEmpty(str) ? true : false);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NullCoalescingExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OmittedArgumentInInvocationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public static partial class MyExtensions
{
    public static void NewColumn(Type @type, string strV1 = null, string code = ""code"", int argInt = 1)
    {
    }

    public static void CallNewColumn()
    {
        NewColumn(typeof(MyExtensions));
        NewColumn(null, code: ""otherCode"");
        NewColumn(null, ""fred"");
        NewColumn(null, argInt: 2);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OmittedArgumentInCallInvocationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Issue445MissingParameter
{
    public void First(string a, string b, int c)
    {
        mySuperFunction(7, optionalSomething: new object());
    }

    private void mySuperFunction(int intSomething, object p = null, object optionalSomething = null)
    {
        throw new NotImplementedException();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExternalReferenceToOutParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var d = new Dictionary<string, string>();
        string s;
        d.TryGetValue(""a"", out s);
    }
}", extension: "cs")
            );
        }
    }

        
    [Fact]
    public async Task ExternalReferenceToOutParameterFromInterfaceImplementationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections;
using System.Collections.Generic;

internal abstract partial class TestClass : IReadOnlyDictionary<int, int>
{
    public bool TryGetValue(int key, out int value)
    {
        value = key;
        return default;
    }

    private void TestMethod()
    {
        var value = default(int);
        TryGetValue(5, out value);
    }

    public abstract bool ContainsKey(int key);
    public abstract IEnumerator<KeyValuePair<int, int>> GetEnumerator();
    public abstract IEnumerator IEnumerable_GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => IEnumerable_GetEnumerator();
    public abstract int this[int key] { get; }
    public abstract IEnumerable<int> Keys { get; }
    public abstract IEnumerable<int> Values { get; }
    public abstract int Count { get; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ElvisOperatorExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass3
{
    private partial class Rec
    {
        public Rec Prop { get; private set; } = new Rec();
    }
    private Rec TestMethod(string str)
    {
        int length = (str?.Length) ?? -1;
        Console.WriteLine(length);
        Console.ReadKey();
        return new Rec()?.Prop?.Prop?.Prop;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ObjectInitializerExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class StudentName
{
    public string LastName, FirstName;
}

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var student2 = new StudentName() { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ObjectInitializerWithInferredNameAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class Issue480
{
    public int Foo;

    public void Test()
    {
        var x = new { Foo };
    }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue949_AnonymousWithBlockMemberSelfAccessAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"{
    var anonymousType1 = new
    {
        A = 1 is var tempA ? tempA : default,
        B = tempA
    };
    var anonymousType2 = new
    {
        A = 2 is var tempA1 ? tempA1 : default,
        B = tempA1
    };
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue949_AnonymousWithNestedBlockMemberSelfAccessAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"{
    var anonymousType = new
    {
        A = 1, // Comment gets duplicated
               // Comment gets duplicated
        B = new
        {
            A = 2 is var tempA ? tempA : default,
            B = tempA
        }
    };
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ObjectInitializerExpression2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var student2 = new { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithObjectInitializerCanReadFromPropertiesOfObjectAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class SomeClass
{
    public string SomeProperty;
    static SomeClass initInstance()
    {
        var init = new SomeClass();
        return (init.SomeProperty = init.SomeProperty + nameof(init.SomeProperty), init).init; // Second line gets moved
    } // Third line gets moved

    public static SomeClass Instance = initInstance(); // First line gets moved
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task CollectionInitializersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

internal partial class TestClass
{
    private void DoStuff(object a)
    {
    }
    private void TestMethod()
    {
        DoStuff(new[] { 1, 2 });
        var intList = new List<int>() { 1 };
        var dict = new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DelegateExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = (a) => a * 2;
        test(3);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue1148_AddressOfSignatureCompatibilityAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
using System;

public partial class Issue1148
{
    public static Func<TestObjClass> FuncClass = FunctionReturningClass;
    public static Func<TestBaseObjClass> FuncBaseClass = FunctionReturningClass;
    public static Func<ITestObj> FuncInterface = FunctionReturningClass;
    public static Func<ITestObj, ITestObj> FuncInterfaceParam = CastObj;
    public static Func<TestObjClass, ITestObj> FuncClassParam = CastObj;

    public static TestObjClass FunctionReturningClass()
    {
        return new TestObjClass();
    }

    public static TestObjClass CastObj(ITestObj obj)
    {
        return (TestObjClass)obj;
    }

}

public partial class TestObjClass : TestBaseObjClass, ITestObj
{
}

public partial class TestBaseObjClass
{
}

public partial interface ITestObj
{
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LambdaImmediatelyExecutedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Issue869
{
    public void Main()
    {
        int i = new Func<int>(() => 2)();


        Console.WriteLine(i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LambdaBodyExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = a => a * 2;
        Func<int, int, double> test2 = (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0d;
        };

        Func<int, int, int> test3 = (a, b) => a % b;
        test(3);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AsyncLambdaBodyExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Threading.Tasks;

internal partial class TestClass
{
    private async void TestMethod()
    {
        Func<Task<int>> test0 = async () => 2;
        Func<int, Task<int>> test1 = async a => a * 2;
        Func<int, int, Task<double>> test2 = async (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0d;
        };

        Func<int, int, Task<int>> test3 = async (a, b) => a % b;
        Func<Task<int>> test4 = async () =>
        {
            int i = 2;
            int x = 3;
            return 3;
        };

        await test1(3);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AsyncLambdaParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Threading.Tasks;

internal partial class TestClass
{
    public async Task<bool> mySub()
    {
        return await ExecuteAuthenticatedAsync(async () => await DoSomethingAsync());

    }
    private async Task<bool> ExecuteAuthenticatedAsync(Func<Task<bool>> myFunc)
    {
        return await myFunc();
    }
    private async Task<bool> DoSomethingAsync()
    {
        return true;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TypeInferredLambdaBodyExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private void TestMethod()
    {
        object test(object a) => Operators.MultiplyObject(a, 2);
        object test2(object a, object b)
        {
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectGreater(b, 0, false)))
                return Operators.DivideObject(a, b);
            return 0;
        };

        object test3(object a, object b) => Operators.ModObject(a, b);
        test(3);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue316_LambdaExpressionEqualityCheckAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Linq.Expressions;

internal partial class TestClass
{
    private void TestMethod(string a, string b)
    {
        Expression<Func<bool>> test = () => a == b;
        test.Compile()();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue930_LambdaExpressionEqualityCheckAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Linq.Expressions;

internal partial class TestClass
{
    private void TestMethod(object a)
    {
        Expression<Func<bool>> test = () => a == default;
        test.Compile()();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SingleLineLambdaWithStatementBodyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int x = 1;
        Action simpleAssignmentAction = () => x = 1;
        Action nonBlockAction = () => Console.WriteLine(""Statement"");
        Action ifAction = () => { if (true) return; };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AnonymousLambdaArrayTypeConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class TargetTypeTestClass
{

    private static void Main()
    {
        Action[] actions = new[] { new Action(() => Debug.Print(1.ToString())), new Action(() => Debug.Print(2.ToString())) };
        var objects = new List<object>() { new Action(() => Debug.Print(3.ToString())), new Action(() => Debug.Print(4.ToString())) };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AnonymousLambdaTypeConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class AnonymousLambdaTypeConversionTest
{
    public void CallThing(Delegate thingToCall)
    {
    }

    public void SomeMethod()
    {
    }

    public void Foo()
    {
        CallThing(new Action(() => SomeMethod()));
        CallThing(new Action<object>(a => SomeMethod()));
        CallThing(new Func<bool>(() =>
        {
            SomeMethod();
            return false;
        }));
        CallThing(new Func<object, bool>(a => false));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AwaitAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Threading.Tasks;

internal partial class TestClass
{
    private Task<int> SomeAsyncMethod()
    {
        return Task.FromResult(0);
    }

    private async void TestMethod()
    {
        int result = await SomeAsyncMethod();
        Console.WriteLine(result);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NameQualifyingHandlesInheritanceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClassBase
{
    public void DoStuff()
    {
    }
}
internal partial class TestClass : TestClassBase
{
    private void TestMethod()
    {
        DoStuff();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UsingGlobalImportAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public string TestMethod()
    {
        return Constants.vbCrLf;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ValueCapitalisationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public enum TestState
{
    one,
    two
}

public partial class test
{
    private TestState _state;
    public TestState State
    {
        get
        {
            return _state;
        }
        set
        {
            if (!_state.Equals(value))
            {
                _state = value;
            }
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConstLiteralConversionIssue329Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class Module1
{
    private const bool a = true;
    private const char b = '\u0001';
    private const float c = 1f;
    private const double d = 1d;
    private const decimal e = 1m;
    private const sbyte f = 1;
    private const short g = 1;
    private const int h = 1;
    private const long i = 1L;
    private const byte j = 1;
    private const uint k = 1U;
    private const ushort l = 1;
    private const ulong m = 1UL;
    private const string Nl = ""\r\n"";

    public static void Main()
    {
        const sbyte x = 4;
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseIssue361Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal static partial class Module1
{
    public enum E
    {
        A = 1
    }

    public static void Main()
    {
        int x = 1;
        switch (x)
        {
            case (int)E.A:
                {
                    Console.WriteLine(""z"");
                    break;
                }
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseIssue675Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class EnumTest
{
    public enum UserInterface
    {
        Unknown,
        Spectrum,
        Wisdom
    }

    public void OnLoad(UserInterface? ui)
    {
        int activity = 0;
        switch (ui)
        {
            case object _ when ui is null:
                {
                    activity = 1;
                    break;
                }
            case UserInterface.Spectrum:
                {
                    activity = 2;
                    break;
                }
            case UserInterface.Wisdom:
                {
                    activity = 3;
                    break;
                }

            default:
                {
                    activity = 4;
                    break;
                }
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseDoesntGenerateBreakWhenLastStatementWillExitAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Test
{
    public int OnLoad()
    {
        int x = 5;
        while (true)
        {
            switch (x)
            {
                case 0:
                    {
                        continue;
                    }
                case 1:
                    {
                        x = 1;
                        break;
                    }
                case 2:
                    {
                        return 2;
                    }
                case 3:
                    {
                        throw new Exception();
                    }
                case 4:
                    {
                        if (true)
                        {
                            x = 4;
                        }
                        else
                        {
                            return x;
                        }

                        break;
                    }
                case 5:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else
                        {
                            x = 5;
                        }

                        break;
                    }
                case 6:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            x = 6;
                        }
                        else
                        {
                            return x;
                        }

                        break;
                    }
                case 7:
                    {
                        if (true)
                        {
                            return x;
                        }

                        break;
                    }
                case 8:
                    {
                        if (true)
                            return x;
                        break;
                    }
                case 9:
                    {
                        if (true)
                            x = 9;
                        break;
                    }
                case 10:
                    {
                        if (true)
                            return x;
                        else
                            x = 10;
                        break;
                    }
                case 11:
                    {
                        if (true)
                            x = 11;
                        else
                            return x;
                        break;
                    }
                case 12:
                    {
                        if (true)
                            return x;
                        else
                            return x;
                    }
                case 13:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            continue;
                        }
                        else if (false)
                        {
                            throw new Exception();
                        }
                        else if (false)
                        {
                            break;
                        }
                        else
                        {
                            return x;
                        }
                    }
                case 14:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            return x;
                        }
                        else if (false)
                        {
                            break;
                        }

                        break;
                    }

                default:
                    {
                        if (true)
                        {
                            return x;
                        }
                        else
                        {
                            return x;
                        }
                    }
            }
        }
        return x;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseObjectCaseIntegerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class SelectObjectCaseIntegerTest
{
    public void S()
    {
        object o;
        int j;
        o = 2.0d;
        switch (o)
        {
            case var @case when Operators.ConditionalCompareObjectEqual(@case, 1, false):
                {
                    j = 1;
                    break;
                }
            case var case1 when Operators.ConditionalCompareObjectEqual(case1, 2, false):
                {
                    j = 2;
                    break;
                }
            case var case2 when Operators.ConditionalCompareObjectLessEqual(3, case2, false) && Operators.ConditionalCompareObjectLessEqual(case2, 4, false):
                {
                    j = 3;
                    break;
                }
            case var case3 when Operators.ConditionalCompareObjectGreater(case3, 4, false):
                {
                    j = 4;
                    break;
                }

            default:
                {
                    j = -1;
                    break;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
        //BUG: Correct textual output, but requires var pattern syntax construct not available before CodeAnalysis 3
    }

    [Fact]
    public async Task TupleAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public bool GetString(bool yourBoolean)
{
    return 1 != 1 || (yourBoolean ? true : false);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UseEventBackingFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Diagnostics;

public partial class Foo
{
    public event EventHandler<EventArgs> Bar;

    protected void OnBar(EventArgs e)
    {
        if (Bar is null)
        {
            Debug.WriteLine(""No subscriber"");
        }
        else
        {
            Bar?.Invoke(this, e);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DateTimeToDateAndTimeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        var x = DateAndTime.DateAdd(""m"", 5d, DateTime.Now);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task BaseFinalizeRemovedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Class1
{
    ~Class1()
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task GlobalNameIssue375Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal static partial class Module1
{
    public static void Main()
    {
        double x = DateAndTime.Timer;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TernaryConversionIssue363Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class Module1
{
    public static void Main()
    {
        short x = true ? (short)50 : (short)100;
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task GenericMethodCalledWithAnonymousTypeAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;

public partial class MoreParsing
{
    public void DoGet()
    {
        var anon = new { ANumber = 5 };
        var sameAnon = Identity(anon);
        var repeated = Enumerable.Repeat(anon, 5).ToList();
    }

    private TType Identity<TType>(TType tInstance)
    {
        return tInstance;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DecimalToIntegerCompoundOperatorsWithTypeConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Compound
{
    public void Operators()
    {
        int anInt = 123;
        decimal aDec = 12.3m;
        anInt = (int)Math.Round(anInt * aDec);
        anInt = (int)(anInt / (long)Math.Round(aDec));
        anInt = (int)Math.Round(anInt / aDec);
        anInt = (int)Math.Round(anInt - aDec);
        anInt = (int)Math.Round(anInt + aDec);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DecimalToShortCompoundOperatorsWithTypeConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Compound
{
    public void Operators()
    {
        short aShort = 123;
        decimal aDec = 12.3m;
        aShort = (short)Math.Round(aShort * aDec);
        aShort = (short)(aShort / (long)Math.Round(aDec));
        aShort = (short)Math.Round(aShort / aDec);
        aShort = (short)Math.Round(aShort - aDec);
        aShort = (short)Math.Round(aShort + aDec);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IntegerToShortCompoundOperatorsWithTypeConversionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Compound
{
    public void Operators()
    {
        short aShort = 123;
        int anInt = 12;
        aShort = (short)(aShort * anInt);
        aShort = (short)(aShort / anInt);
        aShort = (short)Math.Round(aShort / (double)anInt);
        aShort = (short)(aShort - anInt);
        aShort = (short)(aShort + anInt);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ShortMultiplicationDeclarationAndAssignmentAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Compound
{
    public void Operators()
    {
        short aShort = 123;
        short anotherShort = 234;
        short x = (short)(aShort * anotherShort);
        x *= aShort; // Implicit cast in C# due to compound operator
        x = (short)(aShort * x);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SquareBracketsInLabelAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public void S()
{
    goto @finally;
@finally:
    ;

    goto Step;
Step:
    ;

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SquareBracketsInIdentifierAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class SurroundingClass
{
    private int _Step_i;

    public void Step()
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task CintIsConvertedCorrectlyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class Compound
{
    public void Operators()
    {
        double do_Tmp = 9999d / 100d;
        int i_Tmp = (int)Math.Round(do_Tmp);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ArgumentsAreTypeConvertedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Drawing;

public partial class Compound
{
    public void TypeCast(int someInt)
    {
        var col = Color.FromArgb((int)Math.Round(someInt * 255.0f), (int)Math.Round(someInt * 255.0f), (int)Math.Round(someInt * 255.0f));
        float[] arry = new float[(int)Math.Round(7d / someInt + 1)];
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NullCoalescingOperatorUsesParenthesisWhenNeededAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class VisualBasicClass
{
    public void TestMethod(string x, Func<int> y)
    {
        string a = x ?? ""x"";
        string b = (x ?? ""x"").ToUpper();
        string c = $""{x ?? ""x""}"";
        string d = $""{(x ?? ""x"").ToUpper()}"";
        var e = y ?? (() => 5);
        var f = y ?? (() => 6);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NullForgivingInvocationDoesNotThrowAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class AClass
{
    public static void Identify(ITraceMessageTalker talker)
    {
        talker?.IdentifyTalker(IdentityTraceMessage());
    }

    private static object IdentityTraceMessage()
    {
        throw new NotImplementedException();
    }
}

public partial interface ITraceMessageTalker
{
    object IdentifyTalker(object v);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NegatedNullableBoolAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public enum CrashEnum
{
    None = 0,
    One = 1,
    Two = 2
}

public partial class CrashClass
{
    public CrashEnum? CrashEnum { get; set; }
    public bool IsSet { get; set; }
}

public partial class CrashTest
{
    public object Edit(bool flag2 = false, CrashEnum? crashEnum = default)
    {
        CrashClass CrashClass = null;
        bool Flag0 = true;
        bool Flag1 = true;
        if (Flag0)
        {
            if (Flag1 && flag2)
            {
                if ((int)crashEnum.GetValueOrDefault() > 0 && (!CrashClass.CrashEnum.HasValue ? true : CrashClass.CrashEnum is var arg1 && crashEnum.HasValue && arg1.HasValue ? crashEnum.Value != arg1.Value : (bool?)null).GetValueOrDefault())
                {
                    CrashClass.CrashEnum = crashEnum;
                    CrashClass.IsSet = true;
                }
            }
        }
        return null;
    }
}", extension: "cs")
            );
        }
    }
}