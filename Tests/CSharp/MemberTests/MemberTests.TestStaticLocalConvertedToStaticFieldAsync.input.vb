Class StaticLocalConvertedToField
    Shared Sub OtherName(x As Boolean)
        Static sPrevPosition As Integer ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Sub OtherName(x As Integer)
        Static sPrevPosition As Integer = 5 ' Comment also moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Shared ReadOnly Property StaticTestProperty() As Integer
        Get
            Static sPrevPosition As Integer = 5 ' Comment also moves with declaration
            Return sPrevPosition + 1
        End Get
    End Property
End Class