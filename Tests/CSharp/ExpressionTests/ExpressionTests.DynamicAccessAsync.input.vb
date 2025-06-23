Option Strict Off ' Directive gets removed

Public Class TestDynamicUsage
    Property Prop As Integer

    Sub S()
        Dim o As Object
        o = New TestDynamicUsage
        o.Prop = 1 'Must not cast to object here
    End Sub
End Class