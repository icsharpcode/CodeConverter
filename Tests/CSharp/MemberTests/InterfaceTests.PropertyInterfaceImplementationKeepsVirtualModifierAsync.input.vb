Public Interface IFoo
    Property PropParams(str As String) As Integer
    Property Prop() As Integer
End Interface

Public Class Foo
    Implements IFoo

    Public Overridable Property PropParams(str As String) As Integer Implements IFoo.PropParams
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Overridable Property Prop As Integer Implements IFoo.Prop
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class