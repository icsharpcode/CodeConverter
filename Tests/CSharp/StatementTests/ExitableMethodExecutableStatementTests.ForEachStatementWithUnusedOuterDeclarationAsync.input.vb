Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim val As Integer
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class