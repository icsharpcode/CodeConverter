Class TestClass
    Private Sub TestMethod()
        Dim x As Integer? = Nothing
        Dim y As Integer? = Nothing
        Dim a As Integer = 0

        If x = y Then Return
        If x <> y Then Return
        If x > y Then Return
        If x >= y Then Return
        If x < y Then Return
        If x <= y Then Return

        If a = y Then Return
        If a <> y Then Return
        If a > y Then Return
        If a >= y Then Return
        If a < y Then Return
        If a <= y Then Return

        IF x = a Then Return
        IF x <> a Then Return
        IF x > a Then Return
        IF x >= a Then Return
        IF x < a Then Return
        IF x <= a Then Return
    End Sub
End Class