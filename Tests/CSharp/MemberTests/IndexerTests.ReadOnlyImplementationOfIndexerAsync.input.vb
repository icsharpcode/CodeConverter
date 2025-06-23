
Public Interface IFoo
    Default ReadOnly Property Item(str As String) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Default Public Overridable ReadOnly Property Item(str As String) As Integer Implements IFoo.Item
    Get
        Return 2
    End Get
    End Property
End Class