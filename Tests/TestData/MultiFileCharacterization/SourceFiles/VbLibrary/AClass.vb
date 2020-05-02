Friend Class AClass
    Enum NestedEnum
        First
    End Enum

    Private dict As New Dictionary(Of Integer, Integer)
    Private anInt As Integer = 2
    Private anIntWithNonStaticInitializerReferencingOtherPart As Integer = anArrayWithNonStaticInitializerReferencingOtherPart.Length

    Private Sub UseOutParameterInClass()
        Dim x
        dict.TryGetValue(1, x)
    End Sub

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
