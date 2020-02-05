using System;

namespace WindowsAppVb
{
    public partial class WinformsDesignerTest
    {
        public WinformsDesignerTest()
        {
            base.Load += WinformsDesignerTest_EnsureSelfEventsWork;
            this.SizeChanged += WinformsDesignerTest_EnsureSelfEventsWork;
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
        }

        private void CheckedChangedOrButtonClicked(object sender, EventArgs e)
        {
            string formConstructedText = "Form constructed";
            if (!(My.MyProject.Forms.m_WinformsDesignerTest == null) && (My.MyProject.Forms.WinformsDesignerTest.Text ?? "") != (formConstructedText ?? ""))
            {
                My.MyProject.Forms.WinformsDesignerTest.Text = formConstructedText;
            }
            else if (My.MyProject.Forms.m_WinformsDesignerTest != null && My.MyProject.Forms.m_WinformsDesignerTest != null)
            {
                My.MyProject.Forms.WinformsDesignerTest = null;
            }
        }

        private void WinformsDesignerTest_EnsureSelfEventsWork(object sender, EventArgs e)
        {
        }
    }
}