Class RefConstArgument
    Const a As String = "a"
    Sub S()
        Const b As String = "b"
        MO(a)
        MS(b)
    End Sub
    Sub MO(ByRef s As Object) : End Sub
    Sub MS(ByRef s As String) : End Sub
End Class