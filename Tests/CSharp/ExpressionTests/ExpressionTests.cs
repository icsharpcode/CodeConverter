using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task ConversionOfNotUsesParensIfNeededAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim rslt = Not 1 = 2
        Dim rslt2 = Not True
        Dim rslt3 = TypeOf New Object() IsNot Boolean
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        bool rslt = !(1 == 2);
        bool rslt2 = !true;
        bool rslt3 = !(new object() is bool);
    }
}");
        }

        [Fact]
        public async Task DateLiteralsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(Optional ByVal pDate As Date = #1/1/1900#)
        Dim rslt = #1/1/1900#
        Dim rslt2 = #8/13/2002 12:14 PM#
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class TestClass
{
    private void TestMethod([Optional, DateTimeConstant(599266080000000000/* #1/1/1900# */)] DateTime pDate)
    {
        var rslt = DateTime.Parse(""1900-01-01"");
        var rslt2 = DateTime.Parse(""2002-08-13 12:14:00"");
    }
}");
        }

        [Fact]
        public async Task DateConstsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue213
    Const x As Date = #1990-1-1#

    Private Sub Y(Optional ByVal opt As Date = x)
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public partial class Issue213
{
    private static DateTime x = DateTime.Parse(""1990-01-01"");

    private void Y([Optional, DateTimeConstant(627667488000000000/* Global.Issue213.x */)] DateTime opt)
    {
    }
}");
        }

        [Fact]
        public async Task MethodCallWithImplicitConversionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Bar(True)
        Me.Bar(""4"")
        Dim ss(1) As String
        Dim y = ss(""0"")
    End Sub

    Sub Bar(x as Integer)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
}");
        }

        [Fact]
        public async Task Issue580_EnumCastsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class EnumToString
    Enum Tes As Short
        None = 0
        TEST2 = 2
    End Enum
    Private Sub TEest2(aEnum As Tes)
        Dim sxtr_Tmp As String = ""Use"" & CShort(aEnum).ToString
        Dim si_Txt As Short = CShort(2 ^ Tes.TEST2)
    End Sub
End Class",
@"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
        short si_Txt = Conversions.ToShort(Math.Pow(2, (double)Tes.TEST2));
    }
}");
        }

        [Fact]
        public async Task IntToEnumArgAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo(ByVal arg As TriState)
    End Sub

    Sub Main()
        Foo(0)
    End Sub
End Class",
@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo(TriState arg)
    {
    }

    public void Main()
    {
        Foo(0);
    }
}");
        }

        [Fact]
        public async Task EnumToIntCastAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class MyTest
    Public Enum TestEnum As Integer
        Test1 = 0
        Test2 = 1
    End Enum

    Sub Main()
        Dim EnumVariable = TestEnum.Test1
        Dim t1 As Integer = EnumVariable
    End Sub
End Class",
@"
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
");
        }

        [Fact]
        public async Task FlagsEnumAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"<Flags()> Public Enum FilePermissions As Integer
    None = 0
    Create = 1
    Read = 2
    Update = 4
    Delete = 8
End Enum
Public Class MyTest
    Public MyEnum As FilePermissions = FilePermissions.None + FilePermissions.Create
End Class",
@"using System;

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
}");
        }

        [Fact]
        public async Task EnumSwitchAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Enum E
        A
    End Enum

    Sub Main()
        Dim e1 = E.A
        Dim e2 As Integer
        Select Case e1
            Case 0
        End Select

        Select Case e2
            Case E.A
        End Select

    End Sub
End Class",
@"
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
}");
        }

        [Fact]
        public async Task DuplicateCaseDiscardedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System
    Friend Module Module1
    Sub Main()
        Select Case 1
            Case 1
                Console.WriteLine(""a"")

            Case 1
                Console.WriteLine(""b"")

        End Select

    End Sub
