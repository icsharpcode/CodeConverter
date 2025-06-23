
Public Interface IFoo
    Default Property Item(str As String) As Integer
End Interface

Public MustInherit Class Foo
    Implements IFoo

    Default Public MustOverride Property Item(str As String) As Integer Implements IFoo.Item
End Class

Public Class FooChild
    Inherits Foo

    Default Public Overrides Property Item(str As String) As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class