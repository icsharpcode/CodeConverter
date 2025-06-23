using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class MethodStatementTests : ConverterTestBase
{
    [Fact]
    public async Task EmptyStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        if (true)
        {
        }

        while (true)
        {
        }

        do
        {
        }
        while (true);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AssignmentStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EnumAssignmentStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal enum MyEnum
{
    AMember
}

internal partial class TestClass
{
    private void TestMethod(string v)
    {
        MyEnum b = (MyEnum)Conversions.ToInteger(Enum.Parse(typeof(MyEnum), v));
        b = (MyEnum)Conversions.ToInteger(Enum.Parse(typeof(MyEnum), v));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AssignmentStatementInDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AssignmentStatementInVarDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}", extension: "cs")
            );
        }
    }

    /// <summary>
    /// Implicitly typed lambdas exist in vb but are not happening in C#. See discussion on https://github.com/dotnet/roslyn/issues/14
    /// * For VB local declarations, inference happens. The closest equivalent in C# is a local function since Func/Action would be overly restrictive for some cases
    /// * For VB field declarations, inference doesn't happen, it just uses "Object", but in C# lambdas can't be assigned to object so we have to settle for Func/Action for externally visible methods to maintain assignability.
    /// </summary>
    [Fact]
    public async Task AssignmentStatementWithFuncAsync()
    {
        // BUG: pubWrite's body is missing a return statement
        // pubWrite is an example of when the LambdaConverter could analyze ConvertedType at usages, realize the return type is never used, and convert it to an Action.
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;
using System.Linq;

public partial class TestFunc
{
    public Func<int, int> pubIdent = (row) => row;
    public Func<int, object> pubWrite = (row) => Console.WriteLine(row);
    private bool isFalse(int row) => false;
    private void write0() => Console.WriteLine(0);

    private void TestMethod()
    {
        bool index(List<string> pList) => pList.All(x => true);
        bool index2(List<string> pList) => pList.All(x => false);
        bool index3(List<int> pList) => pList.All(x => true);
        bool isTrue(List<string> pList) => pList.All(x => true);
        bool isTrueWithNoStatement(List<string> pList) => pList.All(x => true);
        void write() => Console.WriteLine(1);
    }
}
1 source compilation errors:
BC30491: Expression does not produce a value.
2 target compilation errors:
CS0029: Cannot implicitly convert type 'void' to 'object'
CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type", extension: "cs")
            );
        }
    }

    /// <summary>
    /// Technically it's possible to use a type-inferred lambda within a for loop
    /// Other than the above field/local declarations, candidates would be other things using <see cref="SplitVariableDeclarations"/>,
    /// e.g. ForEach (no assignment involved), Using block (can't have a disposable lambda)
    /// </summary>
    [Fact]
    public async Task ContrivedFuncInferenceExampleAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;
using System.Linq;

internal partial class ContrivedFuncInferenceExample
{
    private void TestMethod()
    {
        for (Blah index = (pList) => pList.All(x => true), loopTo = new Blah(); new Blah() >= 0 ? index <= loopTo : index >= loopTo; index += new Blah())
        {
            bool buffer = index.Check(new List<string>());
            Console.WriteLine($""{buffer}"");
        }
    }

    public partial class Blah
    {
        public readonly Func<List<string>, bool> Check;

        public Blah(Func<List<string>, bool> check = null)
        {
            check = check;
        }

        public static implicit operator Blah(Func<List<string>, bool> p1)
        {
            return new Blah(p1);
        }
        public static implicit operator Func<List<string>, bool>(Blah p1)
        {
            return p1.Check;
        }
        public static Blah operator -(Blah p1, Blah p2)
        {
            return new Blah();
        }
        public static Blah operator +(Blah p1, Blah p2)
        {
            return new Blah();
        }
        public static bool operator <=(Blah p1, Blah p2)
        {
            return p1.Check(new List<string>());
        }
        public static bool operator >=(Blah p1, Blah p2)
        {
            return p2.Check(new List<string>());
        }
    }
}
2 target compilation errors:
CS1660: Cannot convert lambda expression to type 'ContrivedFuncInferenceExample.Blah' because it is not a delegate type
CS0019: Operator '>=' cannot be applied to operands of type 'ContrivedFuncInferenceExample.Blah' and 'int'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ObjectInitializationStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string b;
        b = new string(""test"".ToCharArray());
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TupleInitializationStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        (int fics, int dirs) totales = (0, 0);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ObjectInitializationStatementInDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"".ToCharArray());
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ObjectInitializationStatementInVarDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"".ToCharArray());
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EndStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Environment.Exit(0);
    }
}
1 source compilation errors:
BC30615: 'End' statement cannot be used in class library projects.", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StopStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Diagnostics;

