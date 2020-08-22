using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using VbNetStandardLib.My.Resources;

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
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            ToolStripButton7.Image = My.Resources.Resource2.test;
            ToolStripButton8.Image = My.Resources.Resource2.test2;
            ToolStripButton9.Image = My.Resources.Resource2.test3;
            ToolStripButton10.Image = My.Resources.Resource2.test;
            ToolStripButton11.Image = My.Resources.Resource2.test2;
            ToolStripButton12.Image = My.Resources.Resource2.test3;
            ToolStripButton13.Image = GetImage(RootResources.test);
            ToolStripButton13.Text = RootResources.Res1 + RootResources.Res2 + RootResources.String1;
            ToolStripButton14.Image = GetImage(FolderRes.test);
            ToolStripButton14.Text = FolderRes.Res1 + FolderRes.Res2 + FolderRes.String1;
            ToolStripButton15.Image = GetImage(Folder2Res.test);
            ToolStripButton15.Text = Folder2Res.Res1 + Folder2Res.Res2 + Folder2Res.String1;
        }

        private Bitmap GetImage(byte[] a)
        {
            using (var ms = new MemoryStream(a))
            {
                return new Bitmap(ms);
            }
        }
    }
}