Friend Class AClass
    Private Sub UseEnumFromOtherFileInSolution(m As AnEnum)
        Dim [nothing] = Enumerable.Empty(Of String).ToArray()(AnEnum.AnEnumMember)
        Select Case m
            Case -1
                Exit Sub
            Case AnEnum.AnEnumMember
                Exit Sub
        End Select
    End Sub
End Class
