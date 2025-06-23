using System;
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
}