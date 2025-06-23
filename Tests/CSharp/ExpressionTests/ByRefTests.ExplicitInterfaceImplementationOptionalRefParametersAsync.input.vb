Public Interface IFoo
  Function ExplicitFunc(Optional ByRef str2 As String = "") As Integer
End Interface

Public Class Foo
  Implements IFoo

  Private Function ExplicitFunc(Optional ByRef str As String = "") As Integer Implements IFoo.ExplicitFunc
    Return 5
  End Function
End Class