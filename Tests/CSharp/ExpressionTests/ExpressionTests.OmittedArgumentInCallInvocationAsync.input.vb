Public Class Issue445MissingParameter
    Public Sub First(a As String, b As String, c As Integer)
        Call mySuperFunction(7, , New Object())
    End Sub

    Private Sub mySuperFunction(intSomething As Integer, Optional p As Object = Nothing, Optional optionalSomething As Object = Nothing)
        Throw New NotImplementedException()
    End Sub
End Class