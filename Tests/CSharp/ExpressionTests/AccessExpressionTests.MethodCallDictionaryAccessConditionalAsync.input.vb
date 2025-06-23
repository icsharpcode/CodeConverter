Public Class A
    Public Sub Test()
        Dim dict = New Dictionary(Of String, String) From {{"a", "AAA"}, {"b", "bbb"}}
        Dim v = dict?.Item("a")
    End Sub
End Class