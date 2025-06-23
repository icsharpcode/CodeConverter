Class TestClass
Public Function SelfDivisionPossible(x as Integer) As Boolean
    On Error GoTo ErrorHandler
        Dim i as Integer = x / x
        Return True
ErrorHandler:
    Return Err.Number = 6
End Function
End Class