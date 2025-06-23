Imports System.Collections.Generic
Imports System.Linq

Public Class C
    Private Shared Sub LinqWithNullable()
        Dim a = New List(Of Integer?) From {1, 2, 3, Nothing}
        Dim result = From x In a Where x = 1
    End Sub
End Class