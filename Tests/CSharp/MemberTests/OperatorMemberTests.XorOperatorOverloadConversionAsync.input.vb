
Public Class MyType
    Public Shared Operator Xor(left As MyType, right As MyType) As MyType
        Throw New Global.System.NotSupportedException("Not supported")
    End Operator
End Class