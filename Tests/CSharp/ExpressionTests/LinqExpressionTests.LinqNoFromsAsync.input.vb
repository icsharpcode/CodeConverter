Public Class VisualBasicClass
    Public Shared Sub X(objs As List(Of Object))
        Dim MaxObj As Integer = Aggregate o In objs Into Max(o.GetHashCode())
        Dim CountWhereObj As Integer = Aggregate o In objs Where o.GetHashCode() > 3 Into Count()
    End Sub
End Class