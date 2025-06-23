Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.DOFOORENAMED(str, i) + bar.DoFoo(str, i)
    End Function
End Class