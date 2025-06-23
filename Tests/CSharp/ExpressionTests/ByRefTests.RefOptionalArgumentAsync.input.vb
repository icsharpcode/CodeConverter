Public Class OptionalRefIssue91
    Public Shared Function TestSub(Optional ByRef IsDefault As Boolean = False) As Boolean
    End Function

    Public Shared Function CallingFunc() As Boolean
        Return TestSub() AndAlso TestSub(True)
    End Function
End Class