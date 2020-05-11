using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    public class SpecialConversionTests : ConverterTestBase
    {
        [Fact]
        public async Task RaiseEventAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private event EventHandler MyEvent;

    private void TestMethod()
    {
        MyEvent?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact]
        public async Task TestCustomEventAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Class TestClass45
    Private Event backingField As EventHandler

    Public Custom Event MyEvent As EventHandler
        AddHandler(ByVal value As EventHandler)
            AddHandler Me.backingField, value
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
            RemoveHandler Me.backingField, value
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
            Console.WriteLine(""Event Raised"")
        End RaiseEvent
    End Event ' RaiseEvent moves outside this block

    Public Sub RaiseCustomEvent()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class", @"using System;

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
}");
        }

        [Fact]
        public async Task TestFullWidthCharacterCustomEventAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Ｃｌａｓｓ　ＴｅｓｔＣｌａｓｓ４５
　　　　Ｐｒｉｖａｔｅ　Ｅｖｅｎｔ　ｂａｃｋｉｎｇＦｉｅｌｄ　Ａｓ　EventHandler

　　　　Ｐｕｂｌｉｃ　Ｃｕｓｔｏｍ　Ｅｖｅｎｔ　ＭｙＥｖｅｎｔ　Ａｓ　EventHandler
　　　　　　　　ＡｄｄＨａｎｄｌｅｒ（ＢｙＶａｌ　ｖａｌｕｅ　Ａｓ　EventHandler）
　　　　　　　　　　　　ＡｄｄＨａｎｄｌｅｒ　Ｍｅ．ｂａｃｋｉｎｇＦｉｅｌｄ，　ｖａｌｕｅ
　　　　　　　　Ｅｎｄ　ＡｄｄＨａｎｄｌｅｒ
　　　　　　　　ＲｅｍｏｖｅＨａｎｄｌｅｒ（ＢｙＶａｌ　ｖａｌｕｅ　Ａｓ　EventHandler）
　　　　　　　　　　　　ＲｅｍｏｖｅＨａｎｄｌｅｒ　Ｍｅ．ｂａｃｋｉｎｇＦｉｅｌｄ，　ｖａｌｕｅ
　　　　　　　　Ｅｎｄ　ＲｅｍｏｖｅＨａｎｄｌｅｒ
　　　　　　　　ＲａｉｓｅＥｖｅｎｔ（ＢｙＶａｌ　ｓｅｎｄｅｒ　Ａｓ　Ｏｂｊｅｃｔ，　ＢｙＶａｌ　ｅ　Ａｓ　System.EventArgs）
　　　　　　　　　　　　Console．WriteLine（”Ｅｖｅｎｔ　Ｒａｉｓｅｄ”）
　　　　　　　　Ｅｎｄ　ＲａｉｓｅＥｖｅｎｔ
　　　　Ｅｎｄ　Ｅｖｅｎｔ　’　ＲａｉｓｅＥｖｅｎｔ　ｍｏｖｅｓ　ｏｕｔｓｉｄｅ　ｔｈｉｓ　ｂｌｏｃｋ 'Workaround test code not noticing ’ symbol

　　　　Ｐｕｂｌｉｃ　Ｓｕｂ　ＲａｉｓｅＣｕｓｔｏｍＥｖｅｎｔ（）
　　　　　　　　ＲａｉｓｅＥｖｅｎｔ　ＭｙＥｖｅｎｔ（Ｍｅ，　EventArgs.Empty）
　　　　Ｅｎｄ　Ｓｕｂ
Ｅｎｄ　Ｃｌａｓｓ", @"using System;

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
    }　// ＲａｉｓｅＥｖｅｎｔ　ｍｏｖｅｓ　ｏｕｔｓｉｄｅ　ｔｈｉｓ　ｂｌｏｃｋ 'Workaround test code not noticing ’ symbol

    void OnＭｙＥｖｅｎｔ(object ｓｅｎｄｅｒ, EventArgs ｅ)
    {
        Console.WriteLine(""Ｅｖｅｎｔ　Ｒａｉｓｅｄ"");
    }

    public void ＲａｉｓｅＣｕｓｔｏｍＥｖｅｎｔ()
    {
        OnＭｙＥｖｅｎｔ(this, EventArgs.Empty);
    }
}");
        }

        [Fact]
        public async Task HexAndBinaryLiteralsAsync()
        {
        await TestConversionVisualBasicToCSharpAsync(
        @"Class Test
    Public CR As Integer = &HD * &B1
End Class", @"
internal partial class Test
{
    public int CR = 0xD * 0b1;
}");
        }

        [Fact]
        public async Task Issue483_HexAndBinaryLiteralsAsync()
        {
        await TestConversionVisualBasicToCSharpAsync(
        @"Public Class Issue483
    Public Test1 as Integer = &H7A
    Public Test2 as Integer = &H7B
    Public Test3 as Integer = &H7C
    Public Test4 as Integer = &H7D
    Public Test5 as Integer = &H7E
    Public Test6 as Integer = &H7F
End Class", @"
public partial class Issue483
{
    public int Test1 = 0x7A;
    public int Test2 = 0x7B;
    public int Test3 = 0x7C;
    public int Test4 = 0x7D;
    public int Test5 = 0x7E;
    public int Test6 = 0x7F;
}");
        }

        [Fact]
        public async Task Issue544_AssignUsingMidAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
        @"Public Class Issue483
    Private Function numstr(ByVal aDouble As Double) As String
        Dim str_Txt As String = Format(aDouble, ""0.000000"")
        Mid(str_Txt, Len(str_Txt) - 6, 1) = "".""
        Mid(str_Txt, Len(str_Txt) - 6) = "".""
        Mid(str_Txt, Len(str_Txt) - 6) = aDouble
        Console.WriteLine(aDouble)
        If aDouble > 5.0 Then Mid(str_Txt, Len(str_Txt) - 6) = numstr(aDouble - 1.0)
        Return str_Txt
    End Function
End Class", @"using System;
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
        if (aDouble > 5.0)
        {
            var midTmp = numstr(aDouble - 1.0);
            StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, midTmp.Length, midTmp);
        }

        return str_Txt;
    }
}");
        }

        [Fact]
        public async Task TestConstCharacterConversionsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data

Class TestConstCharacterConversions
    Function GetItem(dr As DataRow) As Object
        Const a As String = Chr(7)
        Const b As String = ChrW(8)
        Const t As String = Chr(9)
        Const n As String = ChrW(10)
        Const v As String = Chr(11)
        Const f As String = ChrW(12)
        Const r As String = Chr(13)
        Const x As String = Chr(14)
        Const 字 As String = ChrW(&H5B57)
   End Function
End Class", @"using System.Data;

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
}");
        }
    }
}
