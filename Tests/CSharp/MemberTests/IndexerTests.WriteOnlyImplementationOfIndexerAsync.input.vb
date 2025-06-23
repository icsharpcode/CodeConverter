
Public Interface IFoo
    Default WriteOnly Property Item(str As String) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Default Public Overridable WriteOnly Property Item(str As String) As Integer Implements IFoo.Item
    Set
    End Set
    End Property
End Class