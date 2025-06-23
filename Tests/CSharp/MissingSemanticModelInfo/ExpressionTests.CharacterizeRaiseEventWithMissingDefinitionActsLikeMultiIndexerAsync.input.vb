Imports System

    Friend Class TestClass
        Private Sub TestMethod()
            If MyEvent IsNot Nothing Then MyEvent(Me, EventArgs.Empty)
        End Sub
    End Class