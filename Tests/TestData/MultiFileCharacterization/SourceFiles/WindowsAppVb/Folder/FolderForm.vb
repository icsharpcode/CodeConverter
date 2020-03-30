Imports System.Globalization
Imports System.IO
Imports System.Threading
Imports VbNetStandardLib.My.Resources

Public Class FolderForm
    Private Sub FolderForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US")
        ToolStripButton7.Image = My.Resources.Resource2.test
        ToolStripButton8.Image = My.Resources.Resource2.test2
        ToolStripButton9.Image = My.Resources.Resource2.test3
        ToolStripButton10.Image = My.Resources.Resource2.test
        ToolStripButton11.Image = My.Resources.Resource2.test2
        ToolStripButton12.Image = My.Resources.Resource2.test3
        ToolStripButton13.Image = GetImage(RootResources.test)
        ToolStripButton13.Text = RootResources.Res1 + RootResources.Res2 + RootResources.String1
        ToolStripButton14.Image = GetImage(FolderRes.test)
        ToolStripButton14.Text = FolderRes.Res1 + FolderRes.Res2 + FolderRes.String1
        ToolStripButton15.Image = GetImage(Folder2Res.test)
        ToolStripButton15.Text = Folder2Res.Res1 + Folder2Res.Res2 + Folder2Res.String1
    End Sub

    Private Function GetImage(a As Byte()) As Bitmap
        Using ms = New MemoryStream(a)
            Return New Bitmap(ms)
        End Using
    End Function
End Class