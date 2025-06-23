
Public Class TestClass
    Public Sub M(a As String, b as string)
    End Sub
    Public Sub M(Optional a As String = "1", Optional b as string = "2", Optional c as string = "3")
    End Sub

    Public Sub Test()
        M(a:="4", )
    End Sub
End Class
