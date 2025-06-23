Class TestGenericCast
    Private Shared Function GenericFunctionWithCTypeCast(Of T)() As T
        Const result = 1
        Dim resultObj As Object = result
        Return CType(resultObj, T)
    End Function
    Private Shared Function GenericFunctionWithCast(Of T)() As T
        Const result = 1
        Dim resultObj As Object = result
        Return resultObj
    End Function
    Private Shared Function GenericFunctionWithCastThatExistsInCsharp(Of T As {TestGenericCast})() As T
        Return New TestGenericCast
    End Function
End Class