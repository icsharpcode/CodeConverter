Public Class TestClass
    Shared Function TimeAgo(daysAgo As Integer) As String
        Select Case daysAgo
            Case 0 To 3, 4, Is >= 5, Is < 6, Is <= 7
                Return "this week"
            Case Is > 0
                Return daysAgo \ 7 & " weeks ago"
            Case Else
                Return "in the future"
        End Select
    End Function
End Class