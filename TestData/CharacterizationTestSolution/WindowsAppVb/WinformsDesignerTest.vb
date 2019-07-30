Public Class WinformsDesignerTest
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged, Button1.Click

    End Sub

    Private Sub WinformsDesignerTest_EnsureSelfEventsWork(sender As Object, e As EventArgs) Handles MyBase.Load, Me.SizeChanged

    End Sub
End Class
