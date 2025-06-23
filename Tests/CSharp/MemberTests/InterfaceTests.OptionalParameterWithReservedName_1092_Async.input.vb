
Public Class WithOptionalParameters
    Sub S1(Optional a As Object = Nothing, Optional [default] As String = "")
    End Sub

    Sub S()
        S1(, "a")
    End Sub
End Class