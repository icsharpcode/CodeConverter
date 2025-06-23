Public Interface IFoo
        Property FooBarProp As Integer
    End Interface

Public Interface IBar
    Property FooBarProp As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Property Foo As Integer Implements IFoo.FooBarProp

    Property Bar As Integer Implements IBar.FooBarProp
    
End Class