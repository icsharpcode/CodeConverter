
Public Class C
    Public Function IsHybridApp() As Boolean
        Return New Object().Session("hybrid") IsNot Nothing AndAlso New Object().Session("hybrid") = 1
    End Function
End Class