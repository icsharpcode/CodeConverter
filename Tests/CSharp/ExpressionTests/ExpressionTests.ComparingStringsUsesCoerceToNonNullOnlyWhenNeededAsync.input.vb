Class TestClass
    Private Sub TestMethod(a as String)
        Dim result = a = ("")
        result = "" = a
        result = a = (String.Empty)
        result = String.Empty = a
        result = a = (Nothing)
        result = Nothing = a
        result = a Is Nothing
        result = a IsNot Nothing
        result = Not(a IsNot Nothing)
        result = a = a
        result = a = ("test")
        result = "test" = a
    End Sub
End Class