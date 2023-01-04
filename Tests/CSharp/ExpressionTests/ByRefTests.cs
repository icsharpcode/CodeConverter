using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ByRefTests : ConverterTestBase
{

    [Fact]
    public async Task OptionalRefDateConstsWithOmittedArgListAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue213
    Const x As Date = #1990-1-1#

    Private Sub Y(Optional ByRef opt As Date = x)
    End Sub

    Private Sub CallsY()
        Y
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public partial class Issue213
{
    private static DateTime x = DateTime.Parse(""1990-01-01"");

    private void Y([Optional, DateTimeConstant(627667488000000000L/* Global.Issue213.x */)] ref DateTime opt)
    {
    }

    private void CallsY()
    {
        DateTime argopt = DateTime.Parse(""1990-01-01"");
        Y(opt: ref argopt);
    }
}");
    }

    [Fact]
    public async Task NullInlineRefArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class VisualBasicClass
  Public Sub UseStuff()
    Stuff(Nothing)
  End Sub

  Public Sub Stuff(ByRef strs As String())
  End Sub
End Class", @"
public partial class VisualBasicClass
{
    public void UseStuff()
    {
        string[] argstrs = null;
        Stuff(ref argstrs);
    }

    public void Stuff(ref string[] strs)
    {
    }
}");
    }

    [Fact]
    public async Task RefArgumentRValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Private Property C1 As Class1
    Private _c2 As Class1
    Private _o1 As Object

    Sub Foo()
        Bar(New Class1)
        Bar(C1)
        Bar(Me.C1)
        Bar(_c2)
        Bar(Me._c2)
        Bar(_o1)
        Bar(Me._o1)
    End Sub

    Sub Bar(ByRef class1)
    End Sub
End Class", @"
public partial class Class1
{
    private Class1 C1 { get; set; }
    private Class1 _c2;
    private object _o1;

    public void Foo()
    {
        object argclass1 = new Class1();
        Bar(ref argclass1);
        object argclass11 = C1;
        Bar(ref argclass11);
        C1 = (Class1)argclass11;
        object argclass12 = C1;
        Bar(ref argclass12);
        C1 = (Class1)argclass12;
        object argclass13 = _c2;
        Bar(ref argclass13);
        _c2 = (Class1)argclass13;
        object argclass14 = _c2;
        Bar(ref argclass14);
        _c2 = (Class1)argclass14;
        Bar(ref _o1);
        Bar(ref _o1);
    }

    public void Bar(ref object class1)
    {
    }
}");
    }

    [Fact]
    public async Task NestedRefArgumentRValueIssue876Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue876
    Property SomeProperty As Integer
    Property SomeProperty2 As Integer
    Property SomeProperty3 As Integer

    Public Function InlineAssignHelper(Of T)(ByRef lhs As T, ByVal rhs As T) As T
        lhs = rhs
        Return lhs
    End Function

    Public Sub Main()
        Dim result = InlineAssignHelper(SomeProperty, InlineAssignHelper(SomeProperty2, InlineAssignHelper(SomeProperty3, 1)))
    End Sub
End Class", @"
public partial class Issue876
{
    public int SomeProperty { get; set; }
    public int SomeProperty2 { get; set; }
    public int SomeProperty3 { get; set; }

    public T InlineAssignHelper<T>(ref T lhs, T rhs)
    {
        lhs = rhs;
        return lhs;
    }

