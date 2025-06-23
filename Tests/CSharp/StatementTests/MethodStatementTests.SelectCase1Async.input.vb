Class TestClass
    Private Sub TestMethod(ByVal number As Integer)
        Select Case number
            Case 0, 1, 2
                Console.Write("number is 0, 1, 2")
            Case 5
                Console.Write("section 5")
            Case Else
                Console.Write("default section")
        End Select
    End Sub
End Class