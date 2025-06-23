using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class CaseSensitivityTests : ConverterTestBase
{
    [Fact]
    public async Task HandlesWithDifferingCaseTestAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class VBIsCaseInsensitive
    Inherits System.Web.UI.Page

    Private Sub btnOK_Click(sender As Object, e As System.EventArgs) Handles btnOK.Click
    End Sub
End Class

Partial Public Class VBIsCaseInsensitive
    Protected WithEvents btnOk As Global.System.Web.UI.WebControls.Button
End Class
", extension: "vb"),
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue1154_NamespaceAndClassSameNameDifferentCaseAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
Imports System

Namespace Issue1154
    <CaseSensitive1.Casesensitive1.TestDummy>
    Public Class UpperLowerCase
    End Class

    <Casesensitive2.CaseSensitive2.TestDummy>
    Public Class LowerUpperCase
    End Class

    <CaseSensitive3.CaseSensitive3.TestDummy>
    Public Class SameCase
    End Class
End Namespace

Namespace CaseSensitive1
    Public Class Casesensitive1
        Public Class TestDummyAttribute
            Inherits Attribute
        End Class
    End Class
End Namespace

Namespace Casesensitive2
    Public Class CaseSensitive2
        Public Class TestDummyAttribute
            Inherits Attribute
        End Class
    End Class
End Namespace

Namespace CaseSensitive3
    Public Class CaseSensitive3
        Public Class TestDummyAttribute
            Inherits Attribute
        End Class
    End Class
End Namespace
", extension: "vb"),
                Verifier.Verify(@"
using System;

namespace Issue1154
{
    [CaseSensitive1.Casesensitive1.TestDummy]
    public partial class UpperLowerCase
    {
    }

    [Casesensitive2.CaseSensitive2.TestDummy]
    public partial class LowerUpperCase
    {
    }

    [CaseSensitive3.CaseSensitive3.TestDummy]
    public partial class SameCase
    {
    }
}

namespace CaseSensitive1
{
    public partial class Casesensitive1
    {
        public partial class TestDummyAttribute : Attribute
        {
        }
    }
}

namespace Casesensitive2
{
    public partial class CaseSensitive2
    {
        public partial class TestDummyAttribute : Attribute
        {
        }
    }
}

namespace CaseSensitive3
{
    public partial class CaseSensitive3
    {
        public partial class TestDummyAttribute : Attribute
        {
        }
    }
}
", extension: "cs")
            );
        }
    }



}