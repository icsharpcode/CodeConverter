
Public Class C
    Public Sub S()
        Dim i As Integer = 0
        Modify(i)
        System.Diagnostics.Debug.Assert(i = 1)
        Modify((i))
        System.Diagnostics.Debug.Assert(i = 1)
    End Sub

    Sub Modify(ByRef i As Integer)
        i = i + 1
    End Sub
End Class
