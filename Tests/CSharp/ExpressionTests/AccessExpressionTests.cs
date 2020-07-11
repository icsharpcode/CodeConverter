using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    /// <summary>
    /// Member/Element access
    /// </summary>
    public class AccessExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task MyClassExprAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class TestClass
    Sub TestMethod()
        MyClass.Val = 6
    End Sub

    Shared Val As Integer
End Class", @"
public partial class TestClass
{
    public void TestMethod()
    {
        Val = 6;
    }

    private static int Val;
}");
        }

        [Fact]
        public async Task DictionaryIndexingIssue362Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System ' Removed by simplifier
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Dim Dict As New Dictionary(Of Integer, String)

    Sub Main()
        Dim x = Dict.Values(0).Length
    End Sub
End Module", @"using System.Collections.Generic;
using System.Linq;

internal static partial class Module1
{
    private static Dictionary<int, string> Dict = new Dictionary<int, string>();

    public static void Main()
    {
        int x = Dict.Values.ElementAtOrDefault(0).Length;
    }
}");
        }

        [Fact]
        public async Task MethodCallDictionaryAccessConditionalAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class A
    Public Sub Test()
        Dim dict = New Dictionary(Of String, String) From {{""a"", ""AAA""}, {""b"", ""bbb""}}
        Dim v = dict?.Item(""a"")
    End Sub
End Class", @"using System.Collections.Generic;

public partial class A
{
    public void Test()
    {
        var dict = new Dictionary<string, string>() { { ""a"", ""AAA"" }, { ""b"", ""bbb"" } };
        string v = dict?[""a""];
    }
}");
        }

        [Fact]
        public async Task IndexerWithParameterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data

Public Class A
    Public Function ReadDataSet(myData As DataSet) As String
        With myData.Tables(0).Rows(0)
            Return .Item(""MY_COLUMN_NAME"").ToString()
        End With
    End Function
End Class", @"using System.Data;

public partial class A
{
    public string ReadDataSet(DataSet myData)
    {
        {
            var withBlock = myData.Tables[0].Rows[0];
            return withBlock[""MY_COLUMN_NAME""].ToString();
        }
    }
}");
        }

        [Fact]
        public async Task MethodCallArrayIndexerBracketsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class A
    Public Sub Test()
        Dim str1 = Me.GetStringFromNone(0)
        str1 = GetStringFromNone(0)
        Dim str2 = GetStringFromNone()(1)
        Dim str3 = Me.GetStringsFromString(""abc"")
        str3 = GetStringsFromString(""abc"")
        Dim str4 = GetStringsFromString(""abc"")(1)
        Dim fromStr3 = GetMoreStringsFromString(""bc"")(1)(0)
        Dim explicitNoParameter = GetStringsFromAmbiguous()(0)(1)
        Dim usesParameter1 = GetStringsFromAmbiguous(0)(1)(2)
    End Sub

    Function GetStringFromNone() As String()
        Return New String() { ""A"", ""B"", ""C""}
    End Function

    Function GetStringsFromString(parm As String) As String()
        Return New String() { ""1"", ""2"", ""3""}
    End Function

    Function GetMoreStringsFromString(parm As String) As String()()
        Return New String()() { New String() { ""1"" }}
    End Function

    Function GetStringsFromAmbiguous() As String()()
        Return New String()() { New String() { ""1"" }}
    End Function

    Function GetStringsFromAmbiguous(amb As Integer) As String()()
        Return New String()() { New String() { ""1"" }}
    End Function
End Class", @"
public partial class A
{
    public void Test()
    {
        string str1 = GetStringFromNone()[0];
        str1 = GetStringFromNone()[0];
        string str2 = GetStringFromNone()[1];
        var str3 = GetStringsFromString(""abc"");
        str3 = GetStringsFromString(""abc"");
        string str4 = GetStringsFromString(""abc"")[1];
        string fromStr3 = GetMoreStringsFromString(""bc"")[1][0];
        string explicitNoParameter = GetStringsFromAmbiguous()[0][1];
        string usesParameter1 = GetStringsFromAmbiguous(0)[1][2];
    }

    public string[] GetStringFromNone()
    {
        return new string[] { ""A"", ""B"", ""C"" };
    }

    public string[] GetStringsFromString(string parm)
    {
        return new string[] { ""1"", ""2"", ""3"" };
    }

    public string[][] GetMoreStringsFromString(string parm)
    {
        return new string[][] { new string[] { ""1"" } };
    }

    public string[][] GetStringsFromAmbiguous()
    {
        return new string[][] { new string[] { ""1"" } };
    }

    public string[][] GetStringsFromAmbiguous(int amb)
    {
        return new string[][] { new string[] { ""1"" } };
    }
}");
        }

        [Fact]
        public async Task ElementAtOrDefaultIndexingAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = """".Split("",""c).Select(Function(x) x)
        Dim z = y(0)
    End Sub
End Class", @"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
        string z = y.ElementAtOrDefault(0);
    }
}");
        }

        [Fact]
        public async Task ElementAtOrDefaultInvocationIsNotDuplicatedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = """".Split("",""c).Select(Function(x) x)
        Dim z = y.ElementAtOrDefault(0)
    End Sub
