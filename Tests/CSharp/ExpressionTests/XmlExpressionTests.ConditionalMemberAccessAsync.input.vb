Class TestClass
    Private Sub TestMethod()       
        Dim xDocument = <Test></Test>
        Dim elements1 = xDocument.<Something>.SingleOrDefault()?.<SomethingElse>
    End Sub
End Class