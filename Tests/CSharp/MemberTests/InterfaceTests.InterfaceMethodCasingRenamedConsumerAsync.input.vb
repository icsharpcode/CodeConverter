Public Interface IFoo
        Function DoFoo(str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function dofoo(str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(str As String, i As Integer) As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.dofoo(str, i) + bar.DoFoo(str, i)
    End Function
End Class