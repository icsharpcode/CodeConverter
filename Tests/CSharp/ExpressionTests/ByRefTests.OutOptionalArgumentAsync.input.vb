
Public Class OptionalOutIssue882
    Private Sub TestSub(<Out> ByRef a As Integer, <Out> Optional ByRef b As Integer = Nothing)
        a = 42
        b = 23
    End Sub

    Public Sub CallingFunc()
        Dim a As Integer
        Dim b As Integer
        TestSub(a:=a, b:=b)
        TestSub(a:=a)
    End Sub
End Class