using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public void MyClassExpr()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass
    Sub TestMethod()
        MyClass.Val = 6
    End Sub

    Shared Val As Integer
End Class", @"public class TestClass
{
    public void TestMethod()
    {
        TestClass.Val = 6;
    }

    private static int Val;
}");

        }

        [Fact]
        public void MultilineString()
        {
            // Don't auto-test comments, otherwise it tries to put a comment in the middle of the string, which obviously isn't a valid place for it
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim x = ""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!""
        Dim y = $""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!""
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var x = @""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!"";
        var y = $@""Hello\ All strings in VB are verbatim """" < that's just a single escaped quote
World!"";
    }
}");
        }
        [Fact]
        public void Quotes()
        {
            // Don't auto-test comments, otherwise it tries to put a comment in the middle of the string, which obviously isn't a valid place for it
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Shared Function GetTextFeedInput(pStream As String, pTitle As String, pText As String) As String
        Return ""{"" & AccessKey() & "",""""streamName"""": """""" & pStream & """""",""""point"""": ["" & GetTitleTextPair(pTitle, pText) & ""]}""
    End Function

    Shared Function AccessKey() As String
        Return """"""accessKey"""": """"8iaiHNZpNbBkYHHGbMNiHhAp4uPPyQke""""""
    End Function

    Shared Function GetNameValuePair(pName As String, pValue As Integer) As String
        Return (""{""""name"""": """""" & pName & """""", """"value"""": """""" & pValue & """"""}"")
    End Function

    Shared Function GetNameValuePair(pName As String, pValue As String) As String
        Return (""{""""name"""": """""" & pName & """""", """"value"""": """""" & pValue & """"""}"")
    End Function

    Shared Function GetTitleTextPair(pName As String, pValue As String) As String
        Return (""{""""title"""": """""" & pName & """""", """"msg"""": """""" & pValue & """"""}"")
    End Function
    Shared Function GetDeltaPoint(pDelta As Integer) As String
        Return (""{""""delta"""": """""" & pDelta & """"""}"")
    End Function
End Class", @"using Microsoft.VisualBasic.CompilerServices;

class TestClass
{
    public static string GetTextFeedInput(string pStream, string pTitle, string pText)
    {
        return ""{"" + AccessKey() + "",\""streamName\"": \"""" + pStream + ""\"",\""point\"": ["" + GetTitleTextPair(pTitle, pText) + ""]}"";
    }

    public static string AccessKey()
    {
        return ""\""accessKey\"": \""8iaiHNZpNbBkYHHGbMNiHhAp4uPPyQke\"""";
    }

    public static string GetNameValuePair(string pName, int pValue)
    {
        return (""{\""name\"": \"""" + pName + ""\"", \""value\"": \"""" + Conversions.ToString(pValue) + ""\""}"");
    }

    public static string GetNameValuePair(string pName, string pValue)
    {
        return (""{\""name\"": \"""" + pName + ""\"", \""value\"": \"""" + pValue + ""\""}"");
    }

    public static string GetTitleTextPair(string pName, string pValue)
    {
        return (""{\""title\"": \"""" + pName + ""\"", \""msg\"": \"""" + pValue + ""\""}"");
    }
    public static string GetDeltaPoint(int pDelta)
    {
        return (""{\""delta\"": \"""" + Conversions.ToString(pDelta) + ""\""}"");
    }
}");
        }

        [Fact]
        public void ConversionOfNotUsesParensIfNeeded()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim rslt = Not 1 = 2
        Dim rslt2 = Not True
        Dim rslt3 = TypeOf True IsNot Boolean
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var rslt = !(1 == 2);
        var rslt2 = !true;
        var rslt3 = !(true is bool);
    }
}");
        }

        [Fact]
        public void DateLiterals()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal date As Date = #1/1/1900#)
        Dim rslt = #1/1/1900#
        Dim rslt2 = #8/13/2002 12:14 PM#
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class TestClass
{
    private void TestMethod([Optional, DateTimeConstant(599266080000000000L)] DateTime date)
    {
        var rslt = DateTime.Parse(""1900-01-01"");
        var rslt2 = DateTime.Parse(""2002-08-13 12:14:00"");
    }
}");
        }

        [Fact]
        public void RefArgumentRValue()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
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
End Class", @"public class Class1
{
    private Class1 C1 { get; set; }
    private Class1 _c2;
    private object _o1;

    public void Foo()
    {
        var argclass1 = (object)new Class1();
        Bar(ref argclass1);
        var argclass11 = (object)C1;
        Bar(ref argclass11);
        var argclass12 = (object)this.C1;
        Bar(ref argclass12);
        var argclass13 = (object)_c2;
        Bar(ref argclass13);
        var argclass14 = (object)this._c2;
        Bar(ref argclass14);
        Bar(ref _o1);
        Bar(ref this._o1);
    }

    public void Bar(ref object class1)
    {
    }
}");
        }

        [Fact]
        public void RefArgumentRValue2()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo()
        Dim x = True
        Bar(x = True)
    End Sub

    Sub Foo2()
        Return Bar(True = False)
    End Sub

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

    Sub Bar(ByRef b As Boolean)
    End Sub

    Function Bar2(ByRef c1 As Class1) As Integer
        If c1 IsNot Nothing AndAlso Len(Bar3(Me)) <> 0 Then
            Return 1
        End If
        Return 0
    End Function

    Function Bar3(ByRef c1 As Class1) As String
        Return """"
    End Function

End Class", @"public class Class1
{
    public void Foo()
    {
        var x = true;
        var argb = x == true;
        Bar(ref argb);
    }

    public void Foo2()
    {
        var argb = true == false;
        return Bar(ref argb);
    }

    public void Foo3()
    {
        var argb1 = true == false;
        if (Bar(ref argb1))
        {
            var argb = true == false;
            Bar(ref argb);
        }
    }

    public void Foo4()
    {
        var argb3 = true == false;
        var argb4 = true == false;
        if (Bar(ref argb3))
        {
            var argb = true == false;
            Bar(ref argb);
        }
        else if (Bar(ref argb4))
        {
            var argb2 = true == false;
            Bar(ref argb2);
        }
        else
        {
            var argb1 = true == false;
            Bar(ref argb1);
        }
    }

    public void Foo5()
    {
        var argb = default(bool);
        Bar(ref argb);
    }

    public void Bar(ref bool b)
    {
    }

    public int Bar2(ref Class1 c1)
    {
        var argc1 = this;
        if (c1 != null && Microsoft.VisualBasic.Strings.Len(Bar3(ref argc1)) != 0)
            return 1;
        return 0;
    }

    public string Bar3(ref Class1 c1)
    {
        return """";
    }
}");
        }

        [Fact]
        public void RefArgumentUsing()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Imports System.Data.SqlClient

