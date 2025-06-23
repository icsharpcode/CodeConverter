
Public Interface IFoo
  Function DoFooBar(ByRef str As String, Optional i As Integer = 4) As Integer
End Interface

Public Interface IBar
  Function DoFooBar(ByRef str As String, Optional i As Integer = 8) As Integer
End Interface

Public Class FooBar
  Implements IFoo, IBar

  Function Foo(ByRef str As String, Optional i As Integer = 4) As Integer Implements IFoo.DoFooBar
    Return 4
  End Function

  Function Bar(ByRef str As String, Optional i As Integer = 8) As Integer Implements IBar.DoFooBar
    Return 2
  End Function

End Class