Public Class Issue856
    Sub Main()
        Dim decimalTarget As Decimal
        Double.TryParse("123", decimalTarget)
        
        Dim longTarget As Long
        Integer.TryParse("123", longTarget)
        
        Dim intTarget As Integer
        Long.TryParse("123", intTarget)
    End Sub

End Class