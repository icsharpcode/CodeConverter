Imports System.Linq

Public Class Class1
    Sub Foo()
        Dim y = "".Split(","c).Select(Of Object)(Function(x) x)
    End Sub
End Class