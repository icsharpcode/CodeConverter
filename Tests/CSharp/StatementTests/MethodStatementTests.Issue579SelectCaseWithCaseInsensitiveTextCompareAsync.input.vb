
Option Compare Text ' Comments lost

Class Issue579SelectCaseWithCaseInsensitiveTextCompare
Private Function Test(astr_Temp As String) As Nullable(Of Boolean)
    Select Case astr_Temp
        Case "Test"
            Return True
        Case astr_Temp
            Return False
        Case Else
            Return Nothing
    End Select
End Function
End Class