    public void Main()
    {
        int localInlineAssignHelper() { int arglhs = SomeProperty3; var ret = InlineAssignHelper(ref arglhs, 1); SomeProperty3 = arglhs; return ret; }

        int localInlineAssignHelper1() { int arglhs1 = SomeProperty2; var ret = InlineAssignHelper(ref arglhs1, localInlineAssignHelper()); SomeProperty2 = arglhs1; return ret; }

        int arglhs = SomeProperty;
        int result = InlineAssignHelper(ref arglhs, localInlineAssignHelper1());
        SomeProperty = arglhs;
    }
}");
    }

    [Fact]
    public async Task RefArgumentRValue2Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim x = True
        Bar(x = True)
    End Sub

    Function Foo2()
        Return Bar(True = False)
    End Function

    Sub Foo3()
        If Bar(True = False) Then Bar(True = False)
    End Sub

    Sub Foo4()
        If Bar(True = False) Then
            Bar(True = False)
        ElseIf Bar(True = False) Then
            Bar(True = False)
        Else
            Bar(True = False)
        End If
    End Sub

    Sub Foo5()
        Bar(Nothing)
    End Sub

    Function Bar(ByRef b As Boolean) As Boolean
            Return True
    End Function

    Function Bar2(ByRef c1 As Class1) As Integer
        If c1 IsNot Nothing AndAlso Len(Bar3(Me)) <> 0 Then
            Return 1
        End If
        Return 0
    End Function

    Function Bar3(ByRef c1 As Class1) As String
        Return """"
    End Function

End Class", @"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        bool x = true;
        bool argb = x == true;
        Bar(ref argb);
    }

    public object Foo2()
    {
        bool argb = true == false;
        return Bar(ref argb);
    }

    public void Foo3()
    {
        bool argb1 = true == false;
        if (Bar(ref argb1))
        {
            bool argb = true == false;
            Bar(ref argb);
        }
    }

    public void Foo4()
    {
        bool argb3 = true == false;
        bool argb4 = true == false;
        if (Bar(ref argb3))
        {
            bool argb = true == false;
            Bar(ref argb);
        }
        else if (Bar(ref argb4))
        {
            bool argb2 = true == false;
            Bar(ref argb2);
        }
        else
        {
            bool argb1 = true == false;
            Bar(ref argb1);
        }
    }

    public void Foo5()
    {
        bool argb = default;
        Bar(ref argb);
    }

    public bool Bar(ref bool b)
    {
        return true;
    }

    public int Bar2(ref Class1 c1)
    {
        var argc1 = this;
        if (c1 is not null && Strings.Len(Bar3(ref argc1)) != 0)
        {
            return 1;
        }
        return 0;
    }

    public string Bar3(ref Class1 c1)
    {
        return """";
    }

}");
    }

    [Fact]
    public async Task RefArgumentUsingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data.SqlClient

Public Class Class1
    Sub Foo()
        Using x = New SqlConnection
            Bar(x)
        End Using
    End Sub
    Sub Bar(ByRef x As SqlConnection)

    End Sub
End Class", @"using System.Data.SqlClient;

public partial class Class1
{
    public void Foo()
    {
        using (var x = new SqlConnection())
        {
            var argx = x;
            Bar(ref argx);
        }
    }
    public void Bar(ref SqlConnection x)
    {

    }
}");
    }

    [Fact]
    public async Task RefOptionalArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class OptionalRefIssue91
    Public Shared Function TestSub(Optional ByRef IsDefault As Boolean = False) As Boolean
    End Function

    Public Shared Function CallingFunc() As Boolean
        Return TestSub() AndAlso TestSub(True)
    End Function
End Class", @"using System.Runtime.InteropServices;

public partial class OptionalRefIssue91
{
    public static bool TestSub([Optional, DefaultParameterValue(false)] ref bool IsDefault)
    {
        return default;
    }

    public static bool CallingFunc()
    {
        bool argIsDefault = false;
        bool argIsDefault1 = true;
        return TestSub(IsDefault: ref argIsDefault) && TestSub(ref argIsDefault1);
    }
}");
    }

    [Fact]
    public async Task OutOptionalArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class OptionalOutIssue882
    Private Sub TestSub(<Out> ByRef a As Integer, <Out> Optional ByRef b As Integer = Nothing)
        a = 42
        b = 23
    End Sub

    Public Sub CallingFunc()
        Dim a As Integer
        Dim b As Integer
        TestSub(a:=a, b:=b)
        TestSub(a:=a)
    End Sub
End Class", @"using System.Runtime.InteropServices;

public partial class OptionalOutIssue882
{
    private void TestSub(out int a, [Optional, DefaultParameterValue(default(int))] out int b)
    {
        a = 42;
        b = 23;
    }