Public Class Class1
    Sub Foo()
        Using x = New SqlConnection
            Bar(x)
        End Using
    End Sub
    Sub Bar(ByRef x As SqlConnection)

    End Sub
End Class", @"using System.Data.SqlClient;

public class Class1
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
        public void RefArgumentPropertyInitializer()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Private _p1 As Class1 = Foo(New Class1)
    Public Shared Function Foo(ByRef c1 As Class1) As Class1
        Return c1
    End Function
End Class", @"public class Class1
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
        public void MethodCallWithImplicitConversion()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo()
        Bar(True)
        Me.Bar(""4"")
        Dim ss(1) As String
        Dim y = ss(""0"")
    End Sub

    Sub Bar(x as Integer)
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

public class Class1
{
    public void Foo()
    {
        Bar(Conversions.ToInteger(true));
        this.Bar(Conversions.ToInteger(""4""));
        string[] ss = new string[2];
        var y = ss[Conversions.ToInteger(""0"")];
    }

    public void Bar(int x)
    {
    }
}");
        }

        [Fact]
        public void IntToEnumArg()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo(ByVal arg As TriState)
    End Sub

    Sub Main()
        Foo(0)
    End Sub
End Class",
@"using Microsoft.VisualBasic;

public class Class1
{
    public void Foo(TriState arg)
    {
    }

    public void Main()
    {
        Foo((TriState)0);
    }
}");
        }



        [Fact]
        public void EnumSwitch()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
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
@"public class Class1
{
    enum E
    {
        A
    }

    public void Main()
    {
        var e1 = E.A;
        int e2 = default(int);
        switch (e1)
        {
            case 0:
                {
                    break;
                }
        }

        switch (e2)
        {
            case var @case when @case == (int)E.A:
                {
                    break;
                }
        }
    }
}");
        }


        [Fact]
        public void MethodCallWithoutParens()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
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
End Class", @"public class Class1
{
    public void Foo()
    {
        var w = Bar();
        var x = this.Bar();
        var y = Baz;
        var z = this.Baz;
    }

    public int Bar()
    {
        return 1;
    }
    public int Baz { get; set; }
}");
        }

        [Fact]
        public void ConversionOfCTypeUsesParensIfNeeded()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim rslt = Ctype(true, Object).ToString()
        Dim rslt2 = Ctype(true, Object)
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var rslt = ((object)true).ToString();
        var rslt2 = (object)true;
    }
}");
        }

        [Fact]
        public void DateKeyword()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private DefaultDate as Date = Nothing