End Module",
@"using System;

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
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
            //BUG: Correct textual output, but requires var pattern syntax construct not available before CodeAnalysis 3
        }

        [Fact]
        public async Task MethodCallWithoutParensAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim w = Bar
        Dim x = Me.Bar
        Dim y = Baz()
        Dim z = Me.Baz()
    End Sub

    Function Bar() As Integer
        Return 1
    End Function
    Property Baz As Integer
End Class", @"
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
}");
        }

        [Fact]
        public async Task ConversionOfCTypeUsesParensIfNeededAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim rslt = Ctype(true, Object).ToString()
        Dim rslt2 = Ctype(true, Object)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        string rslt = true.ToString();
        object rslt2 = true;
    }
}");
        }

        [Fact]
        public async Task DateKeywordAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private DefaultDate as Date = Nothing
End Class", @"using System;

internal partial class TestClass
{
    private DateTime DefaultDate = default;
}");
        }

        [Fact]
        public async Task GenericComparisonAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class GenericComparison
    Public Sub m(Of T)(p As T)
        If p Is Nothing Then Return
    End Sub
End Class", @"
public partial class GenericComparison
{
    public void m<T>(T p)
    {
        if (p is null)
            return;
    }
}");
        }

        [Fact]
        public async Task AccessSharedThroughInstanceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class A
    Public Shared x As Integer = 2
    Public Sub Test()
        Dim tmp = Me
        Dim y = Me.x
        Dim z = tmp.x
    End Sub
End Class", @"
public partial class A
{
    public static int x = 2;

    public void Test()
    {
        var tmp = this;
        int y = x;
        int z = x;
    }
}");
        }

        [Fact]
        public async Task EmptyArrayExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"
Public Class Issue495
    Public Function Empty() As Integer()
        Return {}
    End Function
End Class", @"using System;

public partial class Issue495
{
    public int[] Empty()
    {
        return Array.Empty<int>();
    }
}");
        }

        [Fact]
        public async Task ReducedTypeParametersInferrableAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = """".Split("",""c).Select(Of String)(Function(x) x)
    End Sub
End Class", @"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
    }
}");
        }

        [Fact]
        public async Task ReducedTypeParametersNonInferrableAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = """".Split("",""c).Select(Of Object)(Function(x) x)
    End Sub
End Class", @"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select<string, object>(x => x);
    }
}");
        }

        [Fact]
        public async Task EnumNullableConversionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Main()
        Dim x = DayOfWeek.Monday
        Foo(x)
    End Sub

    Sub Foo(x As DayOfWeek?)

    End Sub
End Class", @"using System;

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
}");
        }

        [Fact]
        public async Task UninitializedVariableAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub New()
        Dim needsInitialization As Integer
        Dim notUsed As Integer
        Dim y = needsInitialization
    End Sub

    Sub Foo()
        Dim needsInitialization As Integer
        Dim notUsed As Integer
        Dim y = needsInitialization
    End Sub

    Sub Bar()
        Dim i As Integer, temp As String = String.Empty
        i += 1
    End Sub

    Sub Bar2()
        Dim i As Integer, temp As String = String.Empty
        i = i + 1
    End Sub

    Sub Bar3()
        Dim i As Integer, temp As String = String.Empty
        Dim k As Integer = i + 1
    End Sub

    Sub Bar4()
        Dim i As Integer, temp As String = String.Empty
        Dim k As Integer = i + 1
        i = 1
    End Sub

    Public ReadOnly Property State As Integer
        Get
            Dim needsInitialization As Integer
            Dim notUsed As Integer
            Dim y = needsInitialization
            Return y
        End Get
    End Property
