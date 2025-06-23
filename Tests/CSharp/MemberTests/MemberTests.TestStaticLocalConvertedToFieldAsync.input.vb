Class StaticLocalConvertedToField
    Sub OtherName(x As Boolean)
        Static sPrevPosition As Integer = 3 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Function OtherName(x As Integer) as Integer
        Static sPrevPosition As Integer
        Return sPrevPosition
    End Function
End Class