Class StaticLocalConvertedToField
    Sub New(x As Boolean)
        Static sPrevPosition As Integer = 7 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Sub New(x As Integer)
        Static sPrevPosition As Integer
        Console.WriteLine(sPrevPosition)
    End Sub
End Class