End Class", @"
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
}");
        }

        [Fact]
        public async Task FullyTypeInferredEnumerableCreationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim strings = { ""1"", ""2"" }
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        var strings = new[] { ""1"", ""2"" };
    }
}");
        }

        [Fact]
        public async Task GetTypeExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim typ = GetType(String)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod()
    {
        var typ = typeof(string);
    }
}");
        }

        [Fact]
        public async Task NullableIntegerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Function Bar(value As String) As Integer?
        Dim result As Integer
        If Integer.TryParse(value, result) Then
            Return result
        Else
            Return Nothing
        End If
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task NothingInvokesDefaultForValueTypesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Sub Bar()
        Dim number As Integer
        number = Nothing
        Dim dat As Date
        dat = Nothing
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Bar()
    {
        int number;
        number = default;
        DateTime dat;
        dat = default;
    }
}");
        }

        [Fact]
        public async Task ConditionalExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = If((str = """"), True, False)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        bool result = string.IsNullOrEmpty(str) ? true : false;
    }
}");
        }

        [Fact]
        public async Task ConditionalExpressionInStringConcatAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class ConditionalExpressionInStringConcat
    Private Sub TestMethod(ByVal str As String)
        Dim appleCount as integer = 42
        Console.WriteLine(""I have "" & appleCount & If(appleCount = 1, "" apple"", "" apples""))
    End Sub
End Class", @"using System;

internal partial class ConditionalExpressionInStringConcat
{
    private void TestMethod(string str)
    {
        int appleCount = 42;
        Console.WriteLine(""I have "" + appleCount + (appleCount == 1 ? "" apple"" : "" apples""));
    }
}");
        }

        [Fact]
        public async Task ConditionalExpressionInUnaryExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = Not If((str = """"), True, False)
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        bool result = !(string.IsNullOrEmpty(str) ? true : false);
    }
}");
        }

        [Fact]
        public async Task NullCoalescingExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Console.WriteLine(If(str, ""<null>""))
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
    }
}");
        }

        [Fact]
        public async Task OmittedArgumentInInvocationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Module MyExtensions
    public sub NewColumn(type As Type , Optional strV1 As String = nothing, optional code As String = ""code"")
    End sub

    public Sub CallNewColumn()
        NewColumn(GetType(MyExtensions))
        NewColumn(Nothing, , ""otherCode"")
        NewColumn(Nothing, ""fred"")
    End Sub
End Module", @"using System;

public static partial class MyExtensions
{
    public static void NewColumn(Type type, string strV1 = null, string code = ""code"")
    {
    }

    public static void CallNewColumn()
    {
        NewColumn(typeof(MyExtensions));
        NewColumn(null, code: ""otherCode"");
        NewColumn(null, ""fred"");
    }
}");
        }

        [Fact]
        public async Task OmittedArgumentInCallInvocationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue445MissingParameter
    Public Sub First(a As String, b As String, c As Integer)
        Call mySuperFunction(7, , New Object())
    End Sub


    Private Sub mySuperFunction(intSomething As Integer, Optional p As Object = Nothing, Optional optionalSomething As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Class", @"using System;

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
}");
        }

        [Fact]
        public async Task ExternalReferenceToOutParameterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim d = New Dictionary(Of string, string)
        Dim s As String
        d.TryGetValue(""a"", s)
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var d = new Dictionary<string, string>();
        string s;
        d.TryGetValue(""a"", out s);
    }
}");
        }

        [Fact]
        public async Task ElvisOperatorExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass3
    Private Class Rec
        Public ReadOnly Property Prop As New Rec
    End Class
    Private Function TestMethod(ByVal str As String) As Rec
        Dim length As Integer = If(str?.Length, -1)
        Console.WriteLine(length)
        Console.ReadKey()
        Return New Rec()?.Prop?.Prop?.Prop
    End Function
End Class", @"using System;

internal partial class TestClass3
{
    private partial class Rec
    {
        public Rec Prop { get; private set; } = new Rec();
    }

    private Rec TestMethod(string str)
    {
        int length = str?.Length ?? -1;
        Console.WriteLine(length);
        Console.ReadKey();
        return new Rec()?.Prop?.Prop?.Prop;
    }
}");
        }

        [Fact]
        public async Task ObjectInitializerExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class StudentName
    Public LastName, FirstName As String
