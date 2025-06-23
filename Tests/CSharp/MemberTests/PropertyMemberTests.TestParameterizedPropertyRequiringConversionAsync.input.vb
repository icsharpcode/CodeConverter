Public Class Class1
    Public Property SomeProp(ByVal index As Integer) As Single
        Get
            Return 1.5
        End Get
        Set(ByVal Value As Single)
        End Set
    End Property

    Public Sub Foo()
        Dim someDecimal As Decimal = 123.0
        SomeProp(123) = someDecimal
    End Sub
End Class