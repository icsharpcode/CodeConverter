using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.CSharp;
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
    public async Task RefAfterOptionalArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Sub S(Optional a As Integer = 0, Optional ByRef b As Integer = 0)
    S()
End Sub
", @"
public void S([Optional, DefaultParameterValue(0)] int a, [Optional, DefaultParameterValue(0)] ref int b)
{
    int argb = 0;
    S(b: ref argb);
}");
    }

    [Fact]
    public async Task DateRefAfterOptionalArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Sub S(Optional ByRef dt As Date = Nothing)
End Sub
", @"
public void S([Optional] ref DateTime dt)
{
}");
    }

    [Fact]
    public async Task ParenthesizedArgShouldNotBeAssignedBackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class C
    Public Sub S()
        Dim i As Integer = 0
        Modify(i)
        System.Diagnostics.Debug.Assert(i = 1)
        Modify((i))
        System.Diagnostics.Debug.Assert(i = 1)
    End Sub

    Sub Modify(ByRef i As Integer)
        i = i + 1
    End Sub
End Class
", @"using System.Diagnostics;

public partial class C
{
    public void S()
    {
        int i = 0;
        Modify(ref i);
        Debug.Assert(i == 1);
        int argi = i;
        Modify(ref argi);
        Debug.Assert(i == 1);
    }

