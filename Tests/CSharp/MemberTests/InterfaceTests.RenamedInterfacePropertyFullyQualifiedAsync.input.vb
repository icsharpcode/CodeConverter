Namespace TestNamespace
    Public Interface IFoo
        Property FooProp As Integer
    End Interface
End Namespace

Public Class Foo
    Implements TestNamespace.IFoo

    Property FooPropRenamed As Integer Implements TestNamespace.IFoo.FooProp
    
End Class