Class TestClass
    Private Sub TestMethod()
        Call (Sub() Console.Write("Hello"))
        Call (Sub() Console.Write("Hello"))()
        Call TestMethod
        Call TestMethod()
    End Sub
End Class