    public void CallingFunc()
    {
        int a;
        int b;
        TestSub(a: out a, b: out b);
        int argb = 0;
        TestSub(a: out a, b: out argb);
    }
}");
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalRefParametersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Interface IFoo
  Function ExplicitFunc(Optional ByRef str2 As String = """") As Integer
End Interface

Public Class Foo
  Implements IFoo

  Private Function ExplicitFunc(Optional ByRef str As String = """") As Integer Implements IFoo.ExplicitFunc
    Return 5
  End Function
End Class", @"using System.Runtime.InteropServices;

public partial interface IFoo
{
    int ExplicitFunc([Optional, DefaultParameterValue("""")] ref string str2);
}

public partial class Foo : IFoo
{

    private int ExplicitFunc([Optional, DefaultParameterValue("""")] ref string str)
    {
        return 5;
    }

    int IFoo.ExplicitFunc(ref string str) => ExplicitFunc(ref str);
}");
    }

    [Fact]
    public async Task RefArgumentPropertyInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Private _p1 As Class1 = Foo(New Class1)
    Public Shared Function Foo(ByRef c1 As Class1) As Class1
        Return c1
    End Function
End Class", @"
public partial class Class1
{
    static Class1 Foo__p1()
    {
        var argc1 = new Class1();
        return Foo(ref argc1);
    }

    private Class1 _p1 = Foo__p1();
    public static Class1 Foo(ref Class1 c1)
    {
        return c1;
    }
}");
    }

    [Fact]
    public async Task ReadOnlyPropertyRef_Issue843Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Module Module1

    Public Class TestClass
        Public ReadOnly Property Foo As String

        Public Sub New()
            Foo = ""abc""
        End Sub
    End Class

    Sub Main()
        Test02()
    End Sub

    Private Sub Test02()
        Dim t As New TestClass
        Test02Sub(t.Foo)
    End Sub

    Private Sub Test02Sub(ByRef value As String)
        Console.WriteLine(value)
    End Sub

End Module", @"using System;

internal static partial class Module1
{

    public partial class TestClass
    {
        public string Foo { get; private set; }

        public TestClass()
        {
            Foo = ""abc"";
        }
    }

    public static void Main()
    {
        Test02();
    }

    private static void Test02()
    {
        var t = new TestClass();
        string argvalue = t.Foo;
        Test02Sub(ref argvalue);
    }

    private static void Test02Sub(ref string value)
    {
        Console.WriteLine(value);
    }

}");
    }

    [Fact]
    public async Task AssignsBackToPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Class MyTestClass

    Private Property Prop As Integer
    Private Property Prop2 As Integer
        
    Private Function TakesRef(ByRef vrbTst As Integer) As Boolean
        vrbTst = Prop + 1
            Return vrbTst > 3
    End Function
        
    Private Sub TakesRefVoid(ByRef vrbTst As Integer)
        vrbTst = vrbTst + 1
    End Sub

    Public Sub UsesRef(someBool As Boolean, someInt As Integer)

        TakesRefVoid(someInt) ' Convert directly
        TakesRefVoid(1) 'Requires variable before
        TakesRefVoid(Prop2) ' Requires variable before, and to assign back after
                
        Dim a = TakesRef(someInt) ' Convert directly
        Dim b = TakesRef(2) 'Requires variable before
        Dim c = TakesRef(Prop) ' Requires variable before, and to assign back after

        If 16 > someInt OrElse TakesRef(someInt) ' Convert directly
            Console.WriteLine(1)    
        Else If someBool AndAlso TakesRef(3 * a) 'Requires variable before (in local function)
            someInt += 1
        Else If TakesRef(Prop) ' Requires variable before, and to assign back after (in local function)
            someInt -=2
        End If
        Console.WriteLine(someInt)
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class MyTestClass
{

    private int Prop { get; set; }
    private int Prop2 { get; set; }

    private bool TakesRef(ref int vrbTst)
    {
        vrbTst = Prop + 1;
        return vrbTst > 3;
    }

    private void TakesRefVoid(ref int vrbTst)
    {
        vrbTst = vrbTst + 1;
    }

    public void UsesRef(bool someBool, int someInt)
    {

        TakesRefVoid(ref someInt); // Convert directly
        int argvrbTst = 1;
        TakesRefVoid(ref argvrbTst); // Requires variable before
        int argvrbTst1 = Prop2;
        TakesRefVoid(ref argvrbTst1);
        Prop2 = argvrbTst1; // Requires variable before, and to assign back after

        bool a = TakesRef(ref someInt); // Convert directly
        int argvrbTst2 = 2;
        bool b = TakesRef(ref argvrbTst2); // Requires variable before
        int argvrbTst3 = Prop;
        bool c = TakesRef(ref argvrbTst3);
        Prop = argvrbTst3; // Requires variable before, and to assign back after

        bool localTakesRef() { int argvrbTst = 3 * Conversions.ToInteger(a); var ret = TakesRef(ref argvrbTst); return ret; }
        bool localTakesRef1() { int argvrbTst1 = Prop; var ret = TakesRef(ref argvrbTst1); Prop = argvrbTst1; return ret; }

        if (16 > someInt || TakesRef(ref someInt)) // Convert directly
        {
            Console.WriteLine(1);
        }
        else if (someBool && localTakesRef()) // Requires variable before (in local function)
        {
            someInt += 1;
        }
        else if (localTakesRef1()) // Requires variable before, and to assign back after (in local function)
        {
            someInt -= 2;
        }
        Console.WriteLine(someInt);
    }
}");
    }

    [Fact]
    public async Task Issue567Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue567
    Dim arr() As String
    Dim arr2(,) As String

    Sub DoSomething(ByRef str As String)
        str = ""test""
    End Sub

    Sub Main()
        DoSomething(arr(1))
        Debug.Assert(arr(1) = ""test"")
        DoSomething(arr2(2, 2))
        Debug.Assert(arr2(2, 2) = ""test"")
    End Sub

