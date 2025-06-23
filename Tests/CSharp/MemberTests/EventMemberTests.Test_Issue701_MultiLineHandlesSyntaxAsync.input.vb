Public Class Form1
    Private Sub MultiClickHandler(sender As Object, e As EventArgs) Handles Button1.Click,
                                                                            Button2.Click
    End Sub
End Class

Partial Class Form1
    Inherits System.Windows.Forms.Form

    Private Sub InitializeComponent()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
    End Sub

    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
End Class