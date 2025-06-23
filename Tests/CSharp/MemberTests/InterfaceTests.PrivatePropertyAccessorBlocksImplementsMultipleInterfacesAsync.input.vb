Public Interface IFoo
    Property ExplicitProp As Integer
End Interface

Public Interface IBar
    Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    Private Property ExplicitProp As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp ' Comment moves because this line gets split
        Get
          Return 5
        End Get
        Set
        End Set
    End Property
End Class