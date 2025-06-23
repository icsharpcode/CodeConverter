
Public Interface IFoo
    Property Prop(Optional x As Integer = 1, Optional y as Integer = 2) As Integer
End Interface
Public Class SomeClass
    Implements IFoo
    Friend Property Prop2(Optional x As Integer = 1, Optional y as Integer = 2) As Integer Implements IFoo.Prop
        Get
        End Get
        Set
        End Set
    End Property

    Sub TestGet()
        Dim foo As IFoo = Me
        Dim a = Prop2() + Prop2(y := 20) + Prop2(x := 10) + Prop2(y := -2, x := -1) + Prop2(x := -1, y := -2)
        Dim b = foo.Prop() + foo.Prop(y := 20) + foo.Prop(x := 10) + foo.Prop(y := -2, x := -1) + foo.Prop(x := -1, y := -2)
    End Sub

    Sub TestSet()
        Prop2() = 1
        Prop2(-1, -2) = 1
        Prop2(-1) = 1
        Prop2(y := 20) = 1
        Prop2(x := 10) = 1
        Prop2(y := -2, x := -1) = 1
        Prop2(x := -1, y := -2) = 1

        Dim foo As IFoo = Me
        foo.Prop() = 1
        foo.Prop(-1, -2) = 1
        foo.Prop(-1) = 1
        foo.Prop(y := 20) = 1
        foo.Prop(x := 10) = 1
        foo.Prop(y := -2, x := -1) = 1
        foo.Prop(x := -1, y := -2) = 1
    End Sub
End Class