Imports System
Imports System.Linq
Imports Xunit


''' <summary>
''' An if statement calls GetValueOrDefault on Nullables
''' The Not operator propagates nulls and only inverts true/false values
''' </summary>
Public Class BooleanTests

    <Fact>
    Public Sub NullableBoolsOnNothing()
        Dim x As Object = Nothing
        Dim res = 1

        If Not x?.Equals(4) Then res *= 2 Else res *= 3  '3 branch taken

        res *= If(x?.Equals(4), 5, 7) '7 branch taken

        Assert.Equal(21, res)
    End Sub

    <Fact>
     Public Sub NullableBoolsFalse()
        Dim x As Object = 5
        Dim res = 1

        If (Not x?.Equals(4))' x != 4
            res *= 2 'Branch taken
        Else
            res *= 3
        End If


        If (x?.Equals(4))' x == 4
            res *= 5
        Else
            res *= 7 'Branch taken
        End If

        Assert.Equal(14, res)
    End Sub

    <Fact>
    Public Sub NullableBoolsTrue()
        Dim x As Object = 4
        Dim res = 1

        If (Not x?.Equals(4)) Then ' x != 4
            res *= 2
        Else
            res *= 3 'Branch taken
        End If


        If (x?.Equals(4)) Then ' x == 4
            res *= 5 'Branch taken
        Else
            res *= 7
        End If

        Assert.Equal(15, res)
    End Sub

    <Fact>
    Public Sub VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
        Dim a1 As Object = 3
        Dim a2 As Object = 3
        Dim b As Object = 4
        Assert.True(a1 = a2, "Identical values stored in objects should be equal")
        Assert.False(a1 = b, "Different values stored in objects should not be equal")
    End Sub
End Class

