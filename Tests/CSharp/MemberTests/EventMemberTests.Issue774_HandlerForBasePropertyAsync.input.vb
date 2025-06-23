Imports System
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices

Partial Class BaseForm
    Inherits Form
    Friend WithEvents BaseButton As Button
End Class

<DesignerGenerated>
Partial Class BaseForm
    Inherits System.Windows.Forms.Form

    Private Sub InitializeComponent()
        Me.BaseButton = New Button()
    End Sub
End Class

<DesignerGenerated>
Partial Class Form1
    Inherits BaseForm
    Private Sub InitializeComponent()
        Me.Button1 = New Button()
    End Sub
    Friend WithEvents Button1 As Button
End Class

Partial Class Form1
    Private Sub MultiClickHandler(sender As Object, e As EventArgs) Handles Button1.Click,
                                                                            BaseButton.Click
    End Sub
End Class