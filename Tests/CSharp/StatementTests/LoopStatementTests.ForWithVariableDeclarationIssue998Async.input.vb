Class TestClass
    Private Sub TestMethod(someCondition As Boolean)
        For j = 1 To 2
            If someCondition Then
                Dim b As Boolean
                Console.WriteLine(b)
                b = True
            End If
        Next
    End Sub
End Class