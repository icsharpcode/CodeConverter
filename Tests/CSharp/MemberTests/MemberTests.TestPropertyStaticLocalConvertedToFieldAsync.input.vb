Class StaticLocalConvertedToField
    Readonly Property OtherName() As Integer
        Get
            Static sPrevPosition As Integer = 3 ' Comment moves with declaration
            Console.WriteLine(sPrevPosition)
            Return sPrevPosition
        End Get
    End Property
    Readonly Property OtherName(x As Integer) as Integer
        Get
            Static sPrevPosition As Integer
            sPrevPosition += 1
            Return sPrevPosition
        End Get
    End Property
End Class