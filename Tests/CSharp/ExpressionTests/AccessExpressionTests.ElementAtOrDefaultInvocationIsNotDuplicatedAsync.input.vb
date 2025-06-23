Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = "".Split(","c).Select(Function(x) x)
        Dim z = y.ElementAtOrDefault(0)
    End Sub
End Class