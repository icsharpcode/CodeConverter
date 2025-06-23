
Public Class TestClass
    Public Sub M(a As String)
    End Sub
    Public Sub M(Optional a As String = "ss", Optional b as String = "smth")
    End Sub
    
    Public Sub Test()
        M(,"x")
    End Sub
End Class
