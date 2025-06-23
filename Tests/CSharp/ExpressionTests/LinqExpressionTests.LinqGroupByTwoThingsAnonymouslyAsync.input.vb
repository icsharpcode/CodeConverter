Public Class Class1
    Sub Foo()
        Dim xs As New List(Of String)
        Dim y = From x In xs Group By x.Length, x.Count() Into Group
    End Sub
End Class