    public void Modify(ref int i)
    {
        i = i + 1;
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
    private void TestSub(out int a, [Optional] out int b)
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

    int IFoo.ExplicitFunc([Optional, DefaultParameterValue("""")] ref string str) => ExplicitFunc(ref str);
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
        Double.TryParse(""123"".AsSpan(), decimalTarget)
        
        Dim longTarget As Long
        Integer.TryParse(""123"".AsSpan(), longTarget)
        
        Dim intTarget As Integer
        Long.TryParse(""123"".AsSpan(), intTarget)
    End Sub

End Class", @"using System;

public partial class Issue856
{
    public void Main()
    {
        var decimalTarget = default(decimal);
        double argresult = (double)decimalTarget;
        double.TryParse(""123"".AsSpan(), out argresult);
        decimalTarget = (decimal)argresult;

        var longTarget = default(long);
        int argresult1 = (int)longTarget;
        int.TryParse(""123"".AsSpan(), out argresult1);
        longTarget = argresult1;

        var intTarget = default(int);
        long argresult2 = intTarget;
        long.TryParse(""123"".AsSpan(), out argresult2);
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

    [Fact]
    public async Task Issue1225_OutAttributeOnParameterFromOtherFileDoesNotCrashAsync()
    {
        // Issue #1225: IsOutAttribute called SemanticModel.GetTypeInfo(attribute) on an attribute node
        // that belongs to a different syntax tree (parameter declared in a separate source file).
        // This caused ArgumentException "Knoten ist nicht innerhalb Syntaxbaum" /
        // "Node is not within syntax tree". The fix falls back to name-based checking
        // when the attribute's syntax tree differs from the current semantic model's tree.
        var serviceFileContent = @"Imports System.Runtime.InteropServices
Public Class LicenseService
    Public Shared ReadOnly Property Instance As LicenseService
        Get
            Return New LicenseService()
        End Get
    End Property

    Public Function GetLicenseMaybe(<Out> ByRef licenseName As String) As Boolean
        licenseName = Nothing
        Return False
    End Function
End Class";

        var callerFileContent = @"Public Class Caller
    Public Sub Test(licenseName As String)
        Dim res = LicenseService.Instance.GetLicenseMaybe(licenseName)
    End Sub
End Class";

        var options = new TextConversionOptions(DefaultReferences.With()) { ShowCompilationErrors = true };
        var languageConversion = new VBToCSConversion { ConversionOptions = options };
        var serviceTree = languageConversion.CreateTree(serviceFileContent);
        var callerTree = languageConversion.CreateTree(callerFileContent);

        // Add both files to the same project so that the VB compilation sees both
        var serviceDoc = await languageConversion.CreateProjectDocumentFromTreeAsync(serviceTree, options.References);
        var callerDoc = serviceDoc.Project.AddDocumentFromTree(callerTree);

        // Convert only the caller document; its parameter info comes from the service file's syntax tree
        var result = await ProjectConversion.ConvertSingleAsync<VBToCSConversion>(callerDoc, options);
        var output = (result.ConvertedCode ?? "") + (result.GetExceptionsAsString() ?? "");

        Assert.DoesNotContain("#error", output);
        Assert.Contains("GetLicenseMaybe", output);
    }

    [Fact]
    public async Task OptionalStructRefParameterUsesDefaultNotNullIssue886Async()
    {
        // Issue #886: When a VB method has an optional ByRef struct parameter with Nothing as the default,
        // the generated C# should use `default` rather than `null` (null is not valid for value types).
        await TestConversionVisualBasicToCSharpAsync(@"
Structure S
End Structure
Class C
    Sub Foo(Optional ByRef s As S = Nothing)
    End Sub
    Sub Bar()
        Foo()
    End Sub
End Class
", @"using System.Runtime.InteropServices;

internal partial struct S
{
}

internal partial class C
{
    public void Foo([Optional] ref S s)
    {
    }
    public void Bar()
    {
        S args = default;
        Foo(s: ref args);
    }
}");
    }

    [Fact]
    public async Task WritesBackBeforeJumpingIssue896Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Public Class C
    Public Property P As Integer

    Function Inc(ByRef v As Integer) As Integer
        v += 1
        Return v
    End Function

    Function CreateException(ByRef v As Integer) As Exception
        Return New Exception()
    End Function

    Function ReturnsWrittenBack() As Long
        Return Inc(P)
    End Function

    Function ReturnsWithoutWriteBack() As Integer
        Return Inc(New Integer)
    End Function

    Iterator Function YieldsWrittenBack() As IEnumerable(Of Integer)
        Yield Inc(P)
    End Function

    Sub ThrowsWrittenBack()
        Throw CreateException(P)
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class C
{
    public int P { get; set; }

    public int Inc(ref int v)
    {
        v += 1;
        return v;
    }

    public Exception CreateException(ref int v)
    {
        return new Exception();
    }

    public long ReturnsWrittenBack()
    {
        int argv = P;
        long ret = Inc(ref argv);
        P = argv;
        return ret;
    }

    public int ReturnsWithoutWriteBack()
    {
        int argv = new int();
        return Inc(ref argv);
    }

    public IEnumerable<int> YieldsWrittenBack()
    {
        int argv = P;
        var ret = Inc(ref argv);
        P = argv;
        yield return ret;
    }

    public void ThrowsWrittenBack()
    {
        int argv = P;
        var ex = CreateException(ref argv);
        P = argv;
        throw ex;
    }
}");
    }

    [Fact]
    public async Task WritesBackBeforeBlockBodyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Property P As Integer

    Function Inc(ByRef v As Integer) As Integer
        v += 1
        Return v
    End Function

    Function Arr(ByRef v As Integer) As Integer()
        Return New Integer() {v}
    End Function

    Function D(ByRef v As Integer) As IDisposable
        Return Nothing
    End Function

    Function IsOne(ByRef v As Integer) As Boolean
        Return v = 1
    End Function

    Sub BlockHeaders()
        Select Case Inc(P)
            Case 1
                Console.WriteLine(1)
        End Select
        For i = 0 To Inc(P)
            Console.WriteLine(i)
        Next
        For Each item In Arr(P)
            Console.WriteLine(item)
        Next
        Using dd = D(P)
            Console.WriteLine(3)
        End Using
        SyncLock Arr(P)
            Console.WriteLine(4)
        End SyncLock
        With Arr(P)
            Console.WriteLine(.Length)
        End With
        Try
        Catch ex As Exception When IsOne(P)
        End Try
    End Sub

    Sub BlockHeaderWithoutWriteBack()
        Select Case Inc(New Integer)
            Case 1
                Console.WriteLine(1)
        End Select
    End Sub
End Class", @"using System;

public partial class C
{
    public int P { get; set; }

    public int Inc(ref int v)
    {
        v += 1;
        return v;
    }

    public int[] Arr(ref int v)
    {
        return new int[] { v };
    }

    public IDisposable D(ref int v)
    {
        return null;
    }

    public bool IsOne(ref int v)
    {
        return v == 1;
    }

    public void BlockHeaders()
    {
        int localInc() { int argv = P; var ret = Inc(ref argv); P = argv; return ret; }

        switch (localInc())
        {
            case 1:
                Console.WriteLine(1);
                break;
        }
        int localInc1() { int argv1 = P; var ret = Inc(ref argv1); P = argv1; return ret; }

        for (int i = 0, loopTo = localInc1(); i <= loopTo; i++)
            Console.WriteLine(i);
        int[] localArr() { int argv2 = P; var ret = Arr(ref argv2); P = argv2; return ret; }

        foreach (var item in localArr())
            Console.WriteLine(item);
        IDisposable localD() { int argv3 = P; var ret = D(ref argv3); P = argv3; return ret; }

        using (var dd = localD())
        {
            Console.WriteLine(3);
        }
        int[] localArr1() { int argv4 = P; var ret = Arr(ref argv4); P = argv4; return ret; }

        lock (localArr1())
            Console.WriteLine(4);
        int[] localArr2() { int argv5 = P; var ret = Arr(ref argv5); P = argv5; return ret; }

        {
            var withBlock = localArr2();
            Console.WriteLine(withBlock.Length);
        }
        bool localIsOne() { int argv6 = P; var ret = IsOne(ref argv6); P = argv6; return ret; }

        try
        {
        }
        catch (Exception ex) when (localIsOne())
        {
        }
    }

    public void BlockHeaderWithoutWriteBack()
    {
        int argv = new int();
        switch (Inc(ref argv))
        {
            case 1:
                Console.WriteLine(1);
                break;
        }
    }
}");
    }

    [Fact]
    public async Task OmittedOptionalRefInInstanceFieldInitializerIssue1081Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class FieldInitializationTest
    Private First As New Inner()
    Private Second As New Outer(First)
End Class

Public Class Inner
End Class

Public Class Outer
    Sub New(inner As Inner, Optional ByRef x As String = """")
    End Sub
End Class", @"using System.Runtime.InteropServices;

public partial class FieldInitializationTest
{
    private Inner First = new Inner();

    public FieldInitializationTest()
    {
        Second = initSecond();
    }
    Outer initSecond()
    {
        string argx = """";
        return new Outer(First, x: ref argx);
    }

    private Outer Second;
}

public partial class Inner
{
}

public partial class Outer
{
    public Outer(Inner inner, [Optional, DefaultParameterValue("""")] ref string x)
    {
    }
}");
    }

    [Fact]
    public async Task FieldInitializersWriteBackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Property P As Integer
    Private Shared Property SP As Integer
    Private _a As Integer = Inc(P)
    Private Shared _c As Integer = Inc(SP)

    Shared Function Inc(ByRef v As Integer) As Integer
        v += 1
        Return v
    End Function
End Class", @"
public partial class C
{
    public int P { get; set; }
    private static int SP { get; set; }

    public C()
    {
        _a = Inc__a();
    }
    int Inc__a()
    {
        int argv = P;
        int ret = Inc(ref argv);
        P = argv;
        return ret;
    }

    private int _a;
    static int Inc__c()
    {
        int argv = SP;
        int ret = Inc(ref argv);
        SP = argv;
        return ret;
    }

    private static int _c = Inc__c();

    public static int Inc(ref int v)
    {
        v += 1;
        return v;
    }
}");
    }

    [Fact]
    public async Task FieldInitializerMethodNamesAvoidExistingMembersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Property P As Integer
    Private w As Integer = Inc(P) + 1
    Private v As Integer = Inc(P)
    Private Inc_v As Integer

    Function initW() As Integer
        Return 0
    End Function

    Shared Function Inc(ByRef x As Integer) As Integer
        x += 1
        Return x
    End Function
End Class", @"
public partial class C
{
    public int P { get; set; }

    public C()
    {
        w = initW1();
        v = Inc_v1();
    }
    int initW1()
    {
        int localInc() { int argx = P; var ret = Inc(ref argx); P = argx; return ret; }

        return localInc() + 1;
    }

    private int w;
    int Inc_v1()
    {
        int argx = P;
        int ret = Inc(ref argx);
        P = argx;
        return ret;
    }

    private int v;
    private int Inc_v;

    public int initW()
    {
        return 0;
    }

    public static int Inc(ref int x)
    {
        x += 1;
        return x;
    }
}");
    }

    [Fact]
    public async Task WritesBackBeforeAssignmentCompletesWhenItChangesTheWriteBackTargetAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Property P As String
    Public Property Q As String

    Function F(ByRef v As String) As String
        Return v
    End Function

    Function G(ByRef v As String) As C
        Return New C
    End Function

    Sub Assignments(h As C)
        P = F(P)
        h.Q = F(h.Q)
        h = G(h.P)
        Q = F(P)
    End Sub
End Class", @"
public partial class C
{
    public string P { get; set; }
    public string Q { get; set; }

    public string F(ref string v)
    {
        return v;
    }

    public C G(ref string v)
    {
        return new C();
    }

    public void Assignments(C h)
    {
        string argv = P;
        var ret = F(ref argv);
        P = argv;
        P = ret;
        string localF() { string argv = h.Q; var ret = F(ref argv); h.Q = argv; return ret; }

        h.Q = localF();
        string argv1 = h.P;
        var ret1 = G(ref argv1);
        h.P = argv1;
        h = ret1;
        string argv2 = P;
        Q = F(ref argv2);
        P = argv2;
    }
}");
    }

    [Fact]
    public async Task StructureKeepsStatementPlacementWhereALocalFunctionWouldNeedThisAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Structure S
    Public Property P As Integer

    Shared Function Inc(ByRef v As Integer) As Integer
        v += 1
        Return v
    End Function

    Sub M()
        Select Case Inc(P)
            Case 1
                Console.WriteLine(1)
        End Select
        Dim a = Inc(P), b = Inc(P)
        P += Inc(P)
    End Sub
End Structure", @"using System;

public partial struct S
{
    public int P { get; set; }

    public static int Inc(ref int v)
    {
        v += 1;
        return v;
    }

    public void M()
    {
        int argv = P;
        switch (Inc(ref argv))
        {
            case 1:
                Console.WriteLine(1);
                break;
        }
        P = argv;
        int argv1 = P;
        int argv2 = P;
        int a = Inc(ref argv1);
        int b = Inc(ref argv2);
        P = argv1;
        P = argv2;
        int argv3 = P;
        P += Inc(ref argv3);
        P = argv3;
    }
}", incompatibleWithAutomatedCommentTesting: true); // The write-back after the block moves line comments out of order
    }

