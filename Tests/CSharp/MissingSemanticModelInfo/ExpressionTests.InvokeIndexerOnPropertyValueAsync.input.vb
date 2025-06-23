Class TestClass
    Public Property SomeProperty As System.Some.UnknownType
    Private Sub TestMethod()
        Dim num = 0
        Dim value = SomeProperty(num)
        value = SomeProperty(0)
    End Sub
End Class