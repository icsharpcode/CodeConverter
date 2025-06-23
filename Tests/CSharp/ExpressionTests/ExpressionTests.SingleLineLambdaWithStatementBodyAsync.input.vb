Class TestClass
    Private Sub TestMethod()
        Dim x = 1
        Dim simpleAssignmentAction As System.Action = Sub() x = 1
        Dim nonBlockAction As System.Action = Sub() Console.WriteLine("Statement")
        Dim ifAction As Action = Sub() If True Then Exit Sub
    End Sub
End Class