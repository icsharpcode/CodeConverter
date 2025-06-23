Public Class SelectObjectCaseIntegerTest
    Sub S()
        Dim o As Object
        Dim j As Integer
        o = 2.0
        Select Case o
            Case 1
                j = 1
            Case 2
                j = 2
            Case 3 To 4
                j = 3
            Case > 4
                j = 4
            Case Else
                j = -1
        End Select
    End Sub
End Class