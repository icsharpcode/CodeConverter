using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class SpecialConversionTests : ConverterTestBase
{
    [Fact]
    public async Task RaiseEventAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private event EventHandler MyEvent;

    private void TestMethod()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestCustomEventAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class TestClass45
{
    private event EventHandler backingField;

    public event EventHandler MyEvent
    {
        add
        {
            backingField += value;
        }
        remove
        {
            backingField -= value;
        }
    } // RaiseEvent moves outside this block
    void OnMyEvent(object sender, EventArgs e)
    {
        Console.WriteLine(""Event Raised"");
    }

    public void RaiseCustomEvent()
    {
        OnMyEvent(this, EventArgs.Empty);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestFullWidthCharacterCustomEventAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial class ＴｅｓｔＣｌａｓｓ４５
{
    private event EventHandler ｂａｃｋｉｎｇＦｉｅｌｄ;

    public event EventHandler ＭｙＥｖｅｎｔ
    {
        add
        {
            ｂａｃｋｉｎｇＦｉｅｌｄ += value;
        }
        remove
        {
            ｂａｃｋｉｎｇＦｉｅｌｄ -= value;
        }
    } // ＲａｉｓｅＥｖｅｎｔ　ｍｏｖｅｓ　ｏｕｔｓｉｄｅ　ｔｈｉｓ　ｂｌｏｃｋ 'Workaround test code not noticing ’ symbol
    void OnＭｙＥｖｅｎｔ(object ｓｅｎｄｅｒ, EventArgs ｅ)
    {
        Console.WriteLine(""Ｅｖｅｎｔ　Ｒａｉｓｅｄ"");
    }

    public void ＲａｉｓｅＣｕｓｔｏｍＥｖｅｎｔ()
    {
        OnＭｙＥｖｅｎｔ(this, EventArgs.Empty);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task HexAndBinaryLiteralsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class Test
{
    public int CR = 0xD * 0b1;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task HexAndBinaryLiterals754Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial class Test754
{
    private int value = unchecked((int)0x80000000);
    private int value2 = unchecked((int)0xF1234567);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue483_HexAndBinaryLiteralsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Issue483
{
    public int Test1 = 0x7A;
    public int Test2 = 0x7B;
    public int Test3 = 0x7C;
    public int Test4 = 0x7D;
    public int Test5 = 0x7E;
    public int Test6 = 0x7F;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue544_AssignUsingMidAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue483
{
    private string numstr(double aDouble)
    {
        string str_Txt = Strings.Format(aDouble, ""0.000000"");
        StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, 1, ""."");
        StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, ""."".Length, ""."");
        StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, aDouble.ToString().Length, aDouble.ToString());
        Console.WriteLine(aDouble);
        if (aDouble > 5.0d)
        {
            var midTmp = numstr(aDouble - 1.0d);
            StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, midTmp.Length, midTmp);
        }
        return str_Txt;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue1147_LargeNumericHexLiteralsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Issue1147
{
    private const uint LargeUInt = 0xFFFFFFFEU;
    private const ulong LargeULong = 0xFFFFFFFFFFFFFFFEUL;
    private const int LargeInt = unchecked((int)0xFFFFFFFE);
    private const long LargeLong = unchecked((long)0xFFFFFFFFFFFFFFFE);
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task TestConstCharacterConversionsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;

internal partial class TestConstCharacterConversions
{
    public object GetItem(DataRow dr)
    {
        const string a = ""\a"";
        const string b = ""\b"";
        const string t = ""\t"";
        const string n = ""\n"";
        const string v = ""\v"";
        const string f = ""\f"";
        const string r = ""\r"";
        const string x = ""\u000e"";
        const string 字 = ""字"";
        return default;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestNonConstCharacterConversionsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestConversions
{
    public void Test(byte b)
    {
        char x = Strings.Chr(b);
        char y = Strings.ChrW(b);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestNonVisualBasicChrMethodConversionsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestConversions
{
    public void Test()
    {
        string a;
        a = Conversions.ToString(Chr(2));
        a = Conversions.ToString(Chr(2));
        a = ""\u0002"";
        a = ""\u0002"";
        a = ""\u0002"";
    }

    public void TestW()
    {
        string a;
        a = Conversions.ToString(ChrW(2));
        a = Conversions.ToString(ChrW(2));
        a = ""\u0002"";
        a = ""\u0002"";
        a = ""\u0002"";
    }

    public char Chr(object o)
    {
        return Strings.Chr(Conversions.ToInteger(o));
    }

    public char ChrW(object o)
    {
        return Strings.ChrW(Conversions.ToInteger(o));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task UsingBoolInToExpressionAsync()
    {
        // Beware, this will never enter the loop, it's buggy input due to the "i <", but it compiles and runs, so the output should too (and do the same thing)
        {
            await Task.WhenAll(
                Verifier.Verify(@"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public void M(string[] OldWords, string[] NewWords, string HTMLCode)
    {
        for (int i = 0, loopTo = Conversions.ToInteger(i < OldWords.Length - 1); i <= loopTo; i++)
            HTMLCode = HTMLCode.Replace(OldWords[i], NewWords[i]);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task StringOperatorsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public void DummyMethod(string target)
{
    if (Operators.CompareString(target, 'Z'.ToString(), false) < 0 || Operators.CompareString(new string(new char[] { }), target, false) <= 0 || string.IsNullOrEmpty(target) || !string.IsNullOrEmpty(target) || Operators.CompareString(target, new string(new char[] { }), false) >= 0 || Operators.CompareString(target, """", false) > 0)
    {
        Console.WriteLine(""It must be one of those"");
    }
}", extension: "cs")
            );
        }
    }
}