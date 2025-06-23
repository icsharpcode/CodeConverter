Public Partial Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp
    
End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.FooPropRenamed + bar.FooProp
    End Function
End Class