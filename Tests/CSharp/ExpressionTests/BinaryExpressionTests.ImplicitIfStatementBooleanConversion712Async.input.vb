Class TestClass712
    Private Function TestMethod()
        Dim var1 As Boolean? = Nothing
        Dim var2 As Boolean? = Nothing
        If var1 OrElse Not var2 Then Return True Else Return False
    End Function
End Class