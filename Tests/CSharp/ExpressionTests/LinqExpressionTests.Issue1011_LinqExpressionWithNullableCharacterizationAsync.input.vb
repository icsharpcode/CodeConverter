Public Class ConversionTest2
    Private Class MyEntity
        Property FavoriteNumber As Integer?
        Property Name As String
    End Class
    Private Sub BugRepro()

        Dim entities As New List(Of MyEntity)

        Dim result As String = (From e In entities
                                Where e.FavoriteNumber = 123
                                Select e.Name).Single

    End Sub
End Class