    [Fact]
    public async Task StructureKeepsStatementPlacementForOperandsThatWouldNeedThisAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Structure S
    Public Property P As Integer

    Shared Function Inc(ByRef v As Integer) As Integer
        v += 1
        Return v
    End Function

    Sub M()
        Me.P = Inc(Me.P) + 1
        Dim x = 1 + Inc(P)
        P = Inc(Inc(P))
    End Sub
End Structure", @"
public partial struct S
{
    public int P { get; set; }

    public static int Inc(ref int v)
    {
        v += 1;
        return v;
    }

    public void M()
    {
        int argv = P;
        var ret = Inc(ref argv) + 1;
        P = argv;
        P = ret;
        int argv1 = P;
        int x = 1 + Inc(ref argv1);
        P = argv1;
        int argv2 = P;
        int argv3 = Inc(ref argv2);
        var ret1 = Inc(ref argv3);
        P = argv2;
        P = ret1;
    }
}");
    }

    [Fact]
    public async Task LateBoundAssignmentTargetTakesTheLocalFunctionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Option Strict Off

Public Class C
    Public Property P As Integer

    Shared Function Inc(ByRef v As Integer) As Integer
        v += 1
        Return v
    End Function

    Sub M(o As Object)
        o.X = Inc(P)
    End Sub
