Imports System
Imports System.Diagnostics

Public Class TargetTypeTestClass

    Private Shared Sub Main()
        Dim actions As Action() = {Sub() Debug.Print(1), Sub() Debug.Print(2)}
        Dim objects = New List(Of Object) From {Sub() Debug.Print(3), Sub() Debug.Print(4)}
    End Sub
End Class