End Class", @"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
        string z = y.ElementAtOrDefault(0);
    }
}");
        }

        [Fact]
        public async Task EmptyArgumentListsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim str = (New ThreadStaticAttribute).ToString
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        string str = new ThreadStaticAttribute().ToString();
    }
}");
        }

        [Fact]
        public async Task UsesSquareBracketsForIndexerButParenthesesForMethodInvocationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function TestMethod() As String()
        Dim s = ""1,2""
        Return s.Split(s(1))
    End Function
End Class", @"
internal partial class TestClass
{
    private string[] TestMethod()
    {
        string s = ""1,2"";
        return s.Split(s[1]);
    }
}");
        }

        [Fact]
        public async Task ConditionalExpressionWithOmittedArgsListAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result = str?.GetType
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var result = str?.GetType();
    }
}");
        }

        [Fact]
        public async Task MemberAccessAndInvocationExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim length As Integer
        length = str.Length
        Console.WriteLine(""Test"" & length)
        Console.ReadKey()
    End Sub
End Class", @"using System;

internal partial class TestClass
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
        public async Task OmittedParamsArrayAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Module AppBuilderUseExtensions
    <System.Runtime.CompilerServices.Extension>
    Function Use(Of T)(ByVal app As String, ParamArray args As Object()) As Object
        Return Nothing
    End Function
End Module

Class TestClass
    Private Sub TestMethod(ByVal str As String)
        str.Use(Of object)
    End Sub
End Class", @"
internal static partial class AppBuilderUseExtensions
{
    public static object Use<T>(this string app, params object[] args)
    {
        return null;
    }
}

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        str.Use<object>();
    }
}");
        }

        [Fact]
        public async Task ThisMemberAccessExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private member As Integer

    Private Sub TestMethod()
        Me.member = 0
    End Sub
End Class", @"
internal partial class TestClass
{
    private int member;

    private void TestMethod()
    {
        member = 0;
    }
}");
        }

        [Fact]
        public async Task BaseMemberAccessExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class BaseTestClass
    Public member As Integer
End Class

Class TestClass
    Inherits BaseTestClass

    Private Sub TestMethod()
        MyBase.member = 0
    End Sub
End Class", @"
internal partial class BaseTestClass
{
    public int member;
}

internal partial class TestClass : BaseTestClass
{
    private void TestMethod()
    {
        member = 0;
    }
}");
        }

        [Fact]
        public async Task UnqualifiedBaseMemberAccessExpressionAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class BaseController
    Protected Request As HttpRequest
End Class

Public Class ActualController
    Inherits BaseController

    Public Sub Do()
        Request.StatusCode = 200
    End Sub
End Class", @"
public partial class BaseController
{
    protected HttpRequest Request;
}

public partial class ActualController : BaseController
{
    public void Do()
    {
        Request.StatusCode = 200;
    }
}
2 source compilation errors:
BC30183: Keyword is not valid as an identifier.
BC30002: Type 'HttpRequest' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'HttpRequest' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task PartiallyQualifiedNameAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections ' Removed by simplifier
Class TestClass
    Public Sub TestMethod(dir As String)
        IO.Path.Combine(dir, ""file.txt"")
        Dim c As New ObjectModel.ObservableCollection(Of String)
    End Sub
End Class", @"using System.IO;

internal partial class TestClass
{
    public void TestMethod(string dir)
    {
        Path.Combine(dir, ""file.txt"");
        var c = new System.Collections.ObjectModel.ObservableCollection<string>();
    }
}");
        }

        [Fact]
        public async Task TypePromotedModuleIsQualifiedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace TestNamespace
    Public Module TestModule
        Public Sub ModuleFunction()
        End Sub
    End Module
End Namespace

Class TestClass
    Public Sub TestMethod(dir As String)
        TestNamespace.ModuleFunction()
    End Sub
End Class", @"
namespace TestNamespace
{
    public static partial class TestModule
    {
        public static void ModuleFunction()
        {
        }
    }
}

internal partial class TestClass
{
    public void TestMethod(string dir)
    {
        TestNamespace.TestModule.ModuleFunction();
    }
}");
        }

        [Fact]
        public async Task MemberAccessCasingAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Bar()

    End Sub

    Sub Foo()
        bar()
        me.bar()
    End Sub
End Class", @"
public partial class Class1
{
    public void Bar()
    {
    }

    public void Foo()
    {
        Bar();
        Bar();
    }
}");
        }

        [Fact]
        public async Task XmlMemberAccessAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Private Sub LoadValues(ByVal strPlainKey As String)
        Dim xmlFile As XDocument = XDocument.Parse(strPlainKey)
        Dim objActivationInfo As XElement = xmlFile.<ActivationKey>.First
    End Sub
