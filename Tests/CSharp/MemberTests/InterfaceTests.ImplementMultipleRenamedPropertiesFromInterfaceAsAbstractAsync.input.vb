
Public Interface IFoo
    Property ExplicitProp As Integer
End Interface
Public Interface IBar
    Property ExplicitProp As Integer
End Interface
Public MustInherit Class Foo
    Implements IFoo, IBar

    Protected MustOverride Property ExplicitPropRenamed1 As Integer Implements IFoo.ExplicitProp
    Protected MustOverride Property ExplicitPropRenamed2 As Integer Implements IBar.ExplicitProp
End Class