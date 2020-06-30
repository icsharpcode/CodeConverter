using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB
{
    public class ExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task MultilineStringAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task StringInterpolationWithDoubleQuotesAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
End Namespace

1 source compilation errors:
CS7000: Unexpected use of an aliased name");
        }

        [Fact]
        public async Task ConditionalExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod(string str)
    {
        bool result = (str == """") ? true : false;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = If(Equals(str, """"), True, False)
    End Sub
End Class");
        }

        [Fact]
        public async Task DefaultLiteralExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public class DefaultLiteralExpression {

    public bool Foo {
        get {
	        return (Bar == default);
        }
    }

    public int Bar;

}", @"Public Class DefaultLiteralExpression
    Public ReadOnly Property Foo As Boolean
        Get
            Return Bar = Nothing
        End Get
    End Property

    Public Bar As Integer
End Class");
        }

        [Fact]
        public async Task IsNullExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public class Test {

    public bool Foo {
        get {
	    return (Bar is null); //Crashes conversion to VB
        }
    }

    public string Bar;

}", @"Public Class Test
    Public ReadOnly Property Foo As Boolean
        Get
            Return Bar Is Nothing 'Crashes conversion to VB
        End Get
    End Property

    Public Bar As String
End Class");
        }

        [Fact]
        public async Task IfIsPatternExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 target compilation errors:
BC30451: 'CSharpImpl.__Assign' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task DeclarationExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System.Collections.Generic;

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
        public async Task ThrowExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System;

class TestClass
{
    void TestMethod(string str)
    {
        bool result = (str == """") ? throw new Exception(""empty"") : false;
    }
}", @"Imports System

Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Boolean = If(Equals(str, """"), CSharpImpl.__Throw(Of Boolean)(New Exception(""empty"")), False)
    End Sub

    Private Class CSharpImpl
        <Obsolete(""Please refactor calling code to use normal throw statements"")>
        Shared Function __Throw(Of T)(ByVal e As Exception) As T
            Throw e
        End Function
    End Class
End Class

1 target compilation errors:
BC30451: 'CSharpImpl.__Throw' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task NameOfAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    private string n = nameof(TestMethod);

    private void TestMethod()
    {
    }
}", @"Friend Class TestClass
    Private n As String = NameOf(TestMethod)

    Private Sub TestMethod()
    End Sub
End Class");
        }

