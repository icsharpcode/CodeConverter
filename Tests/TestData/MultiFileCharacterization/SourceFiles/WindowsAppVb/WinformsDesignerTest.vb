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

    Private Sub WinformsDesignerTest_MouseClick() Handles Me.MouseClick

    End Sub

    Private Sub ButtonMouseClickWithNoArgs() Handles Button1.MouseClick, Button2.MouseClick

    End Sub

    Private Sub ButtonMouseClickWithNoArgs2() Handles Button1.MouseClick, CheckBox1.CheckedChanged

    End Sub

    Public Sub Init()
        Dim noArgs As MouseEventHandler = AddressOf WinformsDesignerTest_MouseClick
        AddHandler Me.MouseClick, noArgs
        AddHandler Me.MouseClick, AddressOf WinformsDesignerTest_MouseClick
        RemoveHandler Me.MouseClick, noArgs
        RemoveHandler Me.MouseClick, AddressOf WinformsDesignerTest_MouseClick ' Generates a VB warning because it has no effect
    End Sub

    Public Sub Init_Advanced(paramToHandle As MouseEventHandler)
        Init()
        AddHandler Me.MouseClick, paramToHandle
        WinformsDesignerTest_MouseClick()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        FolderForm.ShowDialog()
    End Sub
End Class
