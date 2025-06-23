Class TestClass
    Private Sub TestMethod()
        Dim a As Boolean? = Nothing
        Dim b As Boolean? = Nothing
        Dim x As Boolean = False

        If a Or b Then Return
        If a OrElse b Then Return
        If a Or x Then Return
        If a OrElse x Then Return
        If x Or a Then Return
        If x OrElse a Then Return

        Dim res As Boolean? = a Or b
        res = a OrElse b
        res = a Or x
        res = a OrElse x 
        res = x Or a
        res = x OrElse a 
        
    End Sub
End Class