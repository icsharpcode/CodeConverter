using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class CaseSensitivityTests : ConverterTestBase
{
    [Fact]
    public async Task HandlesWithDifferingCaseTestAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class VBIsCaseInsensitive
    Inherits System.Web.UI.Page

    Private Sub btnOK_Click(sender As Object, e As System.EventArgs) Handles btnOK.Click
    End Sub
End Class

Partial Public Class VBIsCaseInsensitive
    Protected WithEvents btnOk As Global.System.Web.UI.WebControls.Button
End Class
",
            @"using System;
using System.Runtime.CompilerServices;

public partial class VBIsCaseInsensitive : System.Web.UI.Page
{

    private void btnOK_Click(object sender, EventArgs e)
    {
    }
}

public partial class VBIsCaseInsensitive
{
    private System.Web.UI.WebControls.Button _btnOk;

    protected virtual System.Web.UI.WebControls.Button btnOk
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _btnOk;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_btnOk != null)
            {
                _btnOk.Click -= btnOK_Click;
            }

            _btnOk = value;
            if (_btnOk != null)
            {
                _btnOk.Click += btnOK_Click;
            }
        }
    }
}");
    }



}