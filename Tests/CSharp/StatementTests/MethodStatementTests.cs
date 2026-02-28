using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class MethodStatementTests : ConverterTestBase
{
    [Fact]
    public async Task EmptyStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        If True Then
        End If

        While True
        End While

        Do
        Loop While True
    End Sub
End Class", @"
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
}");
    }

    [Fact]
    public async Task AssignmentStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;
    }
}");
    }

    [Fact]
    public async Task EnumAssignmentStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum MyEnum
    AMember
End Enum

Class TestClass
    Private Sub TestMethod(v as String)
        Dim b As MyEnum = MyEnum.Parse(GetType(MyEnum), v)
        b = MyEnum.Parse(GetType(MyEnum), v)
    End Sub
End Class", @"using System;
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
}");
    }

    [Fact]
    public async Task AssignmentStatementInDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer = 0
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}");
    }

    [Fact]
    public async Task AssignmentStatementInVarDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b = 0
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}");
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
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestFunc
    Public pubIdent = Function(row As Integer) row
    Public pubWrite = Function(row As Integer) Console.WriteLine(row)
    Dim isFalse = Function(row As Integer) False
    Dim write0 = Sub()
        Console.WriteLine(0)
    End Sub

    Private Sub TestMethod()
        Dim index = (Function(pList As List(Of String)) pList.All(Function(x) True)),
            index2 = (Function(pList As List(Of String)) pList.All(Function(x) False)),
            index3 = (Function(pList As List(Of Integer)) pList.All(Function(x) True))
        Dim isTrue = Function(pList As List(Of String))
                            Return pList.All(Function(x) True)
                     End Function
        Dim isTrueWithNoStatement = (Function(pList As List(Of String)) pList.All(Function(x) True))
        Dim write = Sub() Console.WriteLine(1)
    End Sub
End Class", @"using System;
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
CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type");
    }

    /// <summary>
    /// Technically it's possible to use a type-inferred lambda within a for loop
    /// Other than the above field/local declarations, candidates would be other things using <see cref="SplitVariableDeclarations"/>,
    /// e.g. ForEach (no assignment involved), Using block (can't have a disposable lambda)
    /// </summary>
    [Fact]
    public async Task ContrivedFuncInferenceExampleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class ContrivedFuncInferenceExample
    Private Sub TestMethod()
        For index = (Function(pList As List(Of String)) pList.All(Function(x) True)) To New Blah() Step New Blah()
            Dim buffer = index.Check(New List(Of String))
            Console.WriteLine($""{buffer}"")
        Next
    End Sub

    Class Blah
        Public ReadOnly Check As Func(Of List(Of String), Boolean)

        Public Sub New(Optional check As Func(Of List(Of String), Boolean) = Nothing)
            check = check
        End Sub

        Public Shared Widening Operator CType(ByVal p1 As Func(Of List(Of String), Boolean)) As Blah
            Return New Blah(p1)
        End Operator
        Public Shared Widening Operator CType(ByVal p1 As Blah) As Func(Of List(Of String), Boolean)
            Return p1.Check
        End Operator
        Public Shared Operator -(ByVal p1 As Blah, ByVal p2 As Blah) As Blah
            Return New Blah()
        End Operator
        Public Shared Operator +(ByVal p1 As Blah, ByVal p2 As Blah) As Blah
            Return New Blah()
        End Operator
        Public Shared Operator <=(ByVal p1 As Blah, ByVal p2 As Blah) As Boolean
            Return p1.Check(New List(Of String))
        End Operator
        Public Shared Operator >=(ByVal p1 As Blah, ByVal p2 As Blah) As Boolean
            Return p2.Check(New List(Of String))
        End Operator
    End Class
