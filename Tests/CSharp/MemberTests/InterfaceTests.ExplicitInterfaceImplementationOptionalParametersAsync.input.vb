Public Interface IFoo
  Property ExplicitProp(Optional str As String = "") As Integer
  Function ExplicitFunc(Optional str2 As String = "", Optional i2 As Integer = 1) As Integer
End Interface

Public Class Foo
  Implements IFoo

  Private Function ExplicitFunc(Optional str As String = "", Optional i2 As Integer = 1) As Integer Implements IFoo.ExplicitFunc
    Return 5
  End Function
    
  Private Property ExplicitProp(Optional str As String = "") As Integer Implements IFoo.ExplicitProp
    Get
      Return 5
    End Get
    Set(value As Integer)
    End Set
  End Property
End Class