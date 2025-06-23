Public Structure TestWithMe
    Private _x As Integer
    Sub S()
        With Me
            ._x = 1
            ._x = 2
        End With
    End Sub
End Structure