        [Fact]
        public async Task NullCoalescingExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod(string str)
    {
        Console.WriteLine(str ?? ""<null>"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Console.WriteLine(If(str, ""<null>""))
    End Sub
End Class

1 source compilation errors:
CS0103: The name 'Console' does not exist in the current context");
        }
        [Fact]
        public async Task CoalescingExpression_AssignmentAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
            Return If(prop, Function()
                                prop2 = CreateProperty()
                                Return prop2
                            End Function())
        End Get
    End Property

    Private Function CreateProperty() As String
        Return """"
    End Function
End Class

1 source compilation errors:
CS0149: Method name expected");
        }

        [Fact]
        public async Task MemberAccessAndInvocationExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 source compilation errors:
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task CallInvokeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod(string str)
    {
        Dispatcher.Invoke(new Action(() => Console.WriteLine(1)));
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dispatcher.Invoke(New Action(Function() Console.WriteLine(1)))
    End Sub
End Class

2 source compilation errors:
CS0103: The name 'Dispatcher' does not exist in the current context
CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)
2 target compilation errors:
BC30451: 'Dispatcher' is not declared. It may be inaccessible due to its protection level.
BC30491: Expression does not produce a value.");
        }

        [Fact]
        public async Task ShiftOperatorsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public class Test
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
        Dim y As Integer = 1
        y <<= 1
        y >>= 1
        y = y << 1
        y = y >> 1
    End Sub
End Class");
        }
        [Fact]
        public async Task CompoundAssignmentTestAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class TestClass {
    void TestMethod() {
        int x = 10;
        x *= 3;
        x /= 3;
    }
}",
                @"Public Class TestClass
    Private Sub TestMethod()
        Dim x As Integer = 10
        x *= 3
        x /= 3
    End Sub
End Class");
        }

        [Fact]
        public async Task ElvisOperatorExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        Dim length As Integer = If(str?.Length, -1)
        Console.WriteLine(length)
        Console.ReadKey()
        Dim redirectUri As String = context.OwinContext.Authentication?.AuthenticationResponseChallenge?.Properties?.RedirectUri
    End Sub
End Class

2 source compilation errors:
CS0103: The name 'Console' does not exist in the current context
CS0103: The name 'context' does not exist in the current context
1 target compilation errors:
BC30451: 'context' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task ObjectInitializerExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"
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
        public async Task ObjectInitializerExpression2Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task ObjectInitializerExpression3Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System.Collections.Generic;

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
        public async Task ThisMemberAccessExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task BaseMemberAccessExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class BaseTestClass
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
        public async Task ReferenceTypeComparisonAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public static bool AreTwoObjectsReferenceEqual()
{
    return new object() == new object();
}", @"Public Shared Function AreTwoObjectsReferenceEqual() As Boolean
    Return New Object() Is New Object()
End Function");
        }

        [Fact]
        public async Task TupleTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public interface ILanguageConversion
{
    IReadOnlyCollection<(string, string)> GetProjectTypeGuidMappings();
    IEnumerable<(string, string)> GetProjectFileReplacementRegexes();
}", @"Public Interface ILanguageConversion
    Function GetProjectTypeGuidMappings() As IReadOnlyCollection(Of (String, String))
    Function GetProjectFileReplacementRegexes() As IEnumerable(Of (String, String))
End Interface

2 source compilation errors:
CS0246: The type or namespace name 'IReadOnlyCollection<>' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'IEnumerable<>' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task ValueTupleTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System;
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
            {""SAY"", (1, CType(AddressOf Console.WriteLine, Action(Of String)))}
        }

        Private Sub Main(ByVal args As String())
            dict(""SAY"").Item2.DynamicInvoke(""Hello World!"")
        End Sub
    End Module
End Namespace");
        }

        [Fact]
        public async Task DelegateExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

2 source compilation errors:
CS1002: ; expected
CS0246: The type or namespace name 'Action<>' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task ExpressionSubAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System;

static class Program
{
    private static void Main(string[] args)
    {
        Action<string> x = (Action<string>)(_ => Environment.Exit(0));
    }
}", @"Imports System

Friend Module Program
    Private Sub Main(ByVal args As String())
        Dim x As Action(Of String) = Sub(__) Environment.Exit(0)
    End Sub
End Module");
        }

        [Fact]
        public async Task LambdaBodyExpressionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

2 source compilation errors:
CS1002: ; expected
CS0815: Cannot assign lambda expression to an implicitly-typed variable");
        }

        [Fact]
        public async Task AwaitAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        Dim result As Integer = Await SomeAsyncMethod()
        Console.WriteLine(result)
    End Sub
End Class

3 source compilation errors:
CS0246: The type or namespace name 'Task<>' could not be found (are you missing a using directive or an assembly reference?)
CS0103: The name 'Task' does not exist in the current context
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task Linq1Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"static void SimpleQuery()
{
    int[] numbers = { 7, 9, 5, 3, 6 };

    var res = from n in numbers
                where n > 5
                select n;

    foreach (var n in res)
        Console.WriteLine(n);
}",
                @"Private Shared Sub SimpleQuery()
    Dim numbers As Integer() = {7, 9, 5, 3, 6}
    Dim res = From n In numbers Where n > 5 Select n

    For Each n In res
        Console.WriteLine(n)
    Next
End Sub

2 source compilation errors:
CS1935: Could not find an implementation of the query pattern for source type 'int[]'.  'Where' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task Linq2Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public static void Linq40()
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
    Dim numbers As Integer() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
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
End Sub

2 source compilation errors:
CS1935: Could not find an implementation of the query pattern for source type 'int[]'.  'GroupBy' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task Linq3Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class Product {
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
        Dim categories As String() = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
        Dim products As Product() = GetProductList()
        Dim q = From c In categories Join p In products On c Equals p.Category Select New With {
            .Category = c, p.ProductName
        }

        For Each v In q
            Console.WriteLine($""{v.ProductName}: {v.Category}"")
        Next
    End Sub
End Class

3 source compilation errors:
CS0103: The name 'GetProductList' does not exist in the current context
CS1935: Could not find an implementation of the query pattern for source type 'string[]'.  'Join' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
CS0103: The name 'Console' does not exist in the current context
1 target compilation errors:
BC30451: 'GetProductList' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task Linq4Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public void Linq103()
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
    Dim categories As String() = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
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
End Sub

3 source compilation errors:
CS0103: The name 'GetProductList' does not exist in the current context
CS1935: Could not find an implementation of the query pattern for source type 'string[]'.  'GroupJoin' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
CS0103: The name 'Console' does not exist in the current context
3 target compilation errors:
BC30451: 'GetProductList' is not declared. It may be inaccessible due to its protection level.
BC36593: Expression of type '?' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.
BC32023: Expression is of type '?', which is not a collection type.");
        }
        [Fact]
        public async Task MultilineSubExpressionWithSingleStatementAsync() {
            await TestConversionCSharpToVisualBasicAsync(
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
                                        If Equals(e.PropertyName, ""AnyProperty"") Then
                                            Add(""changed"")
                                        Else
                                            RemoveAt(0)
                                        End If
                                    End Sub
    End Sub
End Class");
        }
        [Fact]
        public async Task MultilineFunctionExpressionWithSingleStatementAsync() {
            await TestConversionCSharpToVisualBasicAsync(
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
        Dim str As String = create(Me)
    End Sub
End Class");
        }

        [Fact]
        public async Task PrefixUnaryExpression_SingleLineFunctionAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class TestClass {
    public TestClass() {
        System.Func<string, bool> func = o => !string.IsNullOrEmpty(""test"");
    }
}",
@"Public Class TestClass
    Public Sub New()
        Dim func As Func(Of String, Boolean) = Function(o) Not String.IsNullOrEmpty(""test"")
    End Sub
End Class");
        }

        [Fact]
        public async Task Issue486_MustCastNothingAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class WhyWeNeedToCastNothing
{
    public void Example(int? vbInitValue)
    {
        var withDefault = vbInitValue != null ? 7 : default(int?);
        var withNull = vbInitValue != null ? (int?)8 : null;
    }
}",
@"Public Class WhyWeNeedToCastNothing
    Public Sub Example(ByVal vbInitValue As Integer?)
        Dim withDefault = If(vbInitValue IsNot Nothing, 7, DirectCast(Nothing, Integer?))
        Dim withNull = If(vbInitValue IsNot Nothing, 8, DirectCast(Nothing, Integer?))
    End Sub
End Class");
        }

        [Fact]
        public async Task EqualsExpressionAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"public class TestClass {
    public TestClass() {
        int i = 0;
        int j = 0;
        string s1 = ""string1"";
        string s2 = ""string2"";
        object object1 = s1;
        object object2 = s2;
        if(i == j)
            DoSomething();
        if(i == s2)
            DoSomething();
        if(i == object1)
            DoSomething();
        if(s1 == j)
            DoSomething();
        if(s1 == s2)
            DoSomething();
        if(s1 == object2)
            DoSomething();
        if(object1 == j)
            DoSomething();
        if(object1 == s2)
            DoSomething();
        if(object1 == object2)
            DoSomething();
    }
    public void DoSomething() { }
}",
@"Public Class TestClass
    Public Sub New()
        Dim i As Integer = 0
        Dim j As Integer = 0
        Dim s1 As String = ""string1""
        Dim s2 As String = ""string2""
        Dim object1 As Object = s1
        Dim object2 As Object = s2
        If i = j Then DoSomething()
        If i = s2 Then DoSomething()
        If i = object1 Then DoSomething()
        If s1 = j Then DoSomething()
        If Equals(s1, s2) Then DoSomething()
        If s1 Is object2 Then DoSomething()
        If object1 = j Then DoSomething()
        If object1 Is s2 Then DoSomething()
        If object1 Is object2 Then DoSomething()
    End Sub

    Public Sub DoSomething()
    End Sub
End Class

4 source compilation errors:
CS0019: Operator '==' cannot be applied to operands of type 'int' and 'string'
CS0019: Operator '==' cannot be applied to operands of type 'int' and 'object'
CS0019: Operator '==' cannot be applied to operands of type 'string' and 'int'
CS0019: Operator '==' cannot be applied to operands of type 'object' and 'int'");
        }
    }
}