End Class", @"using System;
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
CS0019: Operator '>=' cannot be applied to operands of type 'ContrivedFuncInferenceExample.Blah' and 'int'");
    }

    [Fact]
    public async Task ObjectInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As String
        b = New String(""test"")
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string b;
        b = new string(""test"");
    }
}");
    }

    [Fact]
    public async Task TupleInitializationStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim totales As (fics As Integer, dirs As Integer) = (0, 0)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        (int fics, int dirs) totales = (0, 0);
    }
}");
    }

    [Fact]
    public async Task ObjectInitializationStatementInDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b As String = New String(""test"")
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"");
    }
}");
    }

    [Fact]
    public async Task ObjectInitializationStatementInVarDeclarationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim b = New String(""test"")
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"");
    }
}");
    }

    [Fact]
    public async Task EndStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        End
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Environment.Exit(0);
    }
}
1 source compilation errors:
BC30615: 'End' statement cannot be used in class library projects.");
    }

    [Fact]
    public async Task StopStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Stop
    End Sub
End Class", @"using System.Diagnostics;

internal partial class TestClass
{
    private void TestMethod()
    {
        Debugger.Break();
    }
}");
    }

    [Fact]
    public async Task WithBlockAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        With New System.Text.StringBuilder
            .Capacity = 20
            ?.Append(0)
        End With
    End Sub
End Class", @"using System.Text;

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
}");
    }

    [Fact]
    public async Task WithBlockStruct634Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Structure SomeStruct
    Public FieldA As Integer
    Public FieldB As Integer
End Structure

Module Module1
   Sub Main()
      Dim myArray(0) As SomeStruct

      With myArray(0)
         .FieldA = 3
         .FieldB = 4
      End With

      'Outputs: FieldA was changed to New FieldA value 
      Console.WriteLine($""FieldA was changed to {myArray(0).FieldA}"")
      Console.WriteLine($""FieldB was changed to {myArray(0).FieldB}"")
      Console.ReadLine
   End Sub
End Module", @"using System;

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
}");
    }

    [Fact]
    public async Task WithBlock2Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data.SqlClient

Class TestClass
    Private Sub Save()
        Using cmd As SqlCommand = new SqlCommand()
            With cmd
            .ExecuteNonQuery()
            ?.ExecuteNonQuery()
            .ExecuteNonQuery
            ?.ExecuteNonQuery
            End With
        End Using
    End Sub
End Class", @"using System.Data.SqlClient;

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
}");
    }

    [Fact]
    public async Task WithBlockValueAsync()
    {
        //Whitespace trivia bug on first statement in with block
        await TestConversionVisualBasicToCSharpAsync(@"Public Class VisualBasicClass
    Public Sub Stuff()
        Dim str As SomeStruct
        With Str
            ReDim .ArrField(1)
            ReDim .ArrProp(2)
        End With
    End Sub
End Class

Public Structure SomeStruct
    Public ArrField As String()
    Public Property ArrProp As String()
End Structure", @"
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
}");
    }

    [Fact]
    public async Task WithBlockMeClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestWithMe
    Private _x As Integer
    Sub S()
        With Me
            ._x = 1
            ._x = 2
        End With
    End Sub
End Class", @"
public partial class TestWithMe
{
    private int _x;
    public void S()
    {
        _x = 1;
        _x = 2;
    }
}");
    }

    [Fact]
    public async Task WithBlockMeStructAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Structure TestWithMe
    Private _x As Integer
    Sub S()
        With Me
            ._x = 1
            ._x = 2
        End With
    End Sub
End Structure", @"
public partial struct TestWithMe
{
    private int _x;
    public void S()
    {
        _x = 1;
        _x = 2;
    }
}");
    }

    [Fact]
    public async Task WithBlockForEachAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic

Public Class TestWithForEachClass
    Private _x As Integer

    Public Shared Sub Main()
        Dim x = New List(Of TestWithForEachClass)()
        For Each y In x
            With y
                ._x = 1
                System.Console.Write(._x)
            End With
            y = Nothing
        Next
    End Sub