End Class", @"using System.Diagnostics;

public partial class Issue567
{
    private string[] arr;
    private string[,] arr2;

    public void DoSomething(ref string str)
    {
        str = ""test"";
    }

    public void Main()
    {
        DoSomething(ref arr[1]);
        Debug.Assert(arr[1] == ""test"");
        DoSomething(ref arr2[2, 2]);
        Debug.Assert(arr2[2, 2] == ""test"");
    }

}");
    }

    [Fact]
    public async Task Issue567ExtendedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue567
    Sub DoSomething(ByRef str As String)
        lst = New List(Of String)({4.ToString(), 5.ToString(), 6.ToString()})
        lst2 = New List(Of Object)({4.ToString(), 5.ToString(), 6.ToString()})
        str = 999.ToString()
    End Sub

    Sub Main()
        DoSomething(lst(1))
        Debug.Assert(lst(1) = 4.ToString())
        DoSomething(lst2(1))
        Debug.Assert(lst2(1) = 5.ToString())
    End Sub

End Class

Friend Module Other
    Public lst As List(Of String) = New List(Of String)({ 1.ToString(), 2.ToString(), 3.ToString()})
    Public lst2 As List(Of Object) = New List(Of Object)({ 1.ToString(), 2.ToString(), 3.ToString()})
End Module", @"using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue567
{
    public void DoSomething(ref string str)
    {
        Other.lst = new List<string>(new[] { 4.ToString(), 5.ToString(), 6.ToString() });
        Other.lst2 = new List<object>(new[] { 4.ToString(), 5.ToString(), 6.ToString() });
        str = 999.ToString();
    }

    public void Main()
    {
        var tmp = Other.lst;
        string argstr = tmp[1];
        DoSomething(ref argstr);
        tmp[1] = argstr;
        Debug.Assert((Other.lst[1] ?? """") == (4.ToString() ?? """"));
        var tmp1 = Other.lst2;
        string argstr1 = Conversions.ToString(tmp1[1]);
        DoSomething(ref argstr1);
        tmp1[1] = argstr1;
        Debug.Assert(Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(Other.lst2[1], 5.ToString(), false)));
    }

}

internal static partial class Other
{
    public static List<string> lst = new List<string>(new[] { 1.ToString(), 2.ToString(), 3.ToString() });
    public static List<object> lst2 = new List<object>(new[] { 1.ToString(), 2.ToString(), 3.ToString() });
}");
    }

    [Fact]
    public async Task Issue856Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Issue856
    Sub Main()
        Dim decimalTarget As Decimal
        Double.TryParse(""123"", decimalTarget)
        
        Dim longTarget As Long
        Integer.TryParse(""123"", longTarget)
        
        Dim intTarget As Integer
        Long.TryParse(""123"", intTarget)
    End Sub

