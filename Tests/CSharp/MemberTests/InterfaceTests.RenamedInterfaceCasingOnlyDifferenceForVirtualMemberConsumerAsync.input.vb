
Public Interface IFoo
    Function DoFoo() As Integer
    Property Prop As Integer
End Interface

Public MustInherit Class BaseFoo
    Implements IFoo

    Protected Friend Overridable Function doFoo() As Integer Implements IFoo.DoFoo
        Return 4
    End Function

    Protected Friend Overridable Property prop As Integer Implements IFoo.Prop

End Class

Public Class Foo
    Inherits BaseFoo

    Protected Friend Overrides Function DoFoo() As Integer
        Return 5
    End Function

    Protected Friend Overrides Property Prop As Integer

    Private Function Consumer() As Integer
        Dim foo As New Foo()
        Dim interfaceInstance As IFoo = foo
        Dim baseClass As BaseFoo = foo
        Return foo.doFoo() +  foo.DoFoo() +
               interfaceInstance.doFoo() + interfaceInstance.DoFoo() + 
               baseClass.doFoo() + baseClass.DoFoo() +
               foo.prop + foo.Prop +
               interfaceInstance.prop + interfaceInstance.Prop +
               baseClass.prop + baseClass.Prop
    End Function
End Class