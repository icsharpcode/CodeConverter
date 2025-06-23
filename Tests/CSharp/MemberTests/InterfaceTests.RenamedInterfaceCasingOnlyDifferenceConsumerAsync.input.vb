
Public Interface IFoo
    Function DoFoo() As Integer
    Property Prop As Integer
End Interface

Public Class Foo
    Implements IFoo

    Private Function doFoo() As Integer Implements IFoo.DoFoo
        Return 4
    End Function

    Private Property prop As Integer Implements IFoo.Prop

    Private Function Consumer() As Integer
        Dim foo As New Foo()
        Dim interfaceInstance As IFoo = foo
        Return foo.doFoo() + foo.DoFoo() +
               interfaceInstance.doFoo() + interfaceInstance.DoFoo() +
               foo.prop + foo.Prop +
               interfaceInstance.prop + interfaceInstance.Prop
    End Function

End Class