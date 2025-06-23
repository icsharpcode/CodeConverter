Class TestClass
    Private Function FuncReturningNull() As Object
        Dim zeroLambda = Function(y) As Integer
                            Exit Function
                         End Function
        Exit Function
    End Function

    Private Function FuncReturningZero() As Integer
        Dim nullLambda = Function(y) As Object
                            Exit Function
                         End Function
        Exit Function
    End Function

    Private Function FuncReturningAssignedValue() As Integer
        Dim aSub = Sub(y)
                            Exit Sub
                         End Sub
        FuncReturningAssignedValue = 3
        Exit Function
    End Function
End Class