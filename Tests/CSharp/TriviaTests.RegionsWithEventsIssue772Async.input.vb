Public Class VisualBasicClass
    Inherits System.Windows.Forms.Form

    #Region " Members "

        Private _Member As String = String.Empty

    #End Region

    #Region " Construction "

        Public Sub New()
        
        End Sub

    #End Region

    #Region " Methods "

        Public Sub Eventhandler_Load(sender As Object, e As EventArgs) Handles Me.Load
            'Do something
        End Sub

    #End Region

End Class