Class Class1
    Function F(a As Net.IPAddress) As Boolean
        Return If(a?.ScopeId = 0, True)
End Function
End Class