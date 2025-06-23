
Public Class TestClass
    Public Sub M(a As String)
    End Sub
    Public Sub M(a As String, Optional b as String = "smth")
    End Sub
    
    Public Sub Test()
        M("x",)
    End Sub
End Class