End Class", @"using System;

class TestClass
{
    private DateTime DefaultDate = default(DateTime);
}");
        }

        [Fact]
        public void AccessSharedThroughInstance()
        {
            TestConversionVisualBasicToCSharp(@"Public Class A
    Public Shared x As Integer = 2
    Public Sub Test()
        Dim tmp = Me
        Dim y = Me.x
        Dim z = tmp.x
    End Sub
End Class", @"public class A
{
    public static int x = 2;
    public void Test()
    {
        var tmp = this;
        var y = A.x;
        var z = A.x;
    }
}");
        }

        [Fact]
        public void UnknownTypeInvocation()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private property DefaultDate as System.SomeUnknownType
    private sub TestMethod()
        Dim a = DefaultDate(1, 2, 3).Blawer(1, 2, 3)
    End Sub
End Class", @"class TestClass
{
    private System.SomeUnknownType DefaultDate { get; set; }
    private void TestMethod()
    {
        var a = DefaultDate[1, 2, 3].Blawer(1, 2, 3);
    }
}");
        }

        [Fact]
        public void BinaryOperatorsIsIsNotLeftShiftRightShift()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private bIs as Boolean = New Object Is New Object
    Private bIsNot as Boolean = New Object IsNot New Object
    Private bLeftShift as Integer = 1 << 3
    Private bRightShift as Integer = 8 >> 3
End Class", @"class TestClass
{
    private bool bIs = new object() == new object();
    private bool bIsNot = new object() != new object();
    private int bLeftShift = 1 << 3;
    private int bRightShift = 8 >> 3;
}");
        }

        [Fact]
        public void EnumNullableConversion()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Main()
        Dim x = DayOfWeek.Monday
        Foo(x)
    End Sub

    Sub Foo(x As DayOfWeek?)

    End Sub
End Class", @"using System;

public class Class1
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
        public void UninitializedVariable()
        {
            //TODO: Fix comment to be ported to top of property rather than bottom
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
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
End Class", @"public class Class1
{
    public Class1()
    {
        int needsInitialization = default(int);
        int notUsed;
        var y = needsInitialization;
    }

    public void Foo()
    {
        int needsInitialization = default(int);
        int notUsed;
        var y = needsInitialization;
    }

    public void Bar()
    {
        int i = default(int);
        string temp = string.Empty;
        i += 1;
    }

    public void Bar2()
    {
        int i = default(int);
        string temp = string.Empty;
        i = i + 1;
    }

    public void Bar3()
    {
        int i = default(int);
        string temp = string.Empty;
        int k = i + 1;
    }

    public void Bar4()
    {
        int i = default(int);
        string temp = string.Empty;
        int k = i + 1;
        i = 1;
    }

    public int State
    {
        get
        {
            int needsInitialization = default(int);
            int notUsed;
            var y = needsInitialization;
            return y;
        }
    }
}");
        }

        [Fact]
        public void ShiftAssignment()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        x <<= 4
        x >>= 3
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var x = 1;
        x <<= 4;
        x >>= 3;
    }
}");
        }

        [Fact]
        public void StringCompare()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo()
        Dim s1 As String = Nothing
        Dim s2 As String = """"
        If s1 <> s2 Then
            Throw New Exception()
        End If
        If s1 = ""something"" Then
            Throw New Exception()
        End If
        If ""something"" = s1 Then
            Throw New Exception()
        End If
        If s1 = Nothing Then
            '
        End If
        If s1 = """" Then
            '
        End If
    End Sub
End Class", @"using System;

