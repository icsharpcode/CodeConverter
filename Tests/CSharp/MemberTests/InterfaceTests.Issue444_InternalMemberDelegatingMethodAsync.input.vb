Public Interface IFoo
    Function FooDifferentName(ByRef str As String, i As Integer) As Integer
End Interface

Friend Class Foo
    Implements IFoo

    Function BarDifferentName(ByRef str As String, i As Integer) As Integer Implements IFoo.FooDifferentName
        Return 4
    End Function
End Class