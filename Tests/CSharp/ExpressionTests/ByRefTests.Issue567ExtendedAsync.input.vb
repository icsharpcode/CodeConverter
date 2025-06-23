Public Class Issue567
    Sub DoSomething(ByRef str As String)
        lst = New List(Of String)({4.ToString(), 5.ToString(), 6.ToString()})
        lst2 = New List(Of Object)({4.ToString(), 5.ToString(), 6.ToString()})
        str = 999.ToString()
    End Sub

    Sub Main()
        DoSomething(lst(1))
        Debug.Assert(lst(1) = 4.ToString())
        DoSomething(lst2(1))
        Debug.Assert(lst2(1) = 5.ToString())
    End Sub

End Class

Friend Module Other
    Public lst As List(Of String) = New List(Of String)({ 1.ToString(), 2.ToString(), 3.ToString()})
    Public lst2 As List(Of Object) = New List(Of Object)({ 1.ToString(), 2.ToString(), 3.ToString()})
End Module