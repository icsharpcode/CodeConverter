Class TestClass
Public Function SelfDivisionPossible(x as Integer) As Boolean
    On Error GoTo ErrorHandler
        Dim i as Integer = x / x
ErrorHandler:
    Return i <> 0
End Function
End Class