Public Interface IFoo
    WriteOnly Property ExplicitProp As Integer
End Interface

Public Interface IBar
    WriteOnly Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar
    
    WriteOnly Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp ' Comment moves because this line gets split
        Set
        End Set        
    End Property
End Class