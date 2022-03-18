Imports System
Imports System.Linq
Imports Xunit


''' <summary>
''' An if statement calls GetValueOrDefault on Nullables
''' The Not operator propagates nulls and only inverts true/false values
''' </summary>
Public Class BooleanTests

    <Theory>
    <InlineData(True, True, False)>
    <InlineData(True, False, True)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub NotEqualOperatorsOnNullableBoolean(a As Boolean?, b As Boolean?, result As Boolean?)
        Dim actualResult = a <> b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, True, False)>
    <InlineData(True, False, True)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub NotEqualOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?)
        Dim actualResult = a <> b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, True, False)>
    <InlineData(True, False, True)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub NotEqualOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?)
        Dim actualResult = a <> b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, True, False)>
    <InlineData(False, False, True)>
    Public Sub EqualOperatorsOnNullableBoolean(a As Boolean?, b As Boolean?, result As Boolean?)
        Dim actualResult = a = b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, True, False)>
    <InlineData(False, False, True)>
    Public Sub EqualOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?)
        Dim actualResult = a = b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, True, False)>
    <InlineData(False, False, True)>
    Public Sub EqualOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?)
        Dim actualResult = a = b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, Nothing, False)>
    <InlineData(False, True, False)>
    <InlineData(False, False, False)>
    Public Sub AndOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?)
        Dim actualResultAnd = a And b
        Dim actualResultAndAlso = a AndAlso b
        Assert.Equal(actualResultAnd, result)
        Assert.Equal(actualResultAndAlso, result)
        Assert.Equal(Not actualResultAnd, Not result)
        Assert.Equal(Not actualResultAndAlso, Not result)
    End Sub

    <Theory>
    <InlineData(Nothing, False, False)>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, True, False)>
    <InlineData(False, False, False)>
    Public Sub AndOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?)
        Dim actualResultAnd = a And b
        Dim actualResultAndAlso = a AndAlso b
        Assert.Equal(actualResultAnd, result)
        Assert.Equal(actualResultAndAlso, result)
        Assert.Equal(Not actualResultAnd, Not result)
        Assert.Equal(Not actualResultAndAlso, Not result)
    End Sub

    <Theory>
    <InlineData(True, Nothing, True)>
    <InlineData(True, True, True)>
    <InlineData(True, False, True)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub OrOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?)
        Dim actualResultOr = a Or b
        Dim actualResultOrElse = a OrElse b
        Assert.Equal(actualResultOr, result)
        Assert.Equal(actualResultOrElse, result)
        Assert.Equal(Not actualResultOr, Not result)
        Assert.Equal(Not actualResultOrElse, Not result)
    End Sub

    <Theory>
    <InlineData(Nothing, True, True)>
    <InlineData(True, True, True)>
    <InlineData(True, False, True)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub OrOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?)
        Dim actualResultOr = a Or b
        Dim actualResultOrElse = a OrElse b
        Assert.Equal(actualResultOr, result)
        Assert.Equal(actualResultOrElse, result)
        Assert.Equal(Not actualResultOr, Not result)
        Assert.Equal(Not actualResultOrElse, Not result)
    End Sub

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

