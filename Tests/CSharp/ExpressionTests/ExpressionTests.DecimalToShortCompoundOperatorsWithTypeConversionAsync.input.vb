Public Class Compound
    Public Sub Operators()
        Dim aShort As Short = 123
        Dim aDec As Decimal = 12.3
        aShort *= aDec
        aShort \= aDec
        aShort /= aDec
        aShort -= aDec
        aShort += aDec
    End Sub
End Class