Public Interface IFoo
    ReadOnly Property ExplicitProp As Integer
End Interface

Public Interface IBar
    ReadOnly Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    ReadOnly Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
End Class