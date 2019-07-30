using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace WindowsAppVb
{
    [global::Microsoft.VisualBasic.CompilerServices.DesignerGenerated()]
    public partial class WinformsDesignerTest : System.Windows.Forms.Form
    {

        // Form overrides dispose to clean up the component list.
        [System.Diagnostics.DebuggerNonUserCode()]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && components != null)
                    components.Dispose();
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
        [System.Diagnostics.DebuggerStepThrough()]
        private void InitializeComponent()
        {
            this._Button1 = new System.Windows.Forms.Button();
            _Button1.Click += Button1_Click;
            _Button1.Click += CheckBox1_CheckedChanged; // In C#, need to assign to field (not property), and bind event manually to ensure Winforms designer renders
            this._CheckBox1 = new System.Windows.Forms.CheckBox();
            _CheckBox1.CheckedChanged += CheckBox1_CheckedChanged;
            this.SuspendLayout();
            // 
            // Button1
            // 
            this._Button1.Location = new System.Drawing.Point(95, 80);
            this._Button1.Name = "Button1";
            this._Button1.Size = new System.Drawing.Size(75, 23);
            this._Button1.TabIndex = 0;
            this._Button1.Text = "Button1";
            this._Button1.UseVisualStyleBackColor = true;
            // 
            // CheckBox1
            // 
            this._CheckBox1.AutoSize = true;
            this._CheckBox1.Location = new System.Drawing.Point(89, 28);
            this._CheckBox1.Name = "CheckBox1";
            this._CheckBox1.Size = new System.Drawing.Size(81, 17);
            this._CheckBox1.TabIndex = 1;
            this._CheckBox1.Text = "CheckBox1";
            this._CheckBox1.UseVisualStyleBackColor = true;
            // 
            // WinformsDesignerTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Controls.Add(this._CheckBox1);
            this.Controls.Add(this._Button1);
            this.Name = "WinformsDesignerTest";
            this.Text = "Form1";
            base.Load += WinformsDesignerTest_EnsureSelfEventsWork;
            this.SizeChanged += WinformsDesignerTest_EnsureSelfEventsWork;
            this.ResumeLayout(false);
            this.PerformLayout();
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
                    _Button1.Click -= CheckBox1_CheckedChanged;
                }

                _Button1 = value;
                if (_Button1 != null)
                {
                    _Button1.Click += Button1_Click;
                    _Button1.Click += CheckBox1_CheckedChanged;
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
                    _CheckBox1.CheckedChanged -= CheckBox1_CheckedChanged;
                }

                _CheckBox1 = value;
                if (_CheckBox1 != null)
                {
                    _CheckBox1.CheckedChanged += CheckBox1_CheckedChanged;
                }
            }
        }
    }
}
