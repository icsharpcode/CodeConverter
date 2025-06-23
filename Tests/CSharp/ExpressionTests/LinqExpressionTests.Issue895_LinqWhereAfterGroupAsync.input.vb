
Imports System.Collections.Generic
Imports System.Linq

Public Class Issue895
    Private Shared Sub LinqWithGroup()
        Dim numbers = New List(Of Integer) From {1, 2, 3, 4, 4}
        Dim duplicates = From x In numbers
                         Group By x Into Group
                         Where Group.Count > 1
        System.Console.WriteLine(duplicates.Count)
    End Sub
End Class