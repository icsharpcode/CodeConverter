
Public Interface IFoo
    Property ExplicitProp As Integer
    ReadOnly Property ExplicitReadOnlyProp As Integer
End Interface

Public Class Foo
    Implements IFoo
    
    Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp
    ReadOnly Property ExplicitRenamedReadOnlyProp As Integer Implements IFoo.ExplicitReadOnlyProp

    Private Sub Consumer()
        _ExplicitPropRenamed = 5
        _ExplicitRenamedReadOnlyProp = 10
    End Sub

End Class