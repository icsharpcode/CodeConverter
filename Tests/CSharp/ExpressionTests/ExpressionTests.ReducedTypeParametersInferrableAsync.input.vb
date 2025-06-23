Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = "".Split(","c).Select(Of String)(Function(x) x)
    End Sub
End Class