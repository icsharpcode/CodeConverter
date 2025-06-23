Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Overridable Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo ' Comment ends up out of order, but attached to correct method
        Return 4
    End Function

    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Return MyClass.DoFooRenamed(str, i)
    End Function
End Class