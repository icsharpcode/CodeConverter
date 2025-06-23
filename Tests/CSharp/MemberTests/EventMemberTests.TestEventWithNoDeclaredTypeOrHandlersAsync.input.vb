Public Class TestEventWithNoType
    Public Event OnCakeChange

    Public Sub RaisingFlour()
        RaiseEvent OnCakeChange
    End Sub
End Class