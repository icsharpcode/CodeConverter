Public Interface IFoo
    Function FooDifferentCase(<Out> ByRef str2 As String) As Integer
End Interface

Public Class Foo
    Implements IFoo
    Function fooDifferentCase(<Out> ByRef str2 As String) As Integer Implements IFoo.FOODIFFERENTCASE
        str2 = 2.ToString()
        Return 3
    End Function
End Class