End Class

Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 As StudentName = New StudentName With {.FirstName = ""Craig"", .LastName = ""Playstead""}
    End Sub
End Class", @"
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
}");
        }

        [Fact]
        public async Task ObjectInitializerWithInferredNameAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class Issue480
    Public Foo As Integer

    Sub Test()
        Dim x = New With {Foo}
    End Sub

End Class", @"
internal partial class Issue480
{
    public int Foo;

    public void Test()
    {
        var x = new { Foo };
    }
}");
        }

        [Fact]
        public async Task ObjectInitializerExpression2Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 = New With {Key .FirstName = ""Craig"", Key .LastName = ""Playstead""}
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var student2 = new { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}");
        }

        [Fact]
        public async Task CollectionInitializersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub DoStuff(a As Object)
    End Sub
    Private Sub TestMethod()
        DoStuff({1, 2})
        Dim intList As New List(Of Integer) From {1}
        Dim dict As New Dictionary(Of Integer, Integer) From {{1, 2}, {3, 4}}
    End Sub
End Class", @"using System.Collections.Generic;

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
}");
        }

        [Fact]
        public async Task DelegateExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim test As Func(Of Integer, Integer) = Function(ByVal a As Integer) a * 2
        test(3)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = (a) => a * 2;
        test(3);
    }
}");
        }

        [Fact]
        public async Task LambdaBodyExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim test As Func(Of Integer, Integer) = Function(a) a * 2
        Dim test2 As Func(Of Integer, Integer, Double) = Function(a, b)
            If b > 0 Then Return a / b
            Return 0
        End Function

        Dim test3 As Func(Of Integer, Integer, Integer) = Function(a, b) a Mod b
        test(3)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = a => a * 2;
        Func<int, int, double> test2 = (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0;
        };
        Func<int, int, int> test3 = (a, b) => a % b;
        test(3);
    }
}");
        }

        [Fact]
        public async Task TypeInferredLambdaBodyExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim test = Function(a) a * 2
        Dim test2 = Function(a, b)
            If b > 0 Then Return a / b
            Return 0
        End Function

        Dim test3 = Function(a, b) a Mod b
        test(3)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

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
}");
        }

        [Fact]
        public async Task SingleLineLambdaWithStatementBodyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        Dim simpleAssignmentAction As System.Action = Sub() x = 1
        Dim nonBlockAction As System.Action = Sub() Console.WriteLine(""Statement"")
        Dim ifAction As Action = Sub() If True Then Exit Sub
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int x = 1;
        Action simpleAssignmentAction = () => x = 1;
        Action nonBlockAction = () => Console.WriteLine(""Statement"");
        Action ifAction = () => { if (true) return; };
    }
}");
        }

        [Fact]
        public async Task AwaitAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function SomeAsyncMethod() As Task(Of Integer)
        Return Task.FromResult(0)
    End Function

    Private Async Sub TestMethod()
        Dim result As Integer = Await SomeAsyncMethod()
        Console.WriteLine(result)
    End Sub
End Class", @"using System;
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
}");
        }

        [Fact]
        public async Task NameQualifyingHandlesInheritanceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClassBase
    Sub DoStuff()
    End Sub
End Class
Class TestClass
    Inherits TestClassBase
    Private Sub TestMethod()
        DoStuff()
    End Sub
End Class", @"
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
}");
        }

        [Fact]
        public async Task UsingGlobalImportAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Public Function TestMethod() As String
         Return vbCrLf
    End Function
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public string TestMethod()
    {
        return Constants.vbCrLf;
    }
}");
        }

        [Fact]
        public async Task ValueCapitalisationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"public Enum TestState
one
two
end enum
public class test
private _state as TestState
    Public Property State As TestState
        Get
            Return _state
        End Get
        Set
            If Not _state.Equals(Value) Then
                _state = Value
            End If
        End Set
    End Property
