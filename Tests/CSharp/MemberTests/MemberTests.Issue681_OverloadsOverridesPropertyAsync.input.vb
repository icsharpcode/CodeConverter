Public Class C 
    Inherits B

    Public ReadOnly Overloads Overrides Property X()
        Get
            Return Nothing
        End Get
    End Property
End Class

Public Class B
    Public ReadOnly Overridable Property X()
        Get
            Return Nothing
        End Get
    End Property
End Class