End Class", @"using System;
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
            y = null;
        }
    }
}
1 target compilation errors:
CS1656: Cannot assign to 'y' because it is a 'foreach iteration variable'");
    }

    [Fact]
    public async Task NestedWithBlockAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        With New System.Text.StringBuilder
            Dim withBlock as Integer = 3
            With New System.Text.StringBuilder
                Dim withBlock1 as Integer = 4
                .Capacity = withBlock1
            End With

            .Length = withBlock
        End With
    End Sub
End Class", @"using System.Text;

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
}");
    }

    [Fact]
    public async Task DeclarationStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Test
    Private Sub TestMethod()
the_beginning:
        Dim value As Integer = 1
        Const myPIe As Double = 2 * System.Math.PI
        Dim text = ""This is my text!""
        GoTo the_beginning
    End Sub
End Class", @"using System;

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
}");
    }
    [Fact]
    public async Task DeclarationStatementTwoVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class Test
    Private Sub TestMethod()
        Dim x, y As Date
        Console.WriteLine(x)
        Console.WriteLine(y)
    End Sub
End Class", @"using System;

internal partial class Test
{
    private void TestMethod()
    {
        DateTime x = default, y = default;
        Console.WriteLine(x);
        Console.WriteLine(y);
    }
}");
    }

    [Fact]
    public async Task DeclareStatementLongAsync()
    {
        // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Diagnostics
Imports System.Threading

Public Class AcmeClass
    Private Declare Sub SetForegroundWindow Lib ""user32"" (ByVal hwnd As Int32)

    Public Shared Sub Main()
        For Each proc In Process.GetProcesses().Where(Function(p) Not String.IsNullOrEmpty(p.MainWindowTitle))
            SetForegroundWindow(proc.MainWindowHandle.ToInt32())
            Thread.Sleep(1000)
        Next
    End Sub
End Class"
            , @"using System.Diagnostics;
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
}");
    }

    [Fact]
    public async Task DeclareStatementVoidAsync()
    {
        // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Diagnostics
Imports System.Threading

Public Class AcmeClass
    Private Declare Function SetForegroundWindow Lib ""user32"" (ByVal hwnd As Int32) As Long

    Public Shared Sub Main()
        For Each proc In Process.GetProcesses().Where(Function(p) Not String.IsNullOrEmpty(p.MainWindowTitle))
            SetForegroundWindow(proc.MainWindowHandle.ToInt32())
            Thread.Sleep(1000)
        Next
    End Sub
End Class"
            , @"using System.Diagnostics;
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
}");
    }

    [Fact]
    public async Task DeclareStatementWithAttributesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class AcmeClass
    Friend Declare Ansi Function GetNumDevices Lib ""CP210xManufacturing.dll"" Alias ""CP210x_GetNumDevices"" (ByRef NumDevices As String) As Integer
End Class"
            , @"using System.Runtime.InteropServices;

public partial class AcmeClass
{
    [DllImport(""CP210xManufacturing.dll"", EntryPoint = ""CP210x_GetNumDevices"", CharSet = CharSet.Ansi)]
    internal static extern int GetNumDevices(ref string NumDevices);
}");
    }

    [Fact]
    public async Task IfStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal a As Integer)
        Dim b As Integer

        If a = 0 Then
            b = 0
        ElseIf a = 1 Then
            b = 1
        ElseIf a = 2 OrElse a = 3 Then
            b = 2
        Else
            b = 3
        End If
    End Sub
