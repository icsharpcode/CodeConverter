using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class ExpressionTests : ConverterTestBase
    {
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
End Class", @"class TestClass
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
        return (""{\""name\"": \"""" + pName + ""\"", \""value\"": \"""" + pValue + ""\""}"");
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
        return (""{\""delta\"": \"""" + pDelta + ""\""}"");
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
        var x = Math.Pow(7, 6) % 5 / 4 + 3 * 2;
        x += 1;
        x -= 2;
        x *= 3;
        x /= 4;
        x = Math.Pow(x, 5);
    }
}");
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
        var x = 10 / (double)3;
        x /= 2;
        var y = 10.0 / 3;
        y /= 2;
        int z = 8;
        z /= (double)3;
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
        bool result = (str == """") ? true : false;
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

class TestClass
{
    private void TestMethod(string str)
    {
        int length;
        length = str.Length;
        Console.WriteLine(""Test"" + length);
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
        public Rec Prop { get; } = new Rec();
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
                return a / (double)b;
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
        Action ifAction = () =>"/* I don't know why this Action doesn't get qualified when the above two do - just characterizing current behaviour*/ + @"
        {
            if (true)
                return;
        };
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
        public void Linq1()
        {
            TestConversionVisualBasicToCSharp(@"Private Shared Sub SimpleQuery()
    Dim numbers As Integer() = {7, 9, 5, 3, 6}
    Dim res = From n In numbers Where n > 5 Select n

    For Each n In res
        Console.WriteLine(n)
    Next
End Sub",
                @"private static void SimpleQuery()
{
    int[] numbers = new[] { 7, 9, 5, 3, 6 };"/*TODO Remove need for new[]*/ + @"
    var res = from n in numbers
              where n > 5
              select n;

    foreach (var n in res)
        Console.WriteLine(n);
}");
        }

        [Fact]
        public void Linq2()
        {
            TestConversionVisualBasicToCSharp(@"Public Shared Sub Linq40()
    Dim numbers As Integer() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
    Dim numberGroups = From n In numbers Group n By __groupByKey1__ = n Mod 5 Into g Select New With {Key .Remainder = g.Key, Key .Numbers = g}

    For Each g In numberGroups
        Console.WriteLine($""Numbers with a remainder of {g.Remainder} when divided by 5:"")

        For Each n In g.Numbers
            Console.WriteLine(n)
        Next
    Next
End Sub",
                @"public static void Linq40()
{
    int[] numbers = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };"/*TODO Remove need for new[]*/ + @"
    var numberGroups = from n in numbers
                       group n by n % 5 into g
                       select new { Remainder = g.Key, Numbers = g };

    foreach (var g in numberGroups)
    {
        Console.WriteLine($""Numbers with a remainder of {g.Remainder} when divided by 5:"");

        foreach (var n in g.Numbers)
            Console.WriteLine(n);
    }
}");
        }

        [Fact()]
        public void Linq3()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class Product
    Public Category As String
    Public ProductName As String
End Class

Class Test
    Public Sub Linq102()
        Dim categories As String() = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
        Dim products As Product() = GetProductList()
        Dim q = From c In categories Join p In products On c Equals p.Category Select New With {Key .Category = c, p.ProductName}

        For Each v In q
            Console.WriteLine($""{v.ProductName}: {v.Category}"")
        Next
    End Sub
End Class",
                @"using System;
using System.Linq;

class Product
{
    public string Category;
    public string ProductName;
}

class Test
{
    public void Linq102()
    {
        string[] categories = new string[] { ""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood"" };
        Product[] products = GetProductList();
        var q = from c in categories
                join p in products on c equals p.Category
                select new { Category = c, p.ProductName };

        foreach (var v in q)
            Console.WriteLine($""{v.ProductName}: {v.Category}"");
    }
}");
        }

        [Fact]
        public void Linq4()
        {
            TestConversionVisualBasicToCSharp(@"Public Sub Linq103()
    Dim categories As String() = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
    Dim products = GetProductList()
    Dim q = From c In categories Group Join p In products On c Equals p.Category Into ps = Group Select New With {Key .Category = c, Key .Products = ps}

    For Each v In q
        Console.WriteLine(v.Category & "":"")

        For Each p In v.Products
            Console.WriteLine(""   "" & p.ProductName)
        Next
    Next
End Sub", @"public void Linq103()
{
    string[] categories = new string[] { ""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood"" };
    var products = GetProductList();
    var q = from c in categories
            join p in products on c equals p.Category into ps
            select new { Category = c, Products = ps };

    foreach (var v in q)
    {
        Console.WriteLine(v.Category + "":"");

        foreach (var p in v.Products)
            Console.WriteLine(""   "" + p.ProductName);
    }
}");
        }

        [Fact]
        public void Linq5()
        {
            TestConversionVisualBasicToCSharp(@"Private Shared Function FindPicFilePath(picId As String) As String
    For Each FileInfo As FileInfo In From FileInfo1 In AList Where FileInfo1.Name.Substring(0, 6) = picId
        Return FileInfo.FullName
    Next
    Return String.Empty
End Function", @"private static string FindPicFilePath(string picId)
{
    foreach (FileInfo FileInfo in from FileInfo1 in AList
                                  where FileInfo1.Name.Substring(0, 6) == picId
                                  select FileInfo1)
        return FileInfo.FullName;
    return string.Empty;
}");
        }

        [Fact]
        public void LinqMultipleFroms()
        {
            TestConversionVisualBasicToCSharp(@"Private Shared Sub LinqSub()
    Dim _result = From _claimProgramSummary In New List(Of List(Of List(Of List(Of String))))()
                  From _claimComponentSummary In _claimProgramSummary.First()
                  From _lineItemCalculation In _claimComponentSummary.Last()
                  Select _lineItemCalculation
End Sub", @"private static void LinqSub()
{
    var _result = from _claimProgramSummary in new List<List<List<List<string>>>>()
                  from _claimComponentSummary in _claimProgramSummary.First()
                  from _lineItemCalculation in _claimComponentSummary.Last()
                  select _lineItemCalculation;
}");
        }

        [Fact]
        public void PartiallyQualifiedName()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Function TestMethod(dir As String) As String
         Return IO.Path.Combine(dir, ""file.txt"")
    End Function
End Class", @"class TestClass
{
    public string TestMethod(string dir)
    {
        return System.IO.Path.Combine(dir, ""file.txt"");
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
        public void LogicalOrWithConditionalOperator()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
                @"Public Function GetString(yourBoolean as Boolean) As Boolean
    Return 1 <> 1 OrElse if (yourBoolean, True, False)
End Function",
                @"public bool GetString(bool yourBoolean)
{
    return 1 != 1 || yourBoolean ? true : false;
}");
        }
    }
}
