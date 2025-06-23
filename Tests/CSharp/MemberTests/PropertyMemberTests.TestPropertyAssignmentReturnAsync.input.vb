Public Class Class1
    Public ReadOnly Property Foo() As String
        Get
            Foo = ""
        End Get
    End Property
    Public ReadOnly Property X As String
        Get
            X = 4
            X = X * 2
            Dim y = "random variable to check it isn't just using the value of the last statement"
        End Get
    End Property
    Public _y As String
    Public WriteOnly Property Y As String
        Set(value As String)
            If value <> "" Then
                Y = ""
            Else
                _y = ""
            End If
        End Set
    End Property
End Class