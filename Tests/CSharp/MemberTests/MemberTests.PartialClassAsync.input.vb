Public Partial Class TestClass
    Private Sub DoNothing()
        Console.WriteLine("Hello")
    End Sub
End Class

Class TestClass ' VB doesn't require partial here (when just a single class omits it)
    Partial Private Sub DoNothing()
    End Sub
End Class