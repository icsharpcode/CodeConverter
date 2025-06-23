using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class BaseForm : Form
{
    private Button _BaseButton;

    internal virtual Button BaseButton
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _BaseButton;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            _BaseButton = value;
        }
    }

    public BaseForm()
    {
        InitializeComponent();
        BaseButton = _BaseButton;
    }
}

[DesignerGenerated]
internal partial class BaseForm : Form
{

    private void InitializeComponent()
    {
        _BaseButton = new Button();
    }
}

[DesignerGenerated]
internal partial class Form1 : BaseForm
{
    internal override Button BaseButton
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return base.BaseButton;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (base.BaseButton != null)
            {
                base.BaseButton.Click -= MultiClickHandler;
            }

            base.BaseButton = value;
            if (base.BaseButton != null)
            {
                base.BaseButton.Click += MultiClickHandler;
            }
        }
    }

    public Form1()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        Button1 = new Button();
        Button1.Click += new EventHandler(MultiClickHandler);
    }
    internal Button Button1;
}

internal partial class Form1
{
    private void MultiClickHandler(object sender, EventArgs e)
    {
    }
}
