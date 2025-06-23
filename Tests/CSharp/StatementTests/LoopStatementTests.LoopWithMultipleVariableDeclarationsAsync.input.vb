Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim a, b as Integer, c, d as Integer, e, f as Long, g = Sub() System.Console.WriteLine(1)
            a = 1
            b += 1
            
            c += 1
            d = 1

            e += 1
            f = 1

            g()
        Next
    End Sub
End Class