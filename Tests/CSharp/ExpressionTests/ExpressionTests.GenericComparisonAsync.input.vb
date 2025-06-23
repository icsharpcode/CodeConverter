Public Class GenericComparison
    Public Sub m(Of T)(p As T)
        If p Is Nothing Then Return
    End Sub
End Class