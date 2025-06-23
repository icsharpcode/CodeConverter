
Class Issue707SelectCaseAsyncClass
    Private Function Exists(sort As Char?) As Boolean?
        Select Case Microsoft.VisualBasic.LCase(sort + "")
            Case "", Nothing
                Return False
            Case Else
                Return True
        End Select
    End Function
End Class