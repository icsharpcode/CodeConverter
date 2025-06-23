
Public Interface IFoo
    Default Property Item(str As String) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Default Overridable Public Property Item(str As String) As Integer Implements IFoo.Item
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class