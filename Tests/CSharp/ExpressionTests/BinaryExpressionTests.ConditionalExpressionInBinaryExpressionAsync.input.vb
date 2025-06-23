Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim result As Integer = 5 - If((str = ""), 1, 2)
    End Sub
End Class