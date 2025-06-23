
Public Interface IFoo
    Sub Save()
    Property Prop As Integer
End Interface

Public MustInherit Class BaseFoo
    Protected Overridable Sub OnSave()
    End Sub

    Protected Overridable Property MyProp As Integer = 5
End Class

Public Class Foo
    Inherits BaseFoo
    Implements IFoo

    Protected Overrides Sub OnSave() Implements IFoo.Save
    End Sub

    Protected Overrides Property MyProp As Integer = 6 Implements IFoo.Prop

End Class