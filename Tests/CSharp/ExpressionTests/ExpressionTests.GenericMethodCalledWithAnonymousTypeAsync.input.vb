Public Class MoreParsing
    Sub DoGet()
        Dim anon = New With {
            .ANumber = 5
        }
        Dim sameAnon = Identity(anon)
        Dim repeated = Enumerable.Repeat(anon, 5).ToList()
    End Sub

    Private Function Identity(Of TType)(tInstance As TType) As TType
        Return tInstance
    End Function
End Class