End Class", @"
public partial class C
{
    public int P { get; set; }

    public static int Inc(ref int v)
    {
        v += 1;
        return v;
    }

    public void M(object o)
    {
        int localInc() { int argv = P; var ret = Inc(ref argv); P = argv; return ret; } ((dynamic)o).X = localInc();
    }
}", incompatibleWithAutomatedCommentTesting: true); // The late-bound assignment is emitted on the local function's line
    }

    [Fact]
    public async Task ParameterizedPropertyIsWrittenBackThroughItsSetterIssue909Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class ClsDb
    Private ReadOnly _recordValues(10, 30) As String

    Public Sub Get_Equal(ByRef tableBuffer As Integer, ByRef keyIndex As Integer, ByRef keyValue As String, ByRef recordPosition As String, ByRef errorNumber As Integer)
        keyValue = ""found""
    End Sub

    Public Property Qr(ByVal tableBuffer As Integer, ByVal fieldNumber As Integer) As String
        Get
            Return _recordValues(tableBuffer, fieldNumber)
        End Get
        Set(ByVal value As String)
            _recordValues(tableBuffer, fieldNumber) = value
        End Set
    End Property
End Class

Module Euro_Str
    Public Db As New ClsDb
    Public QAdr As Integer
    Public CourseBuffer As Integer
End Module

Public Class Caller
    Sub Find(recordPositionQAdr As String, errorCode As Integer)
        Db.Get_Equal(QAdr, 0, Db.Qr(CourseBuffer, 21), recordPositionQAdr, errorCode)
    End Sub
