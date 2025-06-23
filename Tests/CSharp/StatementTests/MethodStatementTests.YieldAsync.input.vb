Class TestClass
    Private Iterator Function TestMethod(ByVal number As Integer) As IEnumerable(Of Integer)
        If number < 0 Then Return
        If number < 1 Then Exit Function
        For i As Integer = 0 To number - 1
            Yield i
        Next
        Return
    End Function
End Class