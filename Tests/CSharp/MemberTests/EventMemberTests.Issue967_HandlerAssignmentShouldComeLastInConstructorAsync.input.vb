Imports System.Windows.Forms

Public Partial Class MainWindow
    Inherits Form
    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub MainWindow_Loaded() Handles MyBase.Load
        Interaction.MsgBox("Window, loaded")
    End Sub
End Class

Public Partial Class MainWindow
    Public Sub InitializeComponent()
    End Sub
End Class