End Class", @"using System.Linq;
using System.Xml.Linq;

public partial class Class1
{
    private void LoadValues(string strPlainKey)
    {
        var xmlFile = XDocument.Parse(strPlainKey);
        var objActivationInfo = xmlFile.Elements(""ActivationKey"").First();
    }
}");
        }

        [Fact]
        public async Task ExclamationPointOperatorAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Class Issue479
  Default Public ReadOnly Property index(ByVal s As String) As Integer
    Get
      Return 32768 + AscW(s)
    End Get
  End Property
End Class

Public Class TestIssue479
  Public Sub compareAccess()
    Dim hD As Issue479 = New Issue479()
    System.Console.WriteLine(""Traditional access returns "" & hD.index(""X"") & vbCrLf & 
      ""Default property access returns "" & hD(""X"") & vbCrLf &
      ""Dictionary access returns "" & hD!X)
  End Sub
End Class",
                @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Issue479
{
    public int this[string s]
    {
        get
        {
            return 32768 + Strings.AscW(s);
        }
    }
}

public partial class TestIssue479
{
    public void compareAccess()
    {
        var hD = new Issue479();
        Console.WriteLine(""Traditional access returns "" + hD[""X""] + Constants.vbCrLf + ""Default property access returns "" + hD[""X""] + Constants.vbCrLf + ""Dictionary access returns "" + hD[""X""]);
    }
}");
        }

        [Fact]
        public async Task AliasedImportsWithTypePromotionIssue401Async()
        {
            for (int i = 0; i < 3; i++) {
                try {
                    // I believe there are two valid simplifications and the simplifier is non-deterministic
                    // Just retry a few times and see if we get the one we expect before failing
                    // At the same time as this loop I added "aliasedAgain" in the hope that it'd discourage the simplifier from fully qualifying Strings
                    await FlakeyAliasedImportsWithTypePromotionIssue401Async();
                    return;
                } catch (Exception e) {
                }
            }

            await FlakeyAliasedImportsWithTypePromotionIssue401Async();
        }

        private async Task FlakeyAliasedImportsWithTypePromotionIssue401Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Imports System.IO
Imports SIO = System.IO
Imports Microsoft.VisualBasic
Imports VB = Microsoft.VisualBasic

Public Class Test
    Private aliased As String = VB.Left(""SomeText"", 1)
    Private aliasedAgain As String = VB.Left(""SomeText"", 1)
    Private aliased2 As System.Delegate = New SIO.ErrorEventHandler(AddressOf OnError)

    ' Make use of the non-aliased imports, but ensure there's a name clash that requires the aliases in the above case
    Private Tr As String = NameOf(TextReader)
    Private Strings As String = NameOf(AppWinStyle)

    Class ErrorEventHandler
    End Class

    Shared Sub OnError(s As Object, e As ErrorEventArgs)
    End Sub
End Class",
                @"using System;
using System.IO;
using SIO = System.IO;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using VB = Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Test
{
    private string aliased = VB.Strings.Left(""SomeText"", 1);
    private string aliasedAgain = VB.Strings.Left(""SomeText"", 1);
    private Delegate aliased2 = new SIO.ErrorEventHandler(OnError);

    // Make use of the non-aliased imports, but ensure there's a name clash that requires the aliases in the above case
    private string Tr = nameof(TextReader);
    private string Strings = nameof(AppWinStyle);

    public partial class ErrorEventHandler
    {
    }

    public static void OnError(object s, ErrorEventArgs e)
    {
    }
}");
        }

        [Fact]
        public async Task TestGenericMethodGroupGainsBracketsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Enum TheType
    Tree
End Enum

Public Class MoreParsing
    Sub DoGet()
        Dim anon = New With {
            .TheType = GetEnumValues(Of TheType)
        }
    End Sub

    Private Function GetEnumValues(Of TEnum)() As IDictionary(Of Integer, String)
        Return System.Enum.GetValues(GetType(TEnum)).Cast(Of TEnum).
            ToDictionary(Function(enumValue) DirectCast(DirectCast(enumValue, Object), Integer),
                         Function(enumValue) enumValue.ToString())
    End Function
End Class",
                @"using System;
using System.Collections.Generic;
using System.Linq;

public enum TheType
{
    Tree
}

public partial class MoreParsing
{
    public void DoGet()
    {
        var anon = new { TheType = GetEnumValues<TheType>() };
    }

    private IDictionary<int, string> GetEnumValues<TEnum>()
    {
        return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToDictionary(enumValue => (int)(object)enumValue, enumValue => enumValue.ToString());
    }
}");
        }

        [Fact]
        public async Task UsesSquareBracketsForItemIndexerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data

Class TestClass
    Function GetItem(dr As DataRow) As Object
        Return dr.Item(""col1"")
    End Function
End Class", @"using System.Data;

internal partial class TestClass
{
    public object GetItem(DataRow dr)
    {
        return dr[""col1""];
    }
}");
        }
    }
}
