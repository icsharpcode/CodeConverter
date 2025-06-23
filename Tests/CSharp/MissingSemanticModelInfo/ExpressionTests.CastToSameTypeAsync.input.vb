Public Class CastToSameTypeTest

    Sub PositionEnumFromString(ByVal c As Char)
        Select Case c
            Case CChar(".")
                Console.WriteLine(1)
            Case CChar(",")
                Console.WriteLine(2)
        End Select
    End Sub
End Class