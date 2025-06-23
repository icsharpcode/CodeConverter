Class StaticLocalConvertedToField
    Function OtherName() as Integer
        Static sPrevPosition As Integer
        sPrevPosition = 23
        Console.WriteLine(sPrevPosition)
        Return sPrevPosition
    End Function
End Class