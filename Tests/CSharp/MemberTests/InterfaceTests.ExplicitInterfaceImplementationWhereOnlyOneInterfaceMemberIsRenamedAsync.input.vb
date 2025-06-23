
Public Interface IFoo
    Sub Save()
    Property A As Integer
End Interface

Public Interface IBar
    Sub OnSave()
    Property B As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    Public Overridable Sub Save() Implements IFoo.Save, IBar.OnSave
    End Sub

    Public Overridable Property A As Integer Implements IFoo.A, IBar.B

End Class