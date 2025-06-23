Public Class CopiedFromTheSelfVerifyingBooleanTests
    Public Sub VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
        Dim a1 As Object = 3
        Dim a2 As Object = 3
        AssertTrue(a1 = a2, "Identical values stored in objects should be equal")
    End Sub

    Private Sub AssertTrue(v1 As Nullable(Of Boolean), v2 As String)
    End Sub

    Private Sub AssertTrue(v1 As Boolean, v2 As String)
    End Sub
End Class