Class IndexedPropertyWithTrivia
    'a
    Property P(i As Integer) As Integer
        'b
        Get
            '1
            Dim x = 1 '2
            '3
        End Get

        'c
        Set(value As Integer)
            '4
            Dim x = 1 '5
            '6
            x = value + i '7
            '8
        End Set
        'd
    End Property
End Class