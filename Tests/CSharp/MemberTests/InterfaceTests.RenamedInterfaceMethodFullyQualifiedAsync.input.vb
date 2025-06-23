Namespace TestNamespace
    Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface
End Namespace

Public Class Foo
    Implements TestNamespace.IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements TestNamespace.IFoo.DoFoo
        Return 4
    End Function
End Class