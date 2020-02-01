using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task MultilineString()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        var x = @""Hello,
World!"";
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim x = ""Hello,
World!""
    End Sub
End Class");
        }

        [Fact]
        public async Task StringInterpolationWithDoubleQuotes()
        {
            await TestConversionCSharpToVisualBasic(
                @"using System; //Not required in VB due to global imports

namespace global::InnerNamespace
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
}",
                @"Namespace Global.InnerNamespace
    Public Class Test
        Public Function StringInter(ByVal t As String, ByVal dt As Date) As String
            Dim a = $""pre{t} t""
            Dim b = $""pre{t} """" t""
            Dim c = $""pre{t} """"\ t""
            Dim d = $""pre{t & """"""""} """" t""
            Dim e = $""pre{t & """"""""} """"\ t""
            Dim f = $""pre{{escapedBraces}}{dt,4:hh}""
            Return a & b & c & d & e & f
        End Function
    End Class
End Namespace");
        }

        [Fact]
        public async Task ConditionalExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        bool result = (str == """") ? true : false;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result = If(str Is """", True, False)
    End Sub
End Class");
        }

        [Fact]
        public async Task IsNullExpression()
        {
            await TestConversionCSharpToVisualBasic(@"public class Test {

    public bool Foo {
        get {
	    return (Bar is null); //Crashes conversion to VB
        }
    }

    public string Bar;

}", @"Public Class Test
    Public ReadOnly Property Foo As Boolean
        Get
            Return Bar Is Nothing ' Crashes conversion to VB
        End Get
    End Property

    Public Bar As String
End Class");
        }

        [Fact]
        public async Task IfIsPatternExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    private static int GetLength(object node)
    {
        if (node is string s)
        {
            return s.Length;
        }

        return -1;
    }
}", @"Friend Class TestClass
    Private Shared Function GetLength(ByVal node As Object) As Integer
        Dim s As String = Nothing

        If CSharpImpl.__Assign(s, TryCast(node, String)) IsNot Nothing Then
            Return s.Length
        End If

        Return -1
    End Function

    Private Class CSharpImpl
        <Obsolete(""Please refactor calling code to use normal Visual Basic assignment"")>
        Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class
