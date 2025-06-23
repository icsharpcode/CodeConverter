Class TestClass
    Private Sub TestMethod()
        Dim x As Integer? = Nothing
        Dim y As Integer? = Nothing
        Dim a As Integer = 0

        Dim res As Boolean? = x = y
        res = x <> y
        res = x > y
        res = x >= y
        res = x < y
        res = x <= y

        res = a = y
        res = a <> y
        res = a > y
        res = a >= y
        res = a < y
        res = a <= y

        res = x = a
        res = x <> a
        res = x > a
        res = x >= a
        res = x < a
        res = x <= a
    End Sub
End Class