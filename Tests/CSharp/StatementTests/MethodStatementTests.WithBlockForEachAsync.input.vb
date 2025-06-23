Imports System.Collections.Generic

Public Class TestWithForEachClass
    Private _x As Integer

    Public Shared Sub Main()
        Dim x = New List(Of TestWithForEachClass)()
        For Each y In x
            With y
                ._x = 1
                System.Console.Write(._x)
            End With
            y = Nothing
        Next
    End Sub
End Class