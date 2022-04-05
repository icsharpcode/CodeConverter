using System;
using System.Diagnostics;
using System.Drawing;
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
                if (disposing && components is not null)
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
            Button1 = new Button();
            Button1.Click += new EventHandler(Button1_Click);
            Button1.Click += new EventHandler(CheckedChangedOrButtonClicked);
            Button1.MouseClick += new MouseEventHandler(ButtonMouseClickWithNoArgs);
            Button1.MouseClick += new MouseEventHandler(ButtonMouseClickWithNoArgs2); // In C#, need to assign to field (not property), and bind event manually to ensure Winforms designer renders
            CheckBox1 = new CheckBox();
            CheckBox1.CheckedChanged += new EventHandler(CheckedChangedOrButtonClicked);
            CheckBox1.CheckedChanged += new EventHandler(ButtonMouseClickWithNoArgs2);
            Button2 = new Button();
            Button2.MouseClick += new MouseEventHandler(ButtonMouseClickWithNoArgs);
            Button2.Click += new EventHandler(Button2_Click);
            DataGridView1 = new DataGridView();
            Column1 = new DataGridViewTextBoxColumn();
            ColumnWithEvent = new DataGridViewTextBoxColumn();
            ColumnWithEvent.Disposed += new EventHandler(ColumnWithEvent_Disposed);
            ((System.ComponentModel.ISupportInitialize)DataGridView1).BeginInit();
            SuspendLayout();
            // 
            // Button1
            // 
            Button1.Location = new Point(95, 80);
            Button1.Name = "Button1";
            Button1.Size = new Size(75, 23);
            Button1.TabIndex = 0;
            Button1.Text = "Button1";
            Button1.UseVisualStyleBackColor = true;
            // 
            // CheckBox1
            // 
            CheckBox1.AutoSize = true;
            CheckBox1.Location = new Point(89, 28);
            CheckBox1.Name = "CheckBox1";
            CheckBox1.Size = new Size(81, 17);
            CheckBox1.TabIndex = 1;
            CheckBox1.Text = "CheckBox1";
            CheckBox1.UseVisualStyleBackColor = true;
            // 
            // Button2
            // 
            Button2.Location = new Point(95, 110);
            Button2.Name = "Button2";
            Button2.Size = new Size(75, 23);
            Button2.TabIndex = 2;
            Button2.Text = "Show resources";
            Button2.UseVisualStyleBackColor = true;
            // 
            // DataGridView1
            // 
            DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            DataGridView1.Columns.AddRange(new DataGridViewColumn[] { Column1, ColumnWithEvent });
            DataGridView1.Location = new Point(35, 156);
            DataGridView1.Name = "DataGridView1";
            DataGridView1.Size = new Size(240, 150);
            DataGridView1.TabIndex = 3;
            // 
            // Column1
            // 
            Column1.HeaderText = "Column1";
            Column1.Name = "Column1";
            // 
            // ColumnWithEvent
            // 
            ColumnWithEvent.HeaderText = "ColumnWithEvent";
            ColumnWithEvent.Name = "ColumnWithEvent";
            // 
            // WinformsDesignerTest
            // 
            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(471, 364);
            Controls.Add(DataGridView1);
            Controls.Add(Button2);
            Controls.Add(CheckBox1);
            Controls.Add(Button1);
            Name = "WinformsDesignerTest";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)DataGridView1).EndInit();
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

        internal Button Button1;
        internal CheckBox CheckBox1;
        internal Button Button2;
        internal DataGridView DataGridView1;
        internal DataGridViewTextBoxColumn Column1;
        internal DataGridViewTextBoxColumn ColumnWithEvent;
    }
}