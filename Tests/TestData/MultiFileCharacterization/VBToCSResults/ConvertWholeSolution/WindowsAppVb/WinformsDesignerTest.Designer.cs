using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
                if (disposing && components is object)
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
            _Button1.Click += new EventHandler(Button1_Click);
            _Button1.Click += new EventHandler(CheckedChangedOrButtonClicked);
            _Button1.MouseClick += new MouseEventHandler(ButtonMouseClickWithNoArgs);
            _Button1.MouseClick += new MouseEventHandler(ButtonMouseClickWithNoArgs2); // In C#, need to assign to field (not property), and bind event manually to ensure Winforms designer renders
            _CheckBox1 = new CheckBox();
            _CheckBox1.CheckedChanged += new EventHandler(CheckedChangedOrButtonClicked);
            _CheckBox1.CheckedChanged += new EventHandler(ButtonMouseClickWithNoArgs2);
            _Button2 = new Button();
            _Button2.MouseClick += new MouseEventHandler(ButtonMouseClickWithNoArgs);
            _Button2.Click += new EventHandler(Button2_Click);
            SuspendLayout();
            // 
            // Button1
            // 
            _Button1.Location = new Point(95, 80);
            _Button1.Name = "_Button1";
            _Button1.Size = new Size(75, 23);
            _Button1.TabIndex = 0;
            _Button1.Text = "Button1";
            _Button1.UseVisualStyleBackColor = true;
            // 
            // CheckBox1
            // 
            _CheckBox1.AutoSize = true;
            _CheckBox1.Location = new Point(89, 28);
            _CheckBox1.Name = "_CheckBox1";
            _CheckBox1.Size = new Size(81, 17);
            _CheckBox1.TabIndex = 1;
            _CheckBox1.Text = "CheckBox1";
            _CheckBox1.UseVisualStyleBackColor = true;
            // 
            // Button2
            // 
            _Button2.Location = new Point(95, 110);
            _Button2.Name = "_Button2";
            _Button2.Size = new Size(75, 23);
            _Button2.TabIndex = 2;
            _Button2.Text = "Show resources";
            _Button2.UseVisualStyleBackColor = true;
            // 
            // WinformsDesignerTest
            // 
            AutoScaleDimensions = new SizeF(6.0F, 13.0F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(292, 273);
            Controls.Add(_Button2);
            Controls.Add(_CheckBox1);
            Controls.Add(_Button1);
            Name = "WinformsDesignerTest";
            Text = "Form1";
            Load += new EventHandler(WinformsDesignerTest_EnsureSelfEventsWork);
            SizeChanged += new EventHandler(WinformsDesignerTest_EnsureSelfEventsWork);
            MouseClick += new MouseEventHandler(WinformsDesignerTest_MouseClick);
            ResumeLayout(false);
            PerformLayout();
        }

        public static bool TestSub([Optional, DefaultParameterValue(false)] ref bool IsDefault)
        {
            return default;
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
                    _Button1.MouseClick -= (_, __) => ButtonMouseClickWithNoArgs();
                    _Button1.MouseClick -= (_, __) => ButtonMouseClickWithNoArgs2();
                }

                _Button1 = value;
                if (_Button1 != null)
                {
                    _Button1.Click += Button1_Click;
                    _Button1.Click += CheckedChangedOrButtonClicked;
                    _Button1.MouseClick += (_, __) => ButtonMouseClickWithNoArgs();
                    _Button1.MouseClick += (_, __) => ButtonMouseClickWithNoArgs2();
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
                    _CheckBox1.CheckedChanged -= (_, __) => ButtonMouseClickWithNoArgs2();
                }

                _CheckBox1 = value;
                if (_CheckBox1 != null)
                {
                    _CheckBox1.CheckedChanged += CheckedChangedOrButtonClicked;
                    _CheckBox1.CheckedChanged += (_, __) => ButtonMouseClickWithNoArgs2();
                }
            }
        }

        private Button _Button2;

        internal Button Button2
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
                    _Button2.MouseClick -= (_, __) => ButtonMouseClickWithNoArgs();
                    _Button2.Click -= Button2_Click;
                }

                _Button2 = value;
                if (_Button2 != null)
                {
                    _Button2.MouseClick += (_, __) => ButtonMouseClickWithNoArgs();
                    _Button2.Click += Button2_Click;
                }
            }
        }
    }
}