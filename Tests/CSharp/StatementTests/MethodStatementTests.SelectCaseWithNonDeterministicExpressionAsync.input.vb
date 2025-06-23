Public Class TestClass2
    Sub DoesNotThrow()
        Dim rand As New Random
        Select Case rand.Next(8)
            Case Is < 4
            Case 4
            Case Is > 4
            Case Else
                Throw New Exception
        End Select
    End Sub
End Class