Public Class Issue876
    Property SomeProperty As Integer
    Property SomeProperty2 As Integer
    Property SomeProperty3 As Integer

    Public Function InlineAssignHelper(Of T)(ByRef lhs As T, ByVal rhs As T) As T
        lhs = rhs
        Return lhs
    End Function

    Public Sub Main()
        Dim result = InlineAssignHelper(SomeProperty, InlineAssignHelper(SomeProperty2, InlineAssignHelper(SomeProperty3, 1)))
    End Sub
End Class