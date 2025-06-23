Public Class Issue213
    Const x As Date = #1990-1-1#

    Private Sub Y(Optional ByRef opt As Date = x)
    End Sub

    Private Sub CallsY()
        Y
    End Sub
End Class