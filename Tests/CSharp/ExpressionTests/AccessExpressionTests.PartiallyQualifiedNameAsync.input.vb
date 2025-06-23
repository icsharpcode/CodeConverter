Imports System.Collections ' Removed by simplifier
Class TestClass
    Public Sub TestMethod(dir As String)
        IO.Path.Combine(dir, "file.txt")
        Dim c As New ObjectModel.ObservableCollection(Of String)
    End Sub
End Class