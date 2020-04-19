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
    }
}
