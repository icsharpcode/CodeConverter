
Imports System.Collections.Generic
Imports System.Linq

Public Class Issue635
    Dim foo As Object
    Dim l As List(Of Issue635)
    Dim listSelectWhere = From t in l
            Select t.foo
            Where 1 = 2
End Class