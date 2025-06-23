Public Class ConversionInComparisonOperatorTest
    Public Sub Foo()
        Dim SomeDecimal As Decimal = 12.3
        Dim ACalc As Double = 32.1
        If ACalc > 60 / SomeDecimal Then
            Console.WriteLine(1)
        End If
    End Sub
End Class