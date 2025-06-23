Class TestClass
    Private Function TestMethod() As String()
        Dim s = "1,2"
        Return s.Split(s(1))
    End Function
End Class