public class Class1
{
    public void Foo()
    {
        string s1 = null;
        string s2 = """";
        if ((s1 ?? """") != (s2 ?? """"))
            throw new Exception();
        if ((s1 ?? """") == ""something"")
            throw new Exception();
        if (""something"" == (s1 ?? """"))
            throw new Exception();
        if (s1 == null)
        {
        }
        if (string.IsNullOrEmpty(s1))
        {
        }
    }
}");
        }

        [Fact]
        public void StringCompareText()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Option Compare Text
Public Class Class1
    Sub Foo()
        Dim s1 As String = Nothing
        Dim s2 As String = """"
        If s1 <> s2 Then
            Throw New Exception()
        End If
        If s1 = ""something"" Then
            Throw New Exception()
        End If
        If ""something"" = s1 Then
            Throw New Exception()
        End If
        If s1 = Nothing Then
            '
        End If
        If s1 = """" Then
            '
        End If
    End Sub
End Class", @"using System;
using System.Globalization;

public class Class1
{
    public void Foo()
    {
        string s1 = null;
        string s2 = """";
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(s1, s2, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) != 0)
            throw new Exception();
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(s1, ""something"", CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
            throw new Exception();
        if (CultureInfo.CurrentCulture.CompareInfo.Compare(""something"", s1, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth) == 0)
            throw new Exception();
        if (s1 == null)
        {
        }
        if (string.IsNullOrEmpty(s1))
        {
        }
    }
}");
        }

        [Fact]
        public void StringConcatPrecedence()
        {
            TestConversionVisualBasicToCSharp(@"Public Class Class1
    Sub Foo()
        Dim x = ""x "" & 5 - 4 & "" y""
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

public class Class1
{
    public void Foo()
    {
        var x = ""x "" + Conversions.ToString(5 - 4) + "" y"";
    }
}");
        }

        [Fact]
        public void IntegerArithmetic()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 7 ^ 6 Mod 5 \ 4 + 3 * 2
        x += 1
        x -= 2
        x *= 3
        x \= 4
        x ^= 5
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod()
    {
        var x = (Math.Pow((double)7, (double)6) % (double)(5 / 4)) + (double)(3 * 2);
        x += 1;
        x -= 2;
        x *= 3;
        x /= 4;
        x = Math.Pow(x, 5);
    }
}");
        }

        [Fact]
        public void ImplicitConversions()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim x As Double = 1
        Dim y As Decimal = 2
        Dim i1 As Integer = 1
        Dim i2 As Integer = 2
        Dim d1 = i1 / i2
        Dim z = x + y
        Dim z2 = y + x
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices;

class TestClass
{
    private void TestMethod()
    {
        double x = (double)1;
        decimal y = (decimal)2;
        int i1 = 1;
        int i2 = 2;
        var d1 = (double)i1 / (double)i2;
        var z = x + Conversions.ToDouble(y);
        var z2 = Conversions.ToDouble(y) + x;
    }
}
");
        }

        [Fact]
        public void FloatingPointDivisionIsForced()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 10 / 3
        x /= 2
        Dim y = 10.0 / 3
        y /= 2
        Dim z As Integer = 8
        z /= 3
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var x = (double)10 / (double)3;
        x /= 2;
        var y = 10.0 / (double)3;
        y /= 2;
        int z = 8;
        z /= 3;
    }
}");
        }

        [Fact]
        public void FullyTypeInferredEnumerableCreation()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim strings = { ""1"", ""2"" }
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var strings = new[] { ""1"", ""2"" };
    }
}");
        }

        [Fact]
        public void EmptyArgumentLists()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim str = (New ThreadStaticAttribute).ToString
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod()
    {
        var str = (new ThreadStaticAttribute()).ToString();
    }
}");
        }

        [Fact]
        public void StringConcatenationAssignment()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim str = ""Hello, ""
        str &= ""World""
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var str = ""Hello, "";
        str += ""World"";
    }
}");
        }

        [Fact]
        public void GetTypeExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim typ = GetType(String)
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var typ = typeof(string);
    }
}");
        }

        [Fact]
        public void NullableInteger()
        {
            //BUG: Line comments after "else" aren't converted
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Public Function Bar(value As String) As Integer?
        Dim result As Integer
        If Integer.TryParse(value, result) Then
            Return result
        Else
            Return Nothing
        End If
    End Function
End Class", @"class TestClass
{
    public int? Bar(string value)
    {
        int result;
        if (int.TryParse(value, out result))
            return result;
        else
            return default(int?);
    }
}");
        }

        [Fact]
        public void NothingInvokesDefaultForValueTypes()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Sub Bar()
        Dim number As Integer
        number = Nothing
        Dim dat As Date
        dat = Nothing
    End Sub
