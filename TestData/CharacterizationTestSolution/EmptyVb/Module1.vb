Module Module1
    Private dict As New Dictionary(Of Integer, Integer)

    Private Sub UseOutParameterInModule()
        Dim x
        dict.TryGetValue(1, x)
    End Sub

    Sub Main()
        'Empty
    End Sub

End Module
