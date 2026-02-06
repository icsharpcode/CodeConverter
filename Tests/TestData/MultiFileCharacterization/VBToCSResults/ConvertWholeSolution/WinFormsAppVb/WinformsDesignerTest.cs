using System;
using System.Windows.Forms;

namespace WinFormsAppVb
{
    public partial class WinformsDesignerTest
    {
        public WinformsDesignerTest()
        {
            InitializeComponent();
        }
        private void Button1_Click(object sender, EventArgs e)
        {

        }

        private void CheckedChangedOrButtonClicked(object sender, EventArgs e)
        {
            string formConstructedText = "Form constructed";
            if (My.MyProject.Forms.m_WinformsDesignerTest is not null && (My.MyProject.Forms.WinformsDesignerTest.Text ?? "") != (formConstructedText ?? ""))
            {
                My.MyProject.Forms.WinformsDesignerTest.Text = formConstructedText;
            }
            else if (My.MyProject.Forms.m_WinformsDesignerTest is not null && My.MyProject.Forms.m_WinformsDesignerTest is not null)
            {
                My.MyProject.Forms.WinformsDesignerTest = null;
            }
        }

        private void WinformsDesignerTest_EnsureSelfEventsWork(object sender, EventArgs e)
        {

        }

        private void WinformsDesignerTest_MouseClick()
        {

        }

        private void ButtonMouseClickWithNoArgs()
        {

        }

        private void ButtonMouseClickWithNoArgs2()
        {

        }

        public void Init()
        {
            MouseEventHandler noArgs = (_, __) => WinformsDesignerTest_MouseClick();
            MouseClick += noArgs;
            MouseClick += (_, __) => WinformsDesignerTest_MouseClick();
            MouseClick -= noArgs;
            MouseClick -= (_, __) => WinformsDesignerTest_MouseClick(); // Generates a VB warning because it has no effect
        }

        public void Init_Advanced(MouseEventHandler paramToHandle)
        {
            Init();
            MouseClick += paramToHandle;
            WinformsDesignerTest_MouseClick();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            My.MyProject.Forms.FolderForm.ShowDialog();
        }

        private void ColumnWithEvent_Disposed(object sender, EventArgs e)
        {
            // Just here so that the column is turned into a property
        }

        private void WinformsDesignerTest_MouseClick(object sender, MouseEventArgs e) => WinformsDesignerTest_MouseClick();
        private void ButtonMouseClickWithNoArgs(object sender, MouseEventArgs e) => ButtonMouseClickWithNoArgs();
        private void ButtonMouseClickWithNoArgs2(object sender, MouseEventArgs e) => ButtonMouseClickWithNoArgs2();
        private void ButtonMouseClickWithNoArgs2(object sender, EventArgs e) => ButtonMouseClickWithNoArgs2();
    }
}