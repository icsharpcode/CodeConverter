Class TestClass
    Private Sub TestMethod()
        Const value1 = "something"
        Dim xElement = <Elem1 Attr1=<%= value1 %> Attr2=<%= 100 %>></Elem1>
    End Sub
End Class