End Class", @"
public partial class Issue856
{
    public void Main()
    {
        var decimalTarget = default(decimal);
        double argresult = (double)decimalTarget;
        double.TryParse(""123"", out argresult);
        decimalTarget = (decimal)argresult;

        var longTarget = default(long);
        int argresult1 = (int)longTarget;
        int.TryParse(""123"", out argresult1);
        longTarget = argresult1;

        var intTarget = default(int);
        long argresult2 = intTarget;
        long.TryParse(""123"", out argresult2);
        intTarget = (int)argresult2;
    }

}");
    }

    [Fact]
    public async Task OutParameterIsEnforcedByCSharpCompileErrorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Runtime.InteropServices ' Statement removed so comment removed too

Public Class OutParameterIsEnforcedByCSharpCompileError
    Shared Sub LogAndReset(<Out> ByRef arg As Integer)
        System.Console.WriteLine(arg)
    End Sub
End Class", @"using System;

public partial class OutParameterIsEnforcedByCSharpCompileError
{
    public static void LogAndReset(out int arg)
    {
        Console.WriteLine(arg);
    }
}
2 target compilation errors:
CS0269: Use of unassigned out parameter 'arg'
CS0177: The out parameter 'arg' must be assigned to before control leaves the current method");
        // These compile errors are the correct conversion - VB doesn't enforce out parameters not being used for input, or being assigned before output
    }

    [Fact]
    public async Task BinaryExpressionOutParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Runtime.InteropServices ' BUG: Comment lost because overwritten

Public Class BinaryExpressionOutParameter
    Shared Sub Main()
        Dim wide As Object = 7
        Zero(wide)
        Dim narrow As Short = 3
        Zero(narrow)
        Zero(7 + 3)
    End Sub

    Shared Sub Zero(<Out> ByRef arg As Integer)
        arg = 0
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class BinaryExpressionOutParameter
{
    public static void Main()
    {
        object wide = 7;
        int argarg = Conversions.ToInteger(wide);
        Zero(out argarg);
        wide = argarg;
        short narrow = 3;
        int argarg1 = narrow;
        Zero(out argarg1);
        narrow = (short)argarg1;
        int argarg2 = 7 + 3;
        Zero(out argarg2);
    }

    public static void Zero(out int arg)
    {
        arg = 0;
    }
}");
    }

    [Fact]
    public async Task BinaryExpressionRefParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class BinaryExpressionRefParameter
    Shared Sub Main()
        Dim wide As Object = 7
        LogAndReset(wide)
        Dim wideArray() As Object = {3,4,4}
        LogAndReset(wideArray(1))
        Dim narrow As Short = 3
        LogAndReset(narrow)
        LogAndReset(7 + 3)
    End Sub

    Shared Sub LogAndReset(ByRef arg As Integer)
        System.Console.WriteLine(arg)
        arg = 0
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class BinaryExpressionRefParameter
{
    public static void Main()
    {
        object wide = 7;
        int argarg = Conversions.ToInteger(wide);
        LogAndReset(ref argarg);
        wide = argarg;
        object[] wideArray = new object[] { 3, 4, 4 };
        var tmp = wideArray;
        int argarg1 = Conversions.ToInteger(tmp[1]);
        LogAndReset(ref argarg1);
        tmp[1] = argarg1;
        short narrow = 3;
        int argarg2 = narrow;
        LogAndReset(ref argarg2);
        narrow = (short)argarg2;
        int argarg3 = 7 + 3;
        LogAndReset(ref argarg3);
    }

    public static void LogAndReset(ref int arg)
    {
        Console.WriteLine(arg);
        arg = 0;
    }
}");
    }

}