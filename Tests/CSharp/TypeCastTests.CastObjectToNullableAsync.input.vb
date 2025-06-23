Class CastTest
    Private Function Test(input as Object) As Integer?
            Return CType(input, Integer?)
    End Function
    Private Function Test2(input as Object) As Decimal?
        Return CType(input, Nullable(Of Double))
    End Function
    Private Function Test2(input as Integer) As Decimal?
        Return CType(input, Nullable(Of Double))
    End Function
End Class