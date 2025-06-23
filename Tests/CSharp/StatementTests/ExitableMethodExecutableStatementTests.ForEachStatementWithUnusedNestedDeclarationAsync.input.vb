Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim inline1, inline2, keep1, keep2 As Integer
        For Each inline1 In values
            For Each keep1 In values
                For Each inline2 In values
                    If inline2 = 2 Then Continue For
                    If inline2 = 3 Then Exit For
                Next
            Next
            Console.WriteLine(keep1)
        Next
    End Sub
End Class