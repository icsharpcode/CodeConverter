Public Interface IFoo
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Private Function ExplicitFunc(ByRef str As String, i As Integer) As Integer Implements IFoo.ExplicitFunc
        Return 5
    End Function
    
    Private Property ExplicitProp(str As String) As Integer Implements IFoo.ExplicitProp
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class