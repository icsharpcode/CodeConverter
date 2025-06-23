Class TestClass
    Private Sub TestMethod(ByVal str As String)
        Dim length As Integer
        length = str.Length
        Console.WriteLine("Test" & length)
        Console.ReadKey()
    End Sub
End Class