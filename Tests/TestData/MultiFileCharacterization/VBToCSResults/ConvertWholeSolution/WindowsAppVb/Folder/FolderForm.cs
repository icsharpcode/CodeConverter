using System;

namespace WindowsAppVb
{
    public partial class FolderForm
    {
        public FolderForm()
        {
            InitializeComponent();
        }

        private void FolderForm_Load(object sender, EventArgs e)
        {
            ToolStripButton7.Image = My.Resources.Resource2.test;
            ToolStripButton8.Image = My.Resources.Resource2.test2;
            ToolStripButton9.Image = My.Resources.Resource2.test3;
            ToolStripButton10.Image = My.Resources.Resource2.test;
            ToolStripButton11.Image = My.Resources.Resource2.test2;
            ToolStripButton12.Image = My.Resources.Resource2.test3;
        }
    }
}