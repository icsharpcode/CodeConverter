using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

/// <summary>
/// Member/Element access
/// </summary>
public class AccessExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task MyClassExprAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class TestClass
{
    public void TestMethod()
    {
        Val = 6;
    }

    private static int Val;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DictionaryIndexingIssue769Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;

public partial class Classinator769
{
    private Dictionary<int, string> _dictionary = new Dictionary<int, string>();

    private void AccessDictionary()
    {
        if (_dictionary[2] == ""StringyMcStringface"")
        {
            Console.WriteLine(""It is true"");
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DictionaryIndexingIssue362Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

internal static partial class Module1
{
    private static Dictionary<int, string> Dict = new Dictionary<int, string>();

    public static void Main()
    {
        int x = Dict.Values.ElementAtOrDefault(0).Length;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MethodCallDictionaryAccessConditionalAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;

public partial class A
{
    public void Test()
    {
        var dict = new Dictionary<string, string>() { { ""a"", ""AAA"" }, { ""b"", ""bbb"" } };
        string v = dict?[""a""];
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IndexerWithParameterAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;

public partial class A
{
    public string ReadDataSet(DataSet myData)
    {
        {
            var withBlock = myData.Tables[0].Rows[0];
            return withBlock[""MY_COLUMN_NAME""].ToString();
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MethodCallArrayIndexerBracketsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class A
{
    public void Test()
    {
        string str1 = GetStringFromNone()[0];
        str1 = GetStringFromNone()[0];
        string str2 = GetStringFromNone()[1];
        string[] str3 = GetStringsFromString(""abc"");
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ElementAtOrDefaultIndexingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
        string z = y.ElementAtOrDefault(0);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task DataTableIndexingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;
using System.Linq;

internal partial class TestClass
{
    private readonly DataTable _myTable;

    public void TestMethod()
    {
        var dataRow = _myTable.AsEnumerable().ElementAtOrDefault(0);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ElementAtOrDefaultInvocationIsNotDuplicatedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var y = """".Split(',').Select(x => x);
        string z = y.ElementAtOrDefault(0);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task EmptyArgumentListsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        string str = new ThreadStaticAttribute().ToString();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UsesSquareBracketsForIndexerButParenthesesForMethodInvocationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private string[] TestMethod()
    {
        string s = ""1,2"";
        return s.Split(s[1]);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ConditionalExpressionWithOmittedArgsListAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(string str)
    {
        var result = str?.GetType();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MemberAccessAndInvocationExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(string str)
    {
        int length;
        length = str.Length;
        Console.WriteLine(""Test"" + length);
        Console.ReadKey();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OmittedParamsArrayAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ThisMemberAccessExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class TestClass
{
    private int member;

    private void TestMethod()
    {
        member = 0;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task BaseMemberAccessExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UnqualifiedBaseMemberAccessExpressionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
CS0246: The type or namespace name 'HttpRequest' could not be found (are you missing a using directive or an assembly reference?)", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PartiallyQualifiedNameAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.IO;

internal partial class TestClass
{
    public void TestMethod(string dir)
    {
        Path.Combine(dir, ""file.txt"");
        var c = new System.Collections.ObjectModel.ObservableCollection<string>();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TypePromotedModuleIsQualifiedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task MemberAccessCasingAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task XmlMemberAccessAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Linq;
using System.Xml.Linq;

public partial class Class1
{
    private void LoadValues(string strPlainKey)
    {
        var xmlFile = XDocument.Parse(strPlainKey);
        var objActivationInfo = xmlFile.Elements(""ActivationKey"").First();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExclamationPointOperatorAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExclamationPointOperator765Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;

public partial class Issue765
{
    public void GetByName(IDataReader dataReader)
    {
        object foo;
        foo = dataReader[""foo""];
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task AliasedImportsWithTypePromotionIssue401Async()
    {
        for (int i = 0; i < 3; i++) {
            try {
                await FlakeyAliasedImportsWithTypePromotionIssue401Async();
                return;
            } catch (Exception) {
                // I believe there are two valid simplifications and the simplifier is non-deterministic
                // Just retry a few times and see if we get the one we expect before failing
                // At the same time as this loop I added "aliasedAgain" in the hope that it'd discourage the simplifier from fully qualifying Strings
            }
        }

        await FlakeyAliasedImportsWithTypePromotionIssue401Async();
    }

    private async Task FlakeyAliasedImportsWithTypePromotionIssue401Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}
1 target compilation errors:
CS8082: Sub-expression cannot be used in an argument to nameof.", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestGenericMethodGroupGainsBracketsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UsesSquareBracketsForItemIndexerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;

internal partial class TestClass
{
    public object GetItem(DataRow dr)
    {
        return dr[""col1""];
    }
}", extension: "cs")
            );
        }
    }
}