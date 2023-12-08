Public Class Class1
    Private Structure SomeStruct
        Dim SomeField As Integer
        Dim Text As String
    End Structure

    Dim lst As New CSharpDllWithRefReturnIndexer.SpecialListWithRefReturnIndexer(Of SomeStruct)

    Sub S()
        Dim i As Integer
        Dim s As String

        With lst(i)
            .SomeField = 5
            s = .Text
        End With
    End Sub
End Class