End Class", @"
public partial class ClsDb
{
    private readonly string[,] _recordValues = new string[11, 31];

    public void Get_Equal(ref int tableBuffer, ref int keyIndex, ref string keyValue, ref string recordPosition, ref int errorNumber)
    {
        keyValue = ""found"";
    }

    public string get_Qr(int tableBuffer, int fieldNumber)
    {
        return _recordValues[tableBuffer, fieldNumber];
    }
    public void set_Qr(int tableBuffer, int fieldNumber, string value)
    {
        _recordValues[tableBuffer, fieldNumber] = value;
    }
}

internal static partial class Euro_Str
{
    public static ClsDb Db = new ClsDb();
    public static int QAdr;
    public static int CourseBuffer;
}

public partial class Caller
{
    public void Find(string recordPositionQAdr, int errorCode)
    {
        int argkeyIndex = 0;
        string argkeyValue = Euro_Str.Db.get_Qr(Euro_Str.CourseBuffer, 21);
        Euro_Str.Db.Get_Equal(ref Euro_Str.QAdr, ref argkeyIndex, ref argkeyValue, ref recordPositionQAdr, ref errorCode);
        Euro_Str.Db.set_Qr(Euro_Str.CourseBuffer, 21, argkeyValue);
    }
}");
    }

    [Fact]
    public async Task ByRefPropertyTargetEvaluatesReceiverAndIndexesOnceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private _index As Integer

    Public Property Qr(a As Integer, b As Integer) As String
        Get
            Return """"
        End Get
        Set(value As String)
        End Set
    End Property

    Default Public Property Item(i As Integer) As String
        Get
            Return """"
        End Get
        Set(value As String)
        End Set
    End Property

    Public Property Plain As String

    Function NextIndex() As Integer
        Return 1
    End Function

    Function GetC() As C
        Return Me
    End Function

    Shared Sub SetValue(ByRef value As String)
    End Sub

    Shared Sub SetValueAndIndex(ByRef value As String, ByRef index As Integer)
    End Sub

    Sub UsesTwice(other As C)
        Dim i = 1
        SetValue(Qr((i), 2)) ' Locals, constants and fields are evaluated again
        SetValue(other.Qr(_index, NextIndex()))
        SetValue(GetC().Qr(1, 2))
        SetValue(GetC().Plain)
        SetValue(other(NextIndex()))
        SetValueAndIndex(other.Qr(i, 2), i) ' i is written by the call
    End Sub
End Class", @"
public partial class C
{
    private int _index;

    public string get_Qr(int a, int b)
    {
        return """";
    }
    public void set_Qr(int a, int b, string value)
    {
    }

    public string this[int i]
    {
        get
        {
            return """";
        }
        set
        {
        }
    }

    public string Plain { get; set; }

    public int NextIndex()
    {
        return 1;
    }

    public C GetC()
    {
        return this;
    }

    public static void SetValue(ref string value)
    {
    }

    public static void SetValueAndIndex(ref string value, ref int index)
    {
    }

    public void UsesTwice(C other)
    {
        int i = 1;
        string argvalue = get_Qr(i, 2);
        SetValue(ref argvalue);
        set_Qr(i, 2, argvalue); // Locals, constants and fields are evaluated again
        var tmp = NextIndex();
        string argvalue1 = other.get_Qr(_index, tmp);
        SetValue(ref argvalue1);
        other.set_Qr(_index, tmp, argvalue1);
        var tmp1 = GetC();
        string argvalue2 = tmp1.get_Qr(1, 2);
        SetValue(ref argvalue2);
        tmp1.set_Qr(1, 2, argvalue2);
        var tmp2 = GetC();
        string argvalue3 = tmp2.Plain;
        SetValue(ref argvalue3);
        tmp2.Plain = argvalue3;
        var tmp3 = other;
        var tmp4 = NextIndex();
        string argvalue4 = tmp3[tmp4];
        SetValue(ref argvalue4);
        tmp3[tmp4] = argvalue4;
        var tmp5 = i;
        string argvalue5 = other.get_Qr(tmp5, 2);
        SetValueAndIndex(ref argvalue5, ref i);
        other.set_Qr(tmp5, 2, argvalue5); // i is written by the call
    }
}");
    }

    [Fact]
    public async Task ByRefPropertyTargetCopiesOnlyFieldsThatCanChangeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Db
    Public Property Qr(a As Integer, b As Integer) As String
        Get
            Return """"
        End Get
        Set(value As String)
        End Set
    End Property
End Class

Public Class Fields
    Private ReadOnly _readOnlyIndex As Integer = 1
    Private _neverWritten As Integer
    Private _assignedInConstructor As Integer
    Private _writtenElsewhere As Integer
    Public PublicIndex As Integer
    Private _db As New Db

    Public Sub New()
        _assignedInConstructor = 2
    End Sub

    Sub Reset()
        _writtenElsewhere = 0
    End Sub

    Sub UsesFields()
        SetValue(_db.Qr(_readOnlyIndex, _neverWritten))
        SetValue(_db.Qr(_assignedInConstructor, _writtenElsewhere))
        SetValue(_db.Qr(PublicIndex, 1))
    End Sub

    Shared Sub SetValue(ByRef value As String)
    End Sub
End Class", @"
public partial class Db
{
    public string get_Qr(int a, int b)
    {
        return """";
    }
    public void set_Qr(int a, int b, string value)
    {
    }
}