end class", @"
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
}");
        }

        [Fact]
        public async Task ConstLiteralConversionIssue329Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Module Module1
    Const a As Boolean = 1
    Const b As Char = ChrW(1)
    Const c As Single = 1
    Const d As Double = 1
    Const e As Decimal = 1
    Const f As SByte = 1
    Const g As Short = 1
    Const h As Integer = 1
    Const i As Long = 1
    Const j As Byte = 1
    Const k As UInteger = 1
    Const l As UShort = 1
    Const m As ULong = 1
    Const Nl As String = ChrW(13) + ChrW(10)

    Sub Main()
        Const x As SByte = 4
    End Sub
End Module", @"
internal static partial class Module1
{
    private const bool a = true;
    private const char b = '\u0001';
    private const float c = 1;
    private const double d = 1;
    private const decimal e = 1;
    private const sbyte f = 1;
    private const short g = 1;
    private const int h = 1;
    private const long i = 1;
    private const byte j = 1;
    private const uint k = 1;
    private const ushort l = 1;
    private const ulong m = 1;
    private const string Nl = ""\r\n"";

    public static void Main()
    {
        const sbyte x = 4;
    }
}
");
        }

        [Fact]
        public async Task SelectCaseIssue361Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Module Module1
    Enum E
        A = 1
    End Enum

    Sub Main()
        Dim x = 1
        Select Case x
            Case E.A
                Console.WriteLine(""z"")
        End Select
    End Sub
End Module", @"using System;

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
}");
        }

        [Fact]
        public async Task TupleAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Function GetString(yourBoolean as Boolean) As Boolean
    Return 1 <> 1 OrElse if (yourBoolean, True, False)
End Function",
                @"public bool GetString(bool yourBoolean)
{
    return 1 != 1 || (yourBoolean ? true : false);
}");
        }

        [Fact]
        public async Task UseEventBackingFieldAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Class Foo
    Public Event Bar As EventHandler(Of EventArgs)

    Protected Sub OnBar(e As EventArgs)
        If BarEvent Is Nothing Then
            System.Diagnostics.Debug.WriteLine(""No subscriber"")
        Else
            RaiseEvent Bar(Me, e)
        End If
    End Sub
End Class",
                @"using System;
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
}");
        }

        [Fact]
        public async Task DateTimeToDateAndTimeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim x = DateAdd(""m"", 5, Now)
    End Sub
End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        var x = DateAndTime.DateAdd(""m"", 5, DateAndTime.Now);
    }
}");
        }

        [Fact]
        public async Task BaseFinalizeRemovedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class", @"
public partial class Class1
{
    ~Class1()
    {
    }
}");
        }

        [Fact]
        public async Task GlobalNameIssue375Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Module Module1
    Sub Main()
        Dim x = Microsoft.VisualBasic.Timer
    End Sub
End Module", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal static partial class Module1
{
    public static void Main()
    {
        double x = DateAndTime.Timer;
    }
}");
        }

        [Fact]
        public async Task TernaryConversionIssue363Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Module Module1
    Sub Main()
        Dim x As Short = If(True, CShort(50), 100S)
    End Sub
End Module", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal static partial class Module1
{
    public static void Main()
    {
        short x = true ? Conversions.ToShort(50) : Conversions.ToShort(100);
    }
}
");
        }

        [Fact]
        public async Task GenericMethodCalledWithAnonymousTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Class MoreParsing
    Sub DoGet()
        Dim anon = New With {
            .ANumber = 5
        }
        Dim sameAnon = Identity(anon)
        Dim repeated = Enumerable.Repeat(anon, 5).ToList()
    End Sub

    Private Function Identity(Of TType)(tInstance As TType) As TType
        Return tInstance
    End Function
End Class",
                @"using System.Linq;

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
}");
        }

    }
}
