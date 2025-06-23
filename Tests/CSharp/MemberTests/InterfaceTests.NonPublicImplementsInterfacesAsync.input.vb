Public Interface IFoo
    Property FriendProp As Integer
    Sub ProtectedSub()
    Function PrivateFunc() As Integer
    Sub ProtectedInternalSub()
    Sub AbstractSub()
End Interface

Public Interface IBar
    Property FriendProp As Integer
    Sub ProtectedSub()
    Function PrivateFunc() As Integer
    Sub ProtectedInternalSub()
    Sub AbstractSub()
End Interface

Public MustInherit Class BaseFoo
    Implements IFoo, IBar
    
    Friend Overridable Property FriendProp As Integer Implements IFoo.FriendProp, IBar.FriendProp ' Comment moves because this line gets split
        Get
          Return 5
        End Get
        Set
        End Set
    End Property

    Protected Sub ProtectedSub() Implements IFoo.ProtectedSub, IBar.ProtectedSub
    End Sub

    Private Function PrivateFunc() As Integer Implements IFoo.PrivateFunc, IBar.PrivateFunc
    End Function

    Protected Friend Overridable Sub ProtectedInternalSub() Implements IFoo.ProtectedInternalSub, IBar.ProtectedInternalSub
    End Sub

    Protected MustOverride Sub AbstractSubRenamed() Implements IFoo.AbstractSub, IBar.AbstractSub
End Class

Public Class Foo
    Inherits BaseFoo

    Protected Friend Overrides Sub ProtectedInternalSub()
    End Sub

    Protected Overrides Sub AbstractSubRenamed()
    End Sub
End Class
