Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each v In values
            If v = 2 Then Continue For
            If v = 3 Then Exit For
        Next
    End Sub
End Class