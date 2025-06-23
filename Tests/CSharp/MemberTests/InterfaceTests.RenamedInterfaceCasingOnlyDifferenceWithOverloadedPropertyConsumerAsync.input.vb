
Public Interface IUserContext
    ReadOnly Property GroupID As String
End Interface

Public Interface IFoo
    ReadOnly Property ConnectedGroupId As String
End Interface

Public MustInherit Class BaseFoo
    Implements IUserContext

    Protected Friend ReadOnly Property ConnectedGroupID() As String Implements IUserContext.GroupID

End Class

Public Class Foo
    Inherits BaseFoo
    Implements IFoo

    Protected Friend Overloads ReadOnly Property ConnectedGroupID As String Implements IFoo.ConnectedGroupId ' Comment moves because this line gets split
        Get
            Return If("", MyBase.ConnectedGroupID())
        End Get
    End Property

    Private Function Consumer() As String
        Dim foo As New Foo()
        Dim ifoo As IFoo = foo
        Dim baseFoo As BaseFoo = foo
        Dim iUserContext As IUserContext = foo
        Return foo.ConnectedGroupID & foo.ConnectedGroupId & 
               iFoo.ConnectedGroupID & iFoo.ConnectedGroupId &
               baseFoo.ConnectedGroupID & baseFoo.ConnectedGroupId &
               iUserContext.GroupId & iUserContext.GroupID
    End Function

End Class