
Public Interface IFoo
    Sub Save()
    Property Prop As Integer
End Interface

Public MustInherit Class BaseFoo
    Public Overridable Sub OnSave()
    End Sub

    Public Overridable Property MyProp As Integer = 5
End Class

Public Class Foo
    Inherits BaseFoo
    Implements IFoo

    Public Shadows Sub OnSave() Implements IFoo.Save
    End Sub

    Public Shadows Property MyProp As Integer = 6 Implements IFoo.Prop

End Class