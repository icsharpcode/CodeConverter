using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact(Skip = "Not implemented!")]
        public void MultilineString()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim x = ""Hello,
World!""
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private void TestMethod()
    {
        var x = @""Hello,
World!"";
    }
}");
        }

        [Fact]
        public void DateKeyword()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private DefaultDate as Date = Nothing
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private System.DateTime DefaultDate = default(Date);
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
End Class", @"class TestClass
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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

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
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim length As Integer = If(str?.Length, -1)
        Console.WriteLine(length)
        Console.ReadKey()
        Dim redirectUri As String = context.OwinContext.Authentication?.AuthenticationResponseChallenge?.Properties?.RedirectUri
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private void TestMethod(string str)
    {
        int length = str?.Length ?? -1;
        Console.WriteLine(length);
        Console.ReadKey();
        string redirectUri = context.OwinContext.Authentication?.AuthenticationResponseChallenge?.Properties?.RedirectUri;
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
        public void ThisMemberAccessExpression()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private member As Integer

    Private Sub TestMethod()
        Me.member = 0
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class BaseTestClass
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
        Dim test = Function(ByVal a As Integer) a * 2
        test(3)
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private void TestMethod()
    {
        var test = int a => a * 2;
        test(3);
    }
}");
        }

        [Fact]
        public void LambdaBodyExpression()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim test = Function(a) a * 2
        Dim test2 = Function(a, b)
                        If b > 0 Then Return a / b
                        Return 0
                    End Function

        Dim test3 = Function(a, b) a Mod b
        test(3)
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    private void TestMethod()
    {
        var test = a => a * 2;
        var test2 = (a, b) =>
        {
            if (b > 0)
                return a / (double)b;
            return 0;
        };

        var test3 = (a, b) => a % b;
        test(3);
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

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
        System.Console.WriteLine(n);
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
        System.Console.WriteLine($""Numbers with a remainder of {g.Remainder} when divided by 5:"");

        foreach (var n in g.Numbers)
            System.Console.WriteLine(n);
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
                @"class Product
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
        System.Console.WriteLine(v.Category + "":"");

        foreach (var p in v.Products)
            System.Console.WriteLine(""   "" + p.ProductName);
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
        public void PartiallyQualifiedName()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Function TestMethod(dir As String) As String
         Return IO.Path.Combine(dir, ""file.txt"")
    End Function
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    public string TestMethod(string dir)
    {
        return System.IO.Path.Combine(dir, ""file.txt"");
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
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic;

class TestClass
{
    public string TestMethod()
    {
        return Constants.vbCrLf;
    }
}");
        }
    }
}
