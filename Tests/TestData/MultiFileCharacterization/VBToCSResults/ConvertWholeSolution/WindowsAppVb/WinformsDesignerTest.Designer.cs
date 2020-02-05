using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace WindowsAppVb
{
    [Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class WinformsDesignerTest : Form
    {

        // Form overrides dispose to clean up the component list.
        [DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components != null)
                {
                    components.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;

        // NOTE: The following procedure is required by the Windows Form Designer
        // It can be modified using the Windows Form Designer.
        // Do not modify it using the code editor.
        [DebuggerStepThrough()]
        private void InitializeComponent()
        {
            _Button1 = new Button();
            _Button1.Click += Button1_Click;
            _Button1.Click += CheckedChangedOrButtonClicked; // In C#, need to assign to field (not property), and bind event manually to ensure Winforms designer renders
            _CheckBox1 = new CheckBox();
            _CheckBox1.CheckedChanged += CheckedChangedOrButtonClicked;
            SuspendLayout();
            // 
            // Button1
            // 
            _Button1.Location = new Point(95, 80);
            _Button1.Name = "Button1";
            _Button1.Size = new Size(75, 23);
            _Button1.TabIndex = 0;
            _Button1.Text = "Button1";
            _Button1.UseVisualStyleBackColor = true;
            // 
            // CheckBox1
            // 
            _CheckBox1.AutoSize = true;
            _CheckBox1.Location = new Point(89, 28);
            _CheckBox1.Name = "CheckBox1";
            _CheckBox1.Size = new Size(81, 17);
            _CheckBox1.TabIndex = 1;
            _CheckBox1.Text = "CheckBox1";
            _CheckBox1.UseVisualStyleBackColor = true;
            // 
            // WinformsDesignerTest
            // 
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(292, 273);
            Controls.Add(_CheckBox1);
            Controls.Add(_Button1);
            Name = "WinformsDesignerTest";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        public static bool TestSub(ref bool IsDefault = false)
        {
            return default(bool);
        }

        private Button _Button1;

        internal Button Button1
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
                    _Button1.Click -= Button1_Click;
                    _Button1.Click -= CheckedChangedOrButtonClicked;
                }

                _Button1 = value;
                if (_Button1 != null)
                {
                    _Button1.Click += Button1_Click;
                    _Button1.Click += CheckedChangedOrButtonClicked;
                }
            }
        }

        private CheckBox _CheckBox1;

        internal CheckBox CheckBox1
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _CheckBox1;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_CheckBox1 != null)
                {
                    _CheckBox1.CheckedChanged -= CheckedChangedOrButtonClicked;
                }

                _CheckBox1 = value;
                if (_CheckBox1 != null)
                {
                    _CheckBox1.CheckedChanged += CheckedChangedOrButtonClicked;
                }
            }
        }
    }
}