internal partial class TestClass
{
    private void TestMethod()
    {
        Debugger.Break();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlockAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Text;

internal partial class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock = new StringBuilder();
            withBlock.Capacity = 20;
            withBlock?.Append(0);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlockStruct634Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial struct SomeStruct
{
    public int FieldA;
    public int FieldB;
}

internal static partial class Module1
{
    public static void Main()
    {
        var myArray = new SomeStruct[1];

        {
            ref var withBlock = ref myArray[0];
            withBlock.FieldA = 3;
            withBlock.FieldB = 4;
        }

        // Outputs: FieldA was changed to New FieldA value 
        Console.WriteLine($""FieldA was changed to {myArray[0].FieldA}"");
        Console.WriteLine($""FieldB was changed to {myArray[0].FieldB}"");
        Console.ReadLine();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlock2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data.SqlClient;

internal partial class TestClass
{
    private void Save()
    {
        using (var cmd = new SqlCommand())
        {
            cmd.ExecuteNonQuery();
            cmd?.ExecuteNonQuery();
            cmd.ExecuteNonQuery();
            cmd?.ExecuteNonQuery();
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlockValueAsync()
    {
        //Whitespace trivia bug on first statement in with block
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class VisualBasicClass
{
    public void Stuff()
    {
        var str = default(SomeStruct);
        str.ArrField = new string[2];
        str.ArrProp = new string[3];
    }
}

public partial struct SomeStruct
{
    public string[] ArrField;
    public string[] ArrProp { get; set; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlockMeClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class TestWithMe
{
    private int _x;
    public void S()
    {
        _x = 1;
        _x = 2;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlockMeStructAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial struct TestWithMe
{
    private int _x;
    public void S()
    {
        _x = 1;
        _x = 2;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WithBlockForEachAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;

public partial class TestWithForEachClass
{
    private int _x;

    public static void Main()
    {
        var x = new List<TestWithForEachClass>();
        foreach (var y in x)
        {
            y._x = 1;
            Console.Write(y._x);
            y = (TestWithForEachClass)null;
        }
    }
}
1 target compilation errors:
CS1656: Cannot assign to 'y' because it is a 'foreach iteration variable'", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NestedWithBlockAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Text;

internal partial class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock2 = new StringBuilder();
            int withBlock = 3;
            {
                var withBlock3 = new StringBuilder();
                int withBlock1 = 4;
                withBlock3.Capacity = withBlock1;
            }

            withBlock2.Length = withBlock;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DeclarationStatementsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class Test
{
    private void TestMethod()
    {
    the_beginning:
        ;

        int value = 1;
        const double myPIe = 2d * Math.PI;
        string text = ""This is my text!"";
        goto the_beginning;
    }
}", extension: "cs")
            );
        }
    }
    [Fact]
    public async Task DeclarationStatementTwoVariablesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class Test
{
    private void TestMethod()
    {
        DateTime x = default, y = default;
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DeclareStatementLongAsync()
    {
        // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

public partial class AcmeClass
{
    [DllImport(""user32"")]
    private static extern void SetForegroundWindow(int hwnd);

    public static void Main()
    {
        foreach (var proc in Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)))
        {
            SetForegroundWindow(proc.MainWindowHandle.ToInt32());
            Thread.Sleep(1000);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DeclareStatementVoidAsync()
    {
        // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

public partial class AcmeClass
{
    [DllImport(""user32"")]
    private static extern long SetForegroundWindow(int hwnd);

    public static void Main()
    {
        foreach (var proc in Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)))
        {
            SetForegroundWindow(proc.MainWindowHandle.ToInt32());
            Thread.Sleep(1000);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DeclareStatementWithAttributesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Runtime.InteropServices;

public partial class AcmeClass
{
    [DllImport(""CP210xManufacturing.dll"", EntryPoint = ""CP210x_GetNumDevices"", CharSet = CharSet.Ansi)]
    internal static extern int GetNumDevices(ref string NumDevices);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IfStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(int a)
    {
        int b;

        if (a == 0)
        {
            b = 0;
        }
        else if (a == 1)
        {
            b = 1;
        }
        else if (a == 2 || a == 3)
        {
            b = 2;
        }
        else
        {
            b = 3;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IfStatementWithMultiStatementLineAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    public static void MultiStatement(int a)
    {
        if (a == 0)
        {
            Console.WriteLine(1);
            Console.WriteLine(2);
            return;
        }
        Console.WriteLine(3);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NestedBlockStatementsKeepSameNestingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    public static int FindTextInCol(string w, int pTitleRow, int startCol, string needle)
    {

        for (int c = startCol, loopTo = w.Length; c <= loopTo; c++)
        {
            if (string.IsNullOrEmpty(needle))
            {
                if (string.IsNullOrWhiteSpace(w[c].ToString()))
                {
                    return c;
                }
            }
            else if ((w[c].ToString() ?? """") == (needle ?? """"))
            {
                return c;
            }
        }
        return -1;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SyncLockStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject is null)
            throw new ArgumentNullException(nameof(nullObject));

        lock (nullObject)
            Console.WriteLine(nullObject);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ThrowStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject is null)
            throw new ArgumentNullException(nameof(nullObject));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task CallStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        (() => Console.Write(""Hello""))();
        (() => Console.Write(""Hello""))();
        TestMethod();
        TestMethod();
    }
}
1 target compilation errors:
CS0149: Method name expected", extension: "cs")
            );
        }
        //BUG: Requires new Action wrapper
    }

    [Fact]
    public async Task AddRemoveHandlerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    public event EventHandler MyEvent;

    private void TestMethod(EventHandler e)
    {
        MyEvent += e;
        MyEvent += MyHandler;
    }

    private void TestMethod2(EventHandler e)
    {
        MyEvent -= e;
        MyEvent -= MyHandler;
    }

    private void MyHandler(object sender, EventArgs e)
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCase1Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(int number)
    {
        switch (number)
        {
            case 0:
            case 1:
            case 2:
                {
                    Console.Write(""number is 0, 1, 2"");
                    break;
                }
            case 5:
                {
                    Console.Write(""section 5"");
                    break;
                }

            default:
                {
                    Console.Write(""default section"");
                    break;
                }
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseWithExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class TestClass
{
    public static string TimeAgo(int daysAgo)
    {
        switch (daysAgo)
        {
            case var @case when 0 <= @case && @case <= 3:
            case 4:
            case var case1 when case1 >= 5:
            case var case2 when case2 < 6:
            case var case3 when case3 <= 7:
                {
                    return ""this week"";
                }
            case var case4 when case4 > 0:
                {
                    return daysAgo / 7 + "" weeks ago"";
                }

            default:
                {
                    return ""in the future"";
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseWithStringAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class TestClass
{
    public static string TimeAgo(string x)
    {
        switch (Strings.UCase(x) ?? """")
        {
            case var @case when @case == (Strings.UCase(""a"") ?? """"):
            case var case1 when case1 == (Strings.UCase(""b"") ?? """"):
                {
                    return ""ab"";
                }
            case var case2 when case2 == (Strings.UCase(""c"") ?? """"):
                {
                    return ""c"";
                }
            case ""d"":
                {
                    return ""d"";
                }

            default:
                {
                    return ""e"";
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
    public async Task SelectCaseWithExpression2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class TestClass2
{
    public bool CanDoWork(object Something)
    {
        switch (true)
        {
            case object _ when DateTime.Today.DayOfWeek == DayOfWeek.Saturday | DateTime.Today.DayOfWeek == DayOfWeek.Sunday:
                {
                    // we do not work on weekends
                    return false;
                }
            case object _ when !IsSqlAlive():
                {
                    // Database unavailable
                    return false;
                }
            case object _ when Something is int:
                {
                    // Do something with the Integer
                    return true;
                }

            default:
                {
                    // Do something else
                    return false;
                }
        }
    }

    private bool IsSqlAlive()
    {
        // Do something to test SQL Server
        return true;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SelectCaseWithNonDeterministicExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial class TestClass2
{
    public void DoesNotThrow()
    {
        var rand = new Random();
        switch (rand.Next(8))
        {
            case var @case when @case < 4:
                {
                    break;
                }
            case 4:
                {
                    break;
                }
            case var case1 when case1 > 4:
                {
                    break;
                }

            default:
                {
                    throw new Exception();
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue579SelectCaseWithCaseInsensitiveTextCompareAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Globalization;

internal partial class Issue579SelectCaseWithCaseInsensitiveTextCompare
{
    private bool? Test(string astr_Temp)
    {
        switch (astr_Temp ?? """")
        {
            case var @case when CultureInfo.CurrentCulture.CompareInfo.Compare(@case, ""Test"", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
                {
                    return true;
                }
            case var case1 when CultureInfo.CurrentCulture.CompareInfo.Compare(case1, astr_Temp ?? """", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0:
                {
                    return false;
                }

            default:
                {
                    return default;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue707SelectCaseAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class Issue707SelectCaseAsyncClass
{
    private bool? Exists(char? sort)
    {
        switch (Strings.LCase(Conversions.ToString(sort.Value) + """") ?? """")
        {
            case var @case when @case == """":
            case var case1 when case1 == """":
                {
                    return false;
                }

            default:
                {
                    return true;
                }
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TryCatchAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private static bool Log(string message)
    {
        Console.WriteLine(message);
        return false;
    }

    private void TestMethod(int number)
    {
        try
        {
            Console.WriteLine(""try"");
        }
        catch (Exception e)
        {
            Console.WriteLine(""catch1"");
        }
        catch
        {
            Console.WriteLine(""catch all"");
        }
        finally
        {
            Console.WriteLine(""finally"");
        }

        try
        {
            Console.WriteLine(""try"");
        }
        catch (NotImplementedException e2)
        {
            Console.WriteLine(""catch1"");
        }
        catch (Exception e) when (Log(e.Message))
        {
            Console.WriteLine(""catch2"");
        }

        try
        {
            Console.WriteLine(""try"");
        }
        finally
        {
            Console.WriteLine(""finally"");
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SwitchIntToEnumAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal static partial class Main
{
    public enum EWhere : short
    {
        None = 0,
        Bottom = 1
    }

    internal static string prtWhere(EWhere aWhere)
    {
        switch (aWhere)
        {
            case EWhere.None:
                {
                    return "" "";
                }
            case EWhere.Bottom:
                {
                    return ""_ "";
                }
        }

        return default;

    }
}", extension: "cs")
            );
        }
    }

    [Fact] //https://github.com/icsharpcode/CodeConverter/issues/585
    public async Task Issue585_SwitchNonStringAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;

public partial class NonStringSelect
{
    private object Test3(DataRow CurRow)
    {
        foreach (DataColumn CurCol in CurRow.GetColumnsInError())
        {
            switch (CurCol.DataType)
            {
                case var @case when @case == typeof(string):
                    {
                        return false;
                    }

                default:
                    {
                        return true;
                    }
            }
        }

        return default;
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task ExitMethodBlockStatementsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private object FuncReturningNull()
    {
        int zeroLambda(object y) => default;
        return default;
    }

    private int FuncReturningZero()
    {
        object nullLambda(object y) => default;
        return default;
    }

    private int FuncReturningAssignedValue()
    {
        int FuncReturningAssignedValueRet = default;
        void aSub(object y) { return; };
        FuncReturningAssignedValueRet = 3;
        return FuncReturningAssignedValueRet;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task YieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

internal partial class TestClass
{
    private IEnumerable<int> TestMethod(int number)
    {
        if (number < 0)
            yield break;
        if (number < 1)
            yield break;
        for (int i = 0, loopTo = number - 1; i <= loopTo; i++)
            yield return i;
        yield break;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SetterReturnAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class SurroundingClass
{
    public object Prop
    {
        get
        {
            object PropRet = default;
            try
            {
                PropRet = new object();
                return PropRet;
            }
            catch (Exception ex)
            {
            }

            return PropRet;
        }
    }

    public object Func()
    {
        object FuncRet = default;
        try
        {
            FuncRet = new object();
            return FuncRet;
        }
        catch (Exception ex)
        {
        }

        return FuncRet;
    }
}", extension: "cs")
            );
        }
    }
}