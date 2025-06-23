Class RefFunctionCallArgument
    Sub S(ByRef o As Object)
        S(GetI)
    End Sub
    Function GetI() As Integer : End Function
End Class