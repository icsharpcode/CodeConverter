Public Class ConversionTest6
    Private Class MyEntity
        Property Name As String
        Property FavoriteString As String
    End Class
    Public Sub BugRepro()

        Dim entities = New List(Of MyEntity) ' If this was a DbSet from EFCore, then the 'is' below needs to be converted to == to avoid an error. Instead of detecting dbset, we'll just do this for all queries

        Dim data = (From e In entities
                    Where e.Name Is Nothing OrElse e.FavoriteString IsNot Nothing
                    Select e).ToList
    End Sub
End Class
