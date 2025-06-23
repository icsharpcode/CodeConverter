
Public Interface IFoo
    Property Prop(Optional x As Integer = 1, Optional y as Integer = 2, Optional z as Integer = 3) As Integer
End Interface
Public Class SomeClass
    Implements IFoo
    Friend Property Prop2(Optional x As Integer = 1, Optional y as Integer = 2, Optional z as Integer = 3) As Integer Implements IFoo.Prop
        Get
        End Get
        Set
        End Set
    End Property

    Sub TestGet()
        Dim foo As IFoo = Me
        Dim a = Prop2(,) + Prop2(, 20) + Prop2(10,) + Prop2(,20,) + Prop2(,,30) + Prop2(10,,) + Prop2(,,)
        Dim b = foo.Prop(,) + foo.Prop(, 20) + foo.Prop(10,) + foo.Prop(,20,) + foo.Prop(,,30) + foo.Prop(10,,) + foo.Prop(,,)
    End Sub

    Sub TestSet()
        Prop2(,) = 1
        Prop2(, 20) = 1
        Prop2(10, ) = 1
        Prop2(,20,) = 1
        Prop2(,,30) = 1
        Prop2(10,,) = 1
        Prop2(,,) = 1

        Dim foo As IFoo = Me
        foo.Prop(,) = 1
        foo.Prop(, 20) = 1
        foo.Prop(10, ) = 1
        foo.Prop(,20,) = 1
        foo.Prop(,,30) = 1
        foo.Prop(10,,) = 1
        foo.Prop(,,) = 1
    End Sub
End Class