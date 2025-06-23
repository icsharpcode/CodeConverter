Public Interface IFoo
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Interface IBar
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Function Foo(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFooBar, IBar.DoFooBar
        Return 4
    End Function

End Class