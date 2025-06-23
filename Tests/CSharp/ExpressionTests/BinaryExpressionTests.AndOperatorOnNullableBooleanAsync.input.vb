Class TestClass
    Private Sub TestMethod()
        Dim a As Boolean? = Nothing
        Dim b As Boolean? = Nothing
        Dim x As Boolean = False

        If a And b Then Return
        If a AndAlso b Then Return
        If a And x Then Return
        If a AndAlso x Then Return
        If x And a Then Return
        If x AndAlso a Then Return

        Dim res As Boolean? = a And b
        res = a AndAlso b
        res = a And x
        res = a AndAlso x 
        res = x And a
        res = x AndAlso a 
        
    End Sub
End Class