public partial class Fields
{
    private readonly int _readOnlyIndex = 1;
    private int _neverWritten;
    private int _assignedInConstructor;
    private int _writtenElsewhere;
    public int PublicIndex;
    private Db _db = new Db();

    public Fields()
    {
        _assignedInConstructor = 2;
    }

    public void Reset()
    {
        _writtenElsewhere = 0;
    }

    public void UsesFields()
    {
        string argvalue = _db.get_Qr(_readOnlyIndex, _neverWritten);
        SetValue(ref argvalue);
        _db.set_Qr(_readOnlyIndex, _neverWritten, argvalue);
        var tmp = _writtenElsewhere;
        string argvalue1 = _db.get_Qr(_assignedInConstructor, tmp);
        SetValue(ref argvalue1);
        _db.set_Qr(_assignedInConstructor, tmp, argvalue1);
        var tmp1 = PublicIndex;
        string argvalue2 = _db.get_Qr(tmp1, 1);
        SetValue(ref argvalue2);
        _db.set_Qr(tmp1, 1, argvalue2);
    }

    public static void SetValue(ref string value)
    {
    }
}");
    }

    [Fact]
    public async Task ByRefPropertyTargetCopiesFieldsWrittenAfterConstructionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Option Strict Off

Public Class Db
    Public Property Qr(a As Object) As String
        Get
            Return """"
        End Get
        Set(value As String)
        End Set
    End Property
End Class

Public Structure Point
    Public X As Integer
End Structure

Public Class C
    Private _forVar As Integer
    Private _forEachVar As Integer
    Private _reDimmed As Integer()
    Private _erased As Integer()
    Private _midText As String
    Private _lateBoundArg As Integer
    Private _writtenInConstructorLambda As Integer
    Private _writtenOnOtherInstance As Integer
    Private Shared _sharedWrittenInInstanceConstructor As Integer
    Private _byInitializer As Integer
    Private _initialized As Integer = Inc(_byInitializer)
    Private Shared _sharedByInitializer As Integer
    Private Shared _sharedInitialized As Integer = Inc(_sharedByInitializer)
    Private Shared _sharedInSharedConstructor As Integer
    Friend _friendNeverWritten As Integer
    Private _point As Point
    Private ReadOnly _db As New Db

    Shared Sub New()
        _sharedInSharedConstructor = 1
    End Sub

    Public Sub New(other As C)
        Dim a As Action = Sub() _writtenInConstructorLambda = 1
        other._writtenOnOtherInstance = 1
        _sharedWrittenInInstanceConstructor = 1
    End Sub

    Sub Writes(o As Object)
        For _forVar = 1 To 2
        Next
        For Each _forEachVar In {1}
        Next
        ReDim _reDimmed(3)
        Erase _erased
        Mid(_midText, 1) = ""x""
        o.Foo(_lateBoundArg)
    End Sub

    Sub UsesFields()
        SetValue(_db.Qr(_forVar))
        SetValue(_db.Qr(_forEachVar))
        SetValue(_db.Qr(_reDimmed))
        SetValue(_db.Qr(_erased))
        SetValue(_db.Qr(_midText))
        SetValue(_db.Qr(_lateBoundArg))
        SetValue(_db.Qr(_writtenInConstructorLambda))
        SetValue(_db.Qr(_writtenOnOtherInstance))
        SetValue(_db.Qr(_sharedWrittenInInstanceConstructor))
        SetValue(_db.Qr(_byInitializer)) ' Reused from here on
        SetValue(_db.Qr(_sharedByInitializer))
        SetValue(_db.Qr(_sharedInSharedConstructor))
        SetValue(_db.Qr(_friendNeverWritten))
        SetValue(_db.Qr(_point)) ' A structure can change without being assigned
    End Sub

    Shared Function Inc(ByRef x As Integer) As Integer
        x += 1
        Return x
    End Function

    Shared Sub SetValue(ByRef value As String)
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Db
{
    public string get_Qr(object a)
    {
        return """";
    }
    public void set_Qr(object a, string value)
    {
    }
}

