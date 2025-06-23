Class TestClass
    Private Sub TestMethod()
        Dim test = Function(a) a * 2
        Dim test2 = Function(a, b)
            If b > 0 Then Return a / b
            Return 0
        End Function

        Dim test3 = Function(a, b) a Mod b
        test(3)
    End Sub
End Class