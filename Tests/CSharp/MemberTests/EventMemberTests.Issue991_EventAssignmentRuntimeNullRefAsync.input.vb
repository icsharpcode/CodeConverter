Imports System

Public Module Program
    Public Sub Main(args As String())
        Dim c As New SomeClass(New SomeDependency())
        Console.WriteLine("Done")
    End Sub
End Module

Public Class SomeDependency
    Public Event SomeEvent As EventHandler
End Class

Public Class SomeClass
    Private WithEvents _dep As SomeDependency

    Public Sub New(dep)
        _dep = dep
    End Sub

    Private Sub _dep_SomeEvent(sender As Object, e As EventArgs) Handles _dep.SomeEvent
        ' Do Something
    End Sub
End Class