End Class", @"using System;

class TestClass
{
    public void Bar()
    {
        int number;
        number = default(int);
        DateTime dat;
        dat = default(DateTime);
    }
}");
        }

        [Fact]
        public void UsesSquareBracketsForIndexerButParenthesesForMethodInvocation()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Function TestMethod() As String()
        Dim s = ""1,2""
        Return s.Split(s(1))
    End Function
End Class", @"class TestClass
{
    private string[] TestMethod()
    {
        var s = ""1,2"";
        return s.Split(s[1]);
    }
}");
        }

        [Fact]
        public void UsesSquareBracketsForItemIndexer()
        {
            TestConversionVisualBasicToCSharp(@"Imports System.Data

Class TestClass
    Function GetItem(dr As DataRow) As Object
        Return dr.Item(""col1"")
    End Function
End Class", @"using System.Data;

class TestClass
{
    public object GetItem(DataRow dr)
    {
        return dr[""col1""];
    }
}");
        }

        [Fact]
        public void ConditionalExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = If((str = """"), True, False)
    End Sub
End Class", @"class TestClass
{
    private void TestMethod(string str)
    {
        bool result = (string.IsNullOrEmpty(str)) ? true : false;
    }
}");
        }

        [Fact]
        public void ConditionalExpressionInUnaryExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = Not If((str = """"), True, False)
    End Sub
End Class", @"class TestClass
{
    private void TestMethod(string str)
    {
        bool result = !((string.IsNullOrEmpty(str)) ? true : false);
    }
}");
        }

        [Fact]
        public void ConditionalExpressionInBinaryExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Integer = 5 - If((str = """"), 1, 2)
    End Sub
End Class", @"class TestClass
{
    private void TestMethod(string str)
    {
        int result = 5 - ((string.IsNullOrEmpty(str)) ? 1 : 2);
    }
}");
        }

        [Fact]
        public void NullCoalescingExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Console.WriteLine(If(str, ""<null>""))
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
    }
}");
        }
        [Fact]
        public void OmmittedArgumentInInvocation()
        {
            TestConversionVisualBasicToCSharp(@"Imports System

Public Module MyExtensions
    public sub NewColumn(type As Type , Optional strV1 As String = nothing, optional code As String = ""code"")
    End sub

    public Sub CallNewColumn()
        NewColumn(GetType(MyExtensions))
        NewColumn(Nothing, , ""otherCode"")
        NewColumn(Nothing, ""fred"")
    End Sub
End Module", @"using System;

public static class MyExtensions
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
        public void MemberAccessAndInvocationExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim length As Integer
        length = str.Length
        Console.WriteLine(""Test"" & length)
        Console.ReadKey()
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices;

class TestClass
{
    private void TestMethod(string str)
    {
        int length;
        length = str.Length;
        Console.WriteLine(""Test"" + Conversions.ToString(length));
        Console.ReadKey();
    }
}");
        }

        [Fact]
        public void ExternalReferenceToOutParameter()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim d = New Dictionary(Of string, string)
        Dim s As String
        d.TryGetValue(""a"", s)
    End Sub
End Class", @"using System.Collections.Generic;

class TestClass
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
        public void OmittedParamsArray()
        {
            TestConversionVisualBasicToCSharp(@"Module AppBuilderUseExtensions
    <System.Runtime.CompilerServices.Extension>
    Function Use(Of T)(ByVal app As String, ParamArray args As Object()) As Object
        Return Nothing
    End Function
End Module

Class TestClass
    Private Sub TestMethod(ByVal str As String)
        str.Use(Of object)
    End Sub
End Class", @"static class AppBuilderUseExtensions
{
    public static object Use<T>(this string app, params object[] args)
    {
        return null;
    }
}

class TestClass
{
    private void TestMethod(string str)
    {
        str.Use<object>();
    }
}");
        }

        [Fact]
        public void ElvisOperatorExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass3
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

class TestClass3
{
    private class Rec
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

        [Fact()]
        public void ObjectInitializerExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class StudentName
    Public LastName, FirstName As String
End Class

Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 As StudentName = New StudentName With {.FirstName = ""Craig"", .LastName = ""Playstead""}
    End Sub
End Class", @"class StudentName
{
    public string LastName, FirstName;
}

class TestClass
{
    private void TestMethod(string str)
    {
        StudentName student2 = new StudentName() { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}");
        }

        [Fact()]
        public void ObjectInitializerExpression2()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 = New With {Key .FirstName = ""Craig"", Key .LastName = ""Playstead""}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod(string str)
    {
        var student2 = new { FirstName = ""Craig"", LastName = ""Playstead"" };
    }
}");
        }
        [Fact]
        public void CollectionInitializers()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub DoStuff(a As Object)
    End Sub
    Private Sub TestMethod()
        DoStuff({1, 2})
        Dim intList As New List(Of Integer) From {1}
        Dim dict As New Dictionary(Of Integer, Integer) From {{1, 2}, {3, 4}}
    End Sub
End Class", @"using System.Collections.Generic;

class TestClass
{
    private void DoStuff(object a)
    {
    }
    private void TestMethod()
    {
        DoStuff(new[] { 1, 2 });
        List<int> intList = new List<int>() { 1 };
        Dictionary<int, int> dict = new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } };
    }
}");
        }

        [Fact]
        public void ThisMemberAccessExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private member As Integer

    Private Sub TestMethod()
        Me.member = 0
    End Sub
End Class", @"class TestClass
{
    private int member;

    private void TestMethod()
    {
        this.member = 0;
    }
}");
        }

        [Fact]
        public void BaseMemberAccessExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class BaseTestClass
    Public member As Integer
End Class

Class TestClass
    Inherits BaseTestClass

    Private Sub TestMethod()
        MyBase.member = 0
    End Sub
End Class", @"class BaseTestClass
{
    public int member;
}

class TestClass : BaseTestClass
{
    private void TestMethod()
    {
        base.member = 0;
    }
}");
        }

        [Fact]
        public void DelegateExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim test As Func(Of Integer, Integer) = Function(ByVal a As Integer) a * 2
        test(3)
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = (int a) => a * 2;
        test(3);
    }
}");
        }

        [Fact]
        public void LambdaBodyExpression()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
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

class TestClass
{
    private void TestMethod()
    {
        Func<int, int> test = a => a * 2;
        Func<int, int, double> test2 = (a, b) =>
        {
            if (b > 0)
                return (double)a / (double)b;
            return 0;
        };

        Func<int, int, int> test3 = (a, b) => a % b;
        test(3);
    }
}");
        }

        [Fact]
        public void SingleLineLambdaWithStatementBody()
        {
            //Bug: Comments after action definition are lost
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        Dim simpleAssignmentAction As System.Action = Sub() x = 1
        Dim nonBlockAction As System.Action = Sub() Console.WriteLine(""Statement"")
        Dim ifAction As Action = Sub() If True Then Exit Sub
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod()
    {
        var x = 1;
        System.Action simpleAssignmentAction = () => x = 1;
        System.Action nonBlockAction = () => Console.WriteLine(""Statement"");
        Action ifAction = () => { if (true) return; };"/* I don't know why this Action doesn't get qualified when the above two do - just characterizing current behaviour*/ + @"
    }
}");
        }
        
        [Fact]
        public void Await()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Function SomeAsyncMethod() As Task(Of Integer)
        Return Task.FromResult(0)
    End Function

    Private Async Sub TestMethod()
        Dim result As Integer = Await SomeAsyncMethod()
        Console.WriteLine(result)
    End Sub
End Class", @"using System;
using System.Threading.Tasks;

class TestClass
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
        public void PartiallyQualifiedName()
        {
            TestConversionVisualBasicToCSharp(@"Imports System.Collections
Class TestClass
    Public Sub TestMethod(dir As String)
        IO.Path.Combine(dir, ""file.txt"")
        Dim c As New ObjectModel.ObservableCollection(Of String)
    End Sub
End Class", @"class TestClass
{
    public void TestMethod(string dir)
    {
        System.IO.Path.Combine(dir, ""file.txt"");
        System.Collections.ObjectModel.ObservableCollection<string> c = new System.Collections.ObjectModel.ObservableCollection<string>();
    }
}");
        }

        [Fact]
        public void TypePromotedModuleIsQualified()
        {
            TestConversionVisualBasicToCSharp(@"Namespace TestNamespace
    Public Module TestModule
        Public Sub ModuleFunction()
        End Sub
    End Module
End Namespace

Class TestClass
    Public Sub TestMethod(dir As String)
        TestNamespace.ModuleFunction()
    End Sub
End Class", @"namespace TestNamespace
{
    public static class TestModule
    {
        public static void ModuleFunction()
        {
        }
    }
}

class TestClass
{
    public void TestMethod(string dir)
    {
        TestNamespace.TestModule.ModuleFunction();
    }
}");
        }
        
        [Fact]
        public void NameQualifyingHandlesInheritance()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClassBase
    Sub DoStuff()
    End Sub
End Class
Class TestClass
    Inherits TestClassBase
    Private Sub TestMethod()
        DoStuff()
    End Sub
End Class", @"class TestClassBase
{
    public void DoStuff()
    {
    }
}

class TestClass : TestClassBase
{
    private void TestMethod()
    {
        DoStuff();
    }
}");
        }

        [Fact]
        public void UsingGlobalImport()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Function TestMethod() As String
         Return vbCrLf
    End Function
End Class", @"using Microsoft.VisualBasic;

class TestClass
{
    public string TestMethod()
    {
        return Constants.vbCrLf;
    }
}");
        }

        [Fact]
        public void ValueCapitalisation()
        {
            //TODO: Fix comment to be ported to top of property rather than bottom
            TestConversionVisualBasicToCSharpWithoutComments(@"public Enum TestState
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
end class", @"public enum TestState
{
    one,
    two
}

public class test
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
                _state = value;
        }
    }
}");
        }

        [Fact]
        public void StringInterpolationWithConditionalOperator()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
                @"Public Function GetString(yourBoolean as Boolean) As String
    Return $""You {if (yourBoolean, ""do"", ""do not"")} have a true value""
End Function",
                @"public string GetString(bool yourBoolean)
{
    return $""You {(yourBoolean ? ""do"" : ""do not"")} have a true value"";
}");
        }

        [Fact]
        public void Tuple()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
                @"Public Function GetString(yourBoolean as Boolean) As Boolean
    Return 1 <> 1 OrElse if (yourBoolean, True, False)
End Function",
                @"public bool GetString(bool yourBoolean)
{
    return 1 != 1 || (yourBoolean ? true : false);
}");
        }

        [Fact]
        public void UseEventBackingField()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
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

public class Foo
{
    public event EventHandler<EventArgs> Bar;

    protected void OnBar(EventArgs e)
    {
        if (Bar == null)
            System.Diagnostics.Debug.WriteLine(""No subscriber"");
        else
            Bar?.Invoke(this, e);
    }
}");
        }

        [Fact]
        public void StringInterpolationWithDoubleQuotes()
        {
            TestConversionVisualBasicToCSharp(
@"Imports System

Namespace Global.InnerNamespace
    Public Class Test
        Public Function StringInter(t As String, dt As DateTime) As String
            Dim a = $""pre{t} t""
            Dim b = $""pre{t} """" t""
            Dim c = $""pre{t} """"\ t""
            Dim d = $""pre{t & """"""""} """" t""
            Dim e = $""pre{t & """"""""} """"\ t""
            Dim f = $""pre{{escapedBraces}}{dt,4:hh}""
            Return a & b & c & d & e & f
        End Function
    End Class
End Namespace",
@"using System;

namespace InnerNamespace
{
    public class Test
    {
        public string StringInter(string t, DateTime dt)
        {
            var a = $""pre{t} t"";
            var b = $""pre{t} \"" t"";
            var c = $@""pre{t} """"\ t"";
            var d = $""pre{t + ""\""""} \"" t"";
            var e = $@""pre{t + ""\""""} """"\ t"";
            var f = $""pre{{escapedBraces}}{dt,4:hh}"";
            return a + b + c + d + e + f;
        }
    }
}");
        }

        [Fact]
        public void DateTimeToDateAndTime()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Foo()
        Dim x = DateAdd(""m"", 5, Now)
    End Sub
End Class", @"using Microsoft.VisualBasic;

public class Class1
{
    public void Foo()
    {
        var x = DateAndTime.DateAdd(""m"", (double)5, DateAndTime.Now);
    }
}");
        }

        [Fact]
        public void BaseFinalizeRemoved()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub
End Class", @"public class Class1
{
    ~Class1()
    {
    }
}");
        }

        [Fact]
        public void MemberAccessCasing()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class Class1
    Sub Bar()

    End Sub

    Sub Foo()
        bar()
        me.bar()
    End Sub
End Class", @"public class Class1
{
    public void Bar()
    {
    }

    public void Foo()
    {
        Bar();
        this.Bar();
    }
}");
        }

    }
}
