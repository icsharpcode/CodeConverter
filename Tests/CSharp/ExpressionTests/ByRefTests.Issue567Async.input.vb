Public Class Issue567
    Dim arr() As String
    Dim arr2(,) As String

    Sub DoSomething(ByRef str As String)
        str = "test"
    End Sub

    Sub Main()
        DoSomething(arr(1))
        Debug.Assert(arr(1) = "test")
        DoSomething(arr2(2, 2))
        Debug.Assert(arr2(2, 2) = "test")
    End Sub

End Class