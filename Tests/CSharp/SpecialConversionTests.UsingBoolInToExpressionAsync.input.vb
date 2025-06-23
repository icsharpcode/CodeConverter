Public Class C
    Public Sub M(OldWords As String(), NewWords As String(), HTMLCode As String)
        For i As Integer = 0 To i < OldWords.Length - 1
            HTMLCode = HTMLCode.Replace(OldWords(i), NewWords(i))
        Next i
    End Sub
End Class