End Class", @"
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
}");
    }

    [Fact]
    public async Task IfStatementWithMultiStatementLineAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Shared Sub MultiStatement(a As Integer)
        If a = 0 Then Console.WriteLine(1) : Console.WriteLine(2) : Return
        Console.WriteLine(3)
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task NestedBlockStatementsKeepSameNestingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Shared Function FindTextInCol(w As String, pTitleRow As Integer, startCol As Integer, needle As String) As Integer

        For c As Integer = startCol To w.Length
            If needle = """" Then
                If String.IsNullOrWhiteSpace(w(c).ToString) Then
                    Return c
                End If
            Else
                If w(c).ToString = needle Then
                    Return c
                End If
            End If
        Next
        Return -1
    End Function
End Class", @"
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
}");
    }

    [Fact]
    public async Task SyncLockStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))

        SyncLock nullObject
            Console.WriteLine(nullObject)
        End SyncLock
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject is null)
            throw new ArgumentNullException(nameof(nullObject));

        lock (nullObject)
            Console.WriteLine(nullObject);
    }
}");
    }

    [Fact]
    public async Task ThrowStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject is null)
            throw new ArgumentNullException(nameof(nullObject));
    }
}");
    }

    [Fact]
    public async Task CallStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Call (Sub() Console.Write(""Hello""))
        Call (Sub() Console.Write(""Hello""))()
        Call TestMethod
        Call TestMethod()
    End Sub
End Class", @"using System;

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
CS0149: Method name expected");
        //BUG: Requires new Action wrapper
    }

    [Fact]
    public async Task AddRemoveHandlerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Event MyEvent As EventHandler

    Private Sub TestMethod(ByVal e As EventHandler)
        AddHandler Me.MyEvent, e
        AddHandler Me.MyEvent, AddressOf MyHandler
    End Sub

    Private Sub TestMethod2(ByVal e As EventHandler)
        RemoveHandler Me.MyEvent, e
        RemoveHandler Me.MyEvent, AddressOf MyHandler
    End Sub

    Private Sub MyHandler(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task SelectCase1Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal number As Integer)
        Select Case number
            Case 0, 1, 2
                Console.Write(""number is 0, 1, 2"")
            Case 5
                Console.Write(""section 5"")
            Case Else
                Console.Write(""default section"")
        End Select
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task SelectCaseWithExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestClass
    Shared Function TimeAgo(daysAgo As Integer) As String
        Select Case daysAgo
            Case 0 To 3, 4, Is >= 5, Is < 6, Is <= 7
                Return ""this week""
            Case Is > 0
                Return daysAgo \ 7 & "" weeks ago""
            Case Else
                Return ""in the future""
        End Select
    End Function
End Class", @"
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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }

    [Fact]
    public async Task SelectCaseWithStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestClass
    Shared Function TimeAgo(x As String) As String
        Select Case UCase(x)
            Case UCase(""a""), UCase(""b"")
                Return ""ab""
            Case UCase(""c"")
                Return ""c""
            Case ""d""
                Return ""d""
            Case Else
                Return ""e""
        End Select
    End Function
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
        //BUG: Correct textual output, but requires var pattern syntax construct not available before CodeAnalysis 3
    }

    [Fact]
    public async Task SelectCaseWithExpression2Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestClass2
    Function CanDoWork(Something As Object) As Boolean
        Select Case True
            Case Today.DayOfWeek = DayOfWeek.Saturday Or Today.DayOfWeek = DayOfWeek.Sunday
                ' we do not work on weekends
                Return False
            Case Not IsSqlAlive()
                ' Database unavailable
                Return False
            Case TypeOf Something Is Integer
                ' Do something with the Integer
                Return True
            Case Else
                ' Do something else
                Return False
        End Select
    End Function

    Private Function IsSqlAlive() As Boolean
        ' Do something to test SQL Server
        Return True
    End Function
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task SelectCaseWithNonDeterministicExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class TestClass2
    Sub DoesNotThrow()
        Dim rand As New Random
        Select Case rand.Next(8)
            Case Is < 4
            Case 4
            Case Is > 4
            Case Else
                Throw New Exception
        End Select
    End Sub
End Class", @"using System;

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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }

    [Fact]
    public async Task Issue579SelectCaseWithCaseInsensitiveTextCompareAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Option Compare Text ' Comments lost

Class Issue579SelectCaseWithCaseInsensitiveTextCompare
Private Function Test(astr_Temp As String) As Nullable(Of Boolean)
    Select Case astr_Temp
        Case ""Test""
            Return True
        Case astr_Temp
            Return False
        Case Else
            Return Nothing
    End Select
End Function
End Class", @"using System.Globalization;

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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }

    [Fact]
    public async Task Issue707SelectCaseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Class Issue707SelectCaseAsyncClass
    Private Function Exists(sort As Char?) As Boolean?
        Select Case Microsoft.VisualBasic.LCase(sort + """")
            Case """", Nothing
                Return False
            Case Else
                Return True
        End Select
    End Function
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }

    [Fact]
    public async Task TryCatchAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Shared Function Log(ByVal message As String) As Boolean
        Console.WriteLine(message)
        Return False
    End Function

    Private Sub TestMethod(ByVal number As Integer)
        Try
            Console.WriteLine(""try"")
        Catch e As Exception
            Console.WriteLine(""catch1"")
        Catch
            Console.WriteLine(""catch all"")
        Finally
            Console.WriteLine(""finally"")
        End Try

        Try
            Console.WriteLine(""try"")
        Catch e2 As NotImplementedException
            Console.WriteLine(""catch1"")
        Catch e As Exception When Log(e.Message)
            Console.WriteLine(""catch2"")
        End Try

        Try
            Console.WriteLine(""try"")
        Finally
            Console.WriteLine(""finally"")
        End Try
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task SwitchIntToEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Module Main
    Public Enum EWhere As Short
        None = 0
        Bottom = 1
    End Enum

    Friend Function prtWhere(ByVal aWhere As EWhere) As String
        Select Case aWhere
            Case EWhere.None
                Return "" ""
            Case EWhere.Bottom
                Return ""_ ""
        End Select

    End Function
End Module", @"
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
}");
    }

    [Fact] //https://github.com/icsharpcode/CodeConverter/issues/585
    public async Task Issue585_SwitchNonStringAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data

Public Class NonStringSelect
    Private Function Test3(CurRow As DataRow)
        For Each CurCol As DataColumn In CurRow.GetColumnsInError
            Select Case CurCol.DataType
                Case GetType(String)
                    Return False
                Case Else
                    Return True
            End Select
        Next
    End Function
End Class", @"using System.Data;

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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }


    [Fact]
    public async Task ExitMethodBlockStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function FuncReturningNull() As Object
        Dim zeroLambda = Function(y) As Integer
                            Exit Function
                         End Function
        Exit Function
    End Function

    Private Function FuncReturningZero() As Integer
        Dim nullLambda = Function(y) As Object
                            Exit Function
                         End Function
        Exit Function
    End Function

    Private Function FuncReturningAssignedValue() As Integer
        Dim aSub = Sub(y)
                            Exit Sub
                         End Sub
        FuncReturningAssignedValue = 3
        Exit Function
    End Function
End Class", @"
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
}");
    }

    [Fact]
    public async Task YieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Iterator Function TestMethod(ByVal number As Integer) As IEnumerable(Of Integer)
        If number < 0 Then Return
        If number < 1 Then Exit Function
        For i As Integer = 0 To number - 1
            Yield i
        Next
        Return
    End Function
End Class", @"using System.Collections.Generic;

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
}");
    }

    [Fact]
    public async Task SetterReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public ReadOnly Property Prop() As Object
    Get
        Try
            Prop = New Object
            Exit Property
        Catch ex As Exception
        End Try
    End Get
End Property

Public Function Func() As Object
    Try
        Func = New Object
        Exit Function
    Catch ex As Exception
    End Try
End Function", @"
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
}");
    }

    [Fact]
    public async Task WithBlockWithNullConditionalAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class Class1
    Public Property x As Class1
    Public Property Name As String
End Class

Public Class TestClass
    Private _Data As Class1
    Private x As String

    Public Sub TestMethod()
        With _Data
            x = .x?.Name
        End With
    End Sub
End Class", @"
public partial class Class1
{
    public Class1 x { get; set; }
    public string Name { get; set; }
}

public partial class TestClass
{
    private Class1 _Data;
    private string x;

    public void TestMethod()
    {
        {
            ref var withBlock = ref _Data;
            x = withBlock.x?.Name;
        }
    }
}");
    }
}
