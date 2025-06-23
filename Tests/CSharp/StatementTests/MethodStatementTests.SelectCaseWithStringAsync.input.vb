Public Class TestClass
    Shared Function TimeAgo(x As String) As String
        Select Case UCase(x)
            Case UCase("a"), UCase("b")
                Return "ab"
            Case UCase("c")
                Return "c"
            Case "d"
                Return "d"
            Case Else
                Return "e"
        End Select
    End Function
End Class