Module Main
    Public Enum EWhere As Short
        None = 0
        Bottom = 1
    End Enum

    Friend Function prtWhere(ByVal aWhere As EWhere) As String
        Select Case aWhere
            Case EWhere.None
                Return " "
            Case EWhere.Bottom
                Return "_ "
        End Select

    End Function
End Module