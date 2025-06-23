using System;
using System.Runtime.CompilerServices;

public partial class Form1
{
    private void MultiClickHandler(object sender, EventArgs e)
    {
    }
}

public partial class Form1 : System.Windows.Forms.Form
{

    private void InitializeComponent()
    {
        _Button1 = new System.Windows.Forms.Button();
        _Button1.Click += new EventHandler(MultiClickHandler);
        _Button2 = new System.Windows.Forms.Button();
        _Button2.Click += new EventHandler(MultiClickHandler);
    }

    private System.Windows.Forms.Button _Button1;

    internal virtual System.Windows.Forms.Button Button1
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _Button1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_Button1 != null)
            {
                _Button1.Click -= MultiClickHandler;
            }

            _Button1 = value;
            if (_Button1 != null)
            {
                _Button1.Click += MultiClickHandler;
            }
        }
    }
    private System.Windows.Forms.Button _Button2;

    internal virtual System.Windows.Forms.Button Button2
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        get
        {
            return _Button2;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        set
        {
            if (_Button2 != null)
            {
                _Button2.Click -= MultiClickHandler;
            }

            _Button2 = value;
            if (_Button2 != null)
            {
                _Button2.Click += MultiClickHandler;
            }
        }
    }
}