End Class");
        }

        [Fact]
        public async Task DeclarationExpression()
        {
            await TestConversionCSharpToVisualBasic(@"using System.Collections.Generic;

class TestClass
{
    private static bool Do()
    {
        var d = new Dictionary<string, string>();
        return d.TryGetValue("""", out var output);
    }
}", @"Imports System.Collections.Generic

Friend Class TestClass
    Private Shared Function [Do]() As Boolean
        Dim d = New Dictionary(Of String, String)()
        Dim output As string = Nothing
        Return d.TryGetValue("""", output)
    End Function
End Class");
        }

        [Fact]
        public async Task ThrowExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        bool result = (str == """") ? throw new Exception(""empty"") : false;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result = If(str Is """", CSharpImpl.__Throw(Of Boolean)(New Exception(""empty"")), False)
    End Sub

    Private Class CSharpImpl
        <Obsolete(""Please refactor calling code to use normal throw statements"")>
        Shared Function __Throw(Of T)(ByVal e As Exception) As T
            Throw e
        End Function
    End Class
End Class");
        }

        [Fact]
        public async Task NameOf()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    private string n = nameof(TestMethod);

    private void TestMethod()
    {
    }
}", @"Friend Class TestClass
    Private n = NameOf(TestMethod)

    Private Sub TestMethod()
    End Sub
End Class");
        }

        [Fact]
        public async Task NullCoalescingExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Console.WriteLine(If(str, ""<null>""))
    End Sub
End Class");
        }
        [Fact]
        public async Task CoalescingExpression_Assignment()
        {
            await TestConversionCSharpToVisualBasic(
@"class TestClass {
    string prop;
    string prop2;
    string Property {
        get {
            var z = (() => 3)();
            return this.prop ?? (this.prop2 = CreateProperty());
        }
    }
    string CreateProperty() {
        return """";
    }
}",
                @"Friend Class TestClass
    Private prop As String
    Private prop2 As String

    Private ReadOnly Property [Property] As String
        Get
            Dim z = (Function() 3)()
            Return If(Me.prop, Function()
                                   Me.prop2 = CreateProperty()
                                   Return Me.prop2
                               End Function())
        End Get
    End Property

    Private Function CreateProperty() As String
        Return """"
    End Function
End Class");
        }

        [Fact]
        public async Task MemberAccessAndInvocationExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        int length;
        length = str.Length;
        Console.WriteLine(""Test"" + length);
        Console.ReadKey();
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim length As Integer
        length = str.Length
        Console.WriteLine(""Test"" & length)
        Console.ReadKey()
    End Sub
End Class");
        }

        [Fact]
        public async Task CallInvoke()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        Dispatcher.Invoke(new Action(() => Console.WriteLine(1)));
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dispatcher.Invoke(New Action(Function() Console.WriteLine(1)))
    End Sub
End Class");
        }

        [Fact]
        public async Task ShiftOperators()
        {
            await TestConversionCSharpToVisualBasic(@"public class Test
{
    public static void Main()
    {
        int y = 1;
        y <<= 1;
        y >>= 1;
        y = y << 1;
        y = y >> 1;
	}
}", @"Public Class Test
    Public Shared Sub Main()
        Dim y = 1
        y <<= 1
        y >>= 1
        y = y << 1
        y = y >> 1
    End Sub
End Class");
        }
        [Fact]
        public async Task CompoundAssignmentTest() {
            await TestConversionCSharpToVisualBasic(
@"public class TestClass {
    void TestMethod() {
        int x = 10;
        x *= 3;
        x /= 3;
    }
}",
                @"Public Class TestClass
    Private Sub TestMethod()
        Dim x = 10
        x *= 3
        x /= 3
    End Sub
End Class");
        }

        [Fact]
        public async Task ElvisOperatorExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        int length = str?.Length ?? -1;
        Console.WriteLine(length);
        Console.ReadKey();
        string redirectUri = context.OwinContext.Authentication?.AuthenticationResponseChallenge?.Properties?.RedirectUri;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim length = If(str?.Length, -1)
        Console.WriteLine(length)
        Console.ReadKey()
        Dim redirectUri As String = context.OwinContext.Authentication?.AuthenticationResponseChallenge?.Properties?.RedirectUri
    End Sub
End Class");
        }

        [Fact]
        public async Task ObjectInitializerExpression()
        {
            await TestConversionCSharpToVisualBasic(@"
class StudentName
{
    public string LastName, FirstName;
}

class TestClass
{
    void TestMethod(string str)
    {
        StudentName student2 = new StudentName {
            FirstName = ""Craig"",
            LastName = ""Playstead"",
        };
    }
}", @"Friend Class StudentName
    Public LastName, FirstName As String
End Class

Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 As StudentName = New StudentName With {
            .FirstName = ""Craig"",
            .LastName = ""Playstead""
        }
    End Sub
End Class");
        }

        [Fact]
        public async Task ObjectInitializerExpression2()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(string str)
    {
        var student2 = new {
            FirstName = ""Craig"",
            LastName = ""Playstead"",
        };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim student2 = New With {
            .FirstName = ""Craig"",
            .LastName = ""Playstead""
        }
    End Sub
End Class");
        }

        [Fact]
        public async Task ObjectInitializerExpression3()
        {
            await TestConversionCSharpToVisualBasic(@"using System.Collections.Generic;

internal class SomeSettings
{
    public IList<object> Converters { get; set; }
}

internal class Converter
{
    public static readonly SomeSettings Settings = new SomeSettings
    {
        Converters = {},
    };
}", @"Imports System.Collections.Generic

Friend Class SomeSettings
    Public Property Converters As IList(Of Object)
End Class

Friend Class Converter
    Public Shared ReadOnly Settings As SomeSettings = New SomeSettings With {
        .Converters = {}
    }
End Class");
        }

        [Fact]
        public async Task ThisMemberAccessExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    private int member;

    void TestMethod()
    {
        this.member = 0;
    }
}", @"Friend Class TestClass
    Private member As Integer

    Private Sub TestMethod()
        member = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task BaseMemberAccessExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class BaseTestClass
{
    public int member;
}

class TestClass : BaseTestClass
{
    void TestMethod()
    {
        base.member = 0;
    }
}", @"Friend Class BaseTestClass
    Public member As Integer
End Class

Friend Class TestClass
    Inherits BaseTestClass

    Private Sub TestMethod()
        member = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task ReferenceTypeComparison()
        {
            await TestConversionCSharpToVisualBasic(@"public static bool AreTwoObjectsReferenceEqual()
{
    return new object() == new object();
}", @"Public Shared Function AreTwoObjectsReferenceEqual() As Boolean
    Return New Object Is New Object
End Function");
        }

        [Fact]
        public async Task TupleType()
        {
            await TestConversionCSharpToVisualBasic(@"public interface ILanguageConversion
{
    IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings();
    IEnumerable<(string, string)> GetProjectFileReplacementRegexes();
}", @"Public Interface ILanguageConversion
    Function GetProjectTypeGuidMappings() As IReadOnlyCollection(Of (String, String))
    Function GetProjectFileReplacementRegexes() As IEnumerable(Of (String, String))
End Interface");
        }

        [Fact]
        public async Task ValueTupleType()
        {
            await TestConversionCSharpToVisualBasic(@"using System;
using System.Collections.Generic;

namespace PreHOPL
{
    static class Program
    {
        private static readonly Dictionary<string, ValueTuple<int, Delegate>> dict =
            new Dictionary<string, ValueTuple<int, Delegate>>()
        {
            [""SAY""] =  (1, (Action<string>)System.Console.WriteLine)
        };
        private static void Main(string[] args)
        {
            dict[""SAY""].Item2.DynamicInvoke(""Hello World!"");
        }
    }
}", @"Imports System
Imports System.Collections.Generic

Namespace PreHOPL
    Friend Module Program
        Private ReadOnly dict As Dictionary(Of String, ValueTuple(Of Integer, [Delegate])) = New Dictionary(Of String, ValueTuple(Of Integer, [Delegate]))() From {
            {""SAY"", (1, CType(AddressOf System.Console.WriteLine, Action(Of String)))}
        }

        Private Sub Main(ByVal args As String())
            dict(""SAY"").Item2.DynamicInvoke(""Hello World!"")
        End Sub
    End Module
End Namespace");
        }

        [Fact]
        public async Task DelegateExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{

    private static Action<int> m_Event1 = delegate { };

    void TestMethod()
    {
        var test = delegate(int a) { return a * 2 };

        test(3);
    }
}", @"Friend Class TestClass
    Private Shared m_Event1 As Action(Of Integer) = Sub()
                                                    End Sub

    Private Sub TestMethod()
        Dim test = Function(ByVal a As Integer) a * 2
        test(3)
    End Sub
End Class");
        }

        [Fact]
        public async Task ExpressionSub()
        {
            await TestConversionCSharpToVisualBasic(@"using System;

static class Program
{
    private static void Main(string[] args)
    {
        Action<string> x = (Action<string>)(_ => Environment.Exit(0));
    }
}", @"Imports System

Friend Module Program
    Private Sub Main(ByVal args As String())
        Dim x = (Sub(__) Environment.Exit(0))
    End Sub
End Module");
        }

        [Fact]
        public async Task LambdaBodyExpression()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        var test = a => { return a * 2 };
        var test2 = (a, b) => { if (b > 0) return a / b; return 0; }
        var test3 = (a, b) => a % b;

        test(3);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim test = Function(a) a * 2
        Dim test2 = Function(a, b)
                        If b > 0 Then Return a / b
                        Return 0
                    End Function

        Dim test3 = Function(a, b) a Mod b
        test(3)
    End Sub
End Class");
        }

        [Fact]
        public async Task Await()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    Task<int> SomeAsyncMethod()
    {
        return Task.FromResult(0);
    }

    async void TestMethod()
    {
        int result = await SomeAsyncMethod();
        Console.WriteLine(result);
    }
}", @"Friend Class TestClass
    Private Function SomeAsyncMethod() As Task(Of Integer)
        Return Task.FromResult(0)
    End Function

    Private Async Sub TestMethod()
        Dim result = Await SomeAsyncMethod
        Console.WriteLine(result)
    End Sub
End Class");
        }

        [Fact]
        public async Task Linq1()
        {
            await TestConversionCSharpToVisualBasic(@"static void SimpleQuery()
{
    int[] numbers = { 7, 9, 5, 3, 6 };

    var res = from n in numbers
                where n > 5
                select n;

    foreach (var n in res)
        Console.WriteLine(n);
}",
                @"Private Shared Sub SimpleQuery()
    Dim numbers = {7, 9, 5, 3, 6}
    Dim res = From n In numbers Where n > 5 Select n

    For Each n In res
        Console.WriteLine(n)
    Next
End Sub");
        }

        [Fact]
        public async Task Linq2()
        {
            await TestConversionCSharpToVisualBasic(@"public static void Linq40()
    {
        int[] numbers = { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };

        var numberGroups =
            from n in numbers
            group n by n % 5 into g
            select new {
                Remainder = g.Key,
                Numbers = g
            };

        foreach (var g in numberGroups)
        {
            Console.WriteLine($""Numbers with a remainder of {g.Remainder} when divided by 5:"");
            foreach (var n in g.Numbers)
            {
                Console.WriteLine(n);
            }
        }
    }",
                @"Public Shared Sub Linq40()
    Dim numbers = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
    Dim numberGroups = From n In numbers Group n By __groupByKey1__ = n Mod 5 Into g = Group Select New With {
        .Remainder = __groupByKey1__,
        .Numbers = g
    }

    For Each g In numberGroups
        Console.WriteLine($""Numbers with a remainder of {g.Remainder} when divided by 5:"")

        For Each n In g.Numbers
            Console.WriteLine(n)
        Next
    Next
End Sub");
        }

        [Fact]
        public async Task Linq3()
        {
            await TestConversionCSharpToVisualBasic(@"class Product {
    public string Category;
    public string ProductName;
}

class Test {
    public void Linq102()
    {
        string[] categories = new string[]{
            ""Beverages"",
            ""Condiments"",
            ""Vegetables"",
            ""Dairy Products"",
            ""Seafood"" };

            Product[] products = GetProductList();

            var q =
                from c in categories
                join p in products on c equals p.Category
                select new {
                    Category = c, p.ProductName
                };

        foreach (var v in q)
        {
            Console.WriteLine($""{v.ProductName}: {v.Category}"");
        }
    }
}",
                @"Friend Class Product
    Public Category As String
    Public ProductName As String
End Class

Friend Class Test
    Public Sub Linq102()
        Dim categories = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
        Dim products As Product() = GetProductList()
        Dim q = From c In categories Join p In products On c Equals p.Category Select New With {
            .Category = c, p.ProductName
        }

        For Each v In q
            Console.WriteLine($""{v.ProductName}: {v.Category}"")
        Next
    End Sub
End Class");
        }

        [Fact]
        public async Task Linq4()
        {
            await TestConversionCSharpToVisualBasic(@"public void Linq103()
{
    string[] categories = new string[]{
        ""Beverages"",
        ""Condiments"",
        ""Vegetables"",
        ""Dairy Products"",
        ""Seafood"" };

        var products = GetProductList();

        var q =
            from c in categories
            join p in products on c equals p.Category into ps
            select new {
                Category = c,
                Products = ps
            };

    foreach (var v in q)
    {
        Console.WriteLine(v.Category + "":"");
        foreach (var p in v.Products)
        {
            Console.WriteLine(""   "" + p.ProductName);
        }
    }
}", @"Public Sub Linq103()
    Dim categories = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
    Dim products = GetProductList()
    Dim q = From c In categories Group Join p In products On c Equals p.Category Into ps = Group Select New With {
        .Category = c,
        .Products = ps
    }

    For Each v In q
        Console.WriteLine(v.Category & "":"")

        For Each p In v.Products
            Console.WriteLine(""   "" & p.ProductName)
        Next
    Next
End Sub");
        }
        [Fact]
        public async Task MultilineSubExpressionWithSingleStatement() {
            await TestConversionCSharpToVisualBasic(
@"public class TestClass : System.Collections.ObjectModel.ObservableCollection<string> {
    public TestClass() {
        PropertyChanged += (o, e) => {
            if (e.PropertyName == ""AnyProperty"") {
                Add(""changed"");
            } else
                RemoveAt(0);
        };
    }
}",
                @"Public Class TestClass
    Inherits ObjectModel.ObservableCollection(Of String)

    Public Sub New()
        AddHandler PropertyChanged, Sub(o, e)
                                        If e.PropertyName Is ""AnyProperty"" Then
                                            Add(""changed"")
                                        Else
                                            RemoveAt(0)
                                        End If
                                    End Sub
    End Sub
End Class");
        }
        [Fact]
        public async Task MultilineFunctionExpressionWithSingleStatement() {
            await TestConversionCSharpToVisualBasic(
@"using System;
public class TestClass {
    Func<object, string> create = o => {
        if(o is TestClass)
            return ""first"";
        else
            return ""second"";
    };
    public TestClass() {
        string str = create(this);
    }
}",
                @"Imports System

Public Class TestClass
    Private create As Func(Of Object, String) = Function(o)
                                                    If TypeOf o Is TestClass Then
                                                        Return ""first""
                                                    Else
                                                        Return ""second""
                                                    End If
                                                End Function

    Public Sub New()
        Dim str = create(Me)
    End Sub
End Class");
        }
    }
}