public partial struct Point
{
    public int X;
}

public partial class C
{
    private int _forVar;
    private int _forEachVar;
    private int[] _reDimmed;
    private int[] _erased;
    private string _midText;
    private int _lateBoundArg;
    private int _writtenInConstructorLambda;
    private int _writtenOnOtherInstance;
    private static int _sharedWrittenInInstanceConstructor;
    private int _byInitializer;
    private int _initialized;
    private static int _sharedByInitializer;
    private static int _sharedInitialized = Inc(ref _sharedByInitializer);
    private static int _sharedInSharedConstructor;
    internal int _friendNeverWritten;
    private Point _point;
    private readonly Db _db = new Db();

    static C()
    {
        _sharedInSharedConstructor = 1;
    }

    public C(C other)
    {
        _initialized = Inc(ref _byInitializer);
        Action a = () => _writtenInConstructorLambda = 1;
        other._writtenOnOtherInstance = 1;
        _sharedWrittenInInstanceConstructor = 1;
    }

    public void Writes(object o)
    {
        for (_forVar = 1; _forVar <= 2; _forVar++)
        {
        }
        foreach (var current_forEachVar in new[] { 1 })
            _forEachVar = current_forEachVar;
        _reDimmed = new int[4];
        _erased = null;
        StringType.MidStmtStr(ref _midText, 1, ""x"".Length, ""x"");
        ((dynamic)o).Foo(_lateBoundArg);
    }

    public void UsesFields()
    {
        var tmp = _forVar;
        string argvalue = _db.get_Qr(tmp);
        SetValue(ref argvalue);
        _db.set_Qr(tmp, argvalue);
        var tmp1 = _forEachVar;
        string argvalue1 = _db.get_Qr(tmp1);
        SetValue(ref argvalue1);
        _db.set_Qr(tmp1, argvalue1);
        var tmp2 = _reDimmed;
        string argvalue2 = _db.get_Qr(tmp2);
        SetValue(ref argvalue2);
        _db.set_Qr(tmp2, argvalue2);
        var tmp3 = _erased;
        string argvalue3 = _db.get_Qr(tmp3);
        SetValue(ref argvalue3);
        _db.set_Qr(tmp3, argvalue3);
        var tmp4 = _midText;
        string argvalue4 = _db.get_Qr(tmp4);
        SetValue(ref argvalue4);
        _db.set_Qr(tmp4, argvalue4);
        var tmp5 = _lateBoundArg;
        string argvalue5 = _db.get_Qr(tmp5);
        SetValue(ref argvalue5);
        _db.set_Qr(tmp5, argvalue5);
        var tmp6 = _writtenInConstructorLambda;
        string argvalue6 = _db.get_Qr(tmp6);
        SetValue(ref argvalue6);
        _db.set_Qr(tmp6, argvalue6);
        var tmp7 = _writtenOnOtherInstance;
        string argvalue7 = _db.get_Qr(tmp7);
        SetValue(ref argvalue7);
        _db.set_Qr(tmp7, argvalue7);
        var tmp8 = _sharedWrittenInInstanceConstructor;
        string argvalue8 = _db.get_Qr(tmp8);
        SetValue(ref argvalue8);
        _db.set_Qr(tmp8, argvalue8);
        string argvalue9 = _db.get_Qr(_byInitializer);
        SetValue(ref argvalue9);
        _db.set_Qr(_byInitializer, argvalue9); // Reused from here on
        string argvalue10 = _db.get_Qr(_sharedByInitializer);
        SetValue(ref argvalue10);
        _db.set_Qr(_sharedByInitializer, argvalue10);
        string argvalue11 = _db.get_Qr(_sharedInSharedConstructor);
        SetValue(ref argvalue11);
        _db.set_Qr(_sharedInSharedConstructor, argvalue11);
        string argvalue12 = _db.get_Qr(_friendNeverWritten);
        SetValue(ref argvalue12);
        _db.set_Qr(_friendNeverWritten, argvalue12);
        var tmp9 = _point;
        string argvalue13 = _db.get_Qr(tmp9);
        SetValue(ref argvalue13);
        _db.set_Qr(tmp9, argvalue13); // A structure can change without being assigned
    }

