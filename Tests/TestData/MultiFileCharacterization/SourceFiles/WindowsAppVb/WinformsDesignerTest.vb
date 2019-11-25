Public Class WinformsDesignerTest
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    End Sub

    Private Sub CheckedChangedOrButtonClicked(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged, Button1.Click
        Dim formConstructedText  = "Form constructed"
        If Not(My.Forms.WinformsDesignerTest Is Nothing) AndAlso My.Forms.WinformsDesignerTest.Text <> formConstructedText Then
            My.Forms.WinformsDesignerTest.Text = formConstructedText
        Else If My.Forms.WinformsDesignerTest IsNot Nothing AndAlso DirectCast(My.Forms.WinformsDesignerTest, WinformsDesignerTest) IsNot Nothing
            My.Forms.WinformsDesignerTest = Nothing
        End If
    End Sub

    Private Sub WinformsDesignerTest_EnsureSelfEventsWork(sender As Object, e As EventArgs) Handles MyBase.Load, Me.SizeChanged

    End Sub
End Class
