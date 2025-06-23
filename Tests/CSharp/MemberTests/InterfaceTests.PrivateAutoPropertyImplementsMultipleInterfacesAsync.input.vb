Public Interface IFoo
    Property ExplicitProp As Integer
End Interface

Public Interface IBar
    Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    Private Property ExplicitProp As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
End Class