    public static int Inc(ref int x)
    {
        x += 1;
        return x;
    }

    public static void SetValue(ref string value)
    {
    }
}");
    }

    [Fact]
    public async Task ByRefPropertyTargetCopiesFriendFieldsVisibleToFriendAssembliesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Other"")>

Public Class Db
    Public Property Qr(a As Integer) As String
        Get
            Return """"
        End Get
        Set(value As String)
        End Set
    End Property
End Class

Public Class C
    Friend _friendNeverWritten As Integer
    Private _privateNeverWritten As Integer
    Private Shared _sharedWrittenByAnotherTypesConstructor As Integer
    Private ReadOnly _db As New Db

    Private Class Other
        Sub New()
            C._sharedWrittenByAnotherTypesConstructor = 1
        End Sub
    End Class

    Sub UsesFields()
        SetValue(_db.Qr(_friendNeverWritten))
        SetValue(_db.Qr(_privateNeverWritten))
        SetValue(_db.Qr(_sharedWrittenByAnotherTypesConstructor))
    End Sub

    Shared Sub SetValue(ByRef value As String)
    End Sub
End Class", @"using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(""Other"")]

public partial class Db
{
    public string get_Qr(int a)
    {
        return """";
    }
    public void set_Qr(int a, string value)
    {
    }
}

public partial class C
{
    internal int _friendNeverWritten;
    private int _privateNeverWritten;
    private static int _sharedWrittenByAnotherTypesConstructor;
    private readonly Db _db = new Db();

    private partial class Other
    {
        public Other()
        {
            _sharedWrittenByAnotherTypesConstructor = 1;
        }
    }

    public void UsesFields()
    {
        var tmp = _friendNeverWritten;
        string argvalue = _db.get_Qr(tmp);
        SetValue(ref argvalue);
        _db.set_Qr(tmp, argvalue);
        string argvalue1 = _db.get_Qr(_privateNeverWritten);
        SetValue(ref argvalue1);
        _db.set_Qr(_privateNeverWritten, argvalue1);
        var tmp1 = _sharedWrittenByAnotherTypesConstructor;
        string argvalue2 = _db.get_Qr(tmp1);
        SetValue(ref argvalue2);
        _db.set_Qr(tmp1, argvalue2);
    }

    public static void SetValue(ref string value)
    {
    }
}");
    }

    [Fact]
    public async Task ByRefPropertyTargetReusesIntrinsicOperatorsOnReusableOperandsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Option Strict Off

Public Structure Money
    Public Shared Operator +(a As Money, b As Money) As Integer
        Return 0
    End Operator
End Structure

Public Class C
    Public Property Qr(a As Integer, b As Integer) As String
        Get
            Return """"
        End Get
        Set(value As String)
        End Set
    End Property

    Sub UsesOperators(i As Integer, o As Object, m As Money)
        SetValue(Qr(i + 1, -i))
        SetValue(Qr(o + 1, 2))
        SetValue(Qr(m + m, 2))
    End Sub

    Shared Sub SetValue(ByRef value As String)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial struct Money
{
    public static int operator +(Money a, Money b)
    {
        return 0;
    }
}

public partial class C
{
    public string get_Qr(int a, int b)
    {
        return """";
    }
    public void set_Qr(int a, int b, string value)
    {
    }

    public void UsesOperators(int i, object o, Money m)
    {
        string argvalue = get_Qr(i + 1, -i);
        SetValue(ref argvalue);
        set_Qr(i + 1, -i, argvalue);
        var tmp = Conversions.ToInteger(Operators.AddObject(o, 1));
        string argvalue1 = get_Qr(tmp, 2);
        SetValue(ref argvalue1);
        set_Qr(tmp, 2, argvalue1);
        var tmp1 = m + m;
        string argvalue2 = get_Qr(tmp1, 2);
        SetValue(ref argvalue2);
        set_Qr(tmp1, 2, argvalue2);
    }

    public static void SetValue(ref string value)
    {
    }
}", incompatibleWithAutomatedCommentTesting: true); // The comment on Option Strict Off is lost before the added using directive
    }
}