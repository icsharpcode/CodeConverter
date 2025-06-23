Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property fooprop As Integer Implements IFoo.FooProp
    
End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.fooprop + bar.FooProp
    End Function
End Class