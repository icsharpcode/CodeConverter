Class TestClass
    Private Sub TestMethod()
               Dim TotalRead As Long = 1
        Dim ContentLength As Long? = 2 '(It is supposed that TotalRead < ContentLength)
        Dim percentage1 As Integer = Convert.ToInt32((TotalRead / ContentLength) * 100.0)
        Dim percentage2 As Integer = Convert.ToInt32(TotalRead / ContentLength * 100.0)
    End Sub
End Class