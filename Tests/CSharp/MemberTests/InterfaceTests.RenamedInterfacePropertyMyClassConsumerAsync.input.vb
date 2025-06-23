Public Interface IFoo
        ReadOnly Property DoFoo As Integer
        WriteOnly Property DoBar As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Overridable ReadOnly Property DoFooRenamed As Integer Implements IFoo.DoFoo  ' Comment ends up out of order, but attached to correct method
        Get
            Return 4
        End Get
    End Property

    Overridable WriteOnly Property DoBarRenamed As Integer Implements IFoo.DoBar  ' Comment ends up out of order, but attached to correct method
        Set
            Throw New Exception()
        End Set
    End Property

    Sub DoFooRenamedConsumer()
        MyClass.DoBarRenamed = MyClass.DoFooRenamed
    End Sub
End Class