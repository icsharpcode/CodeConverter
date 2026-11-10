Imports System
Imports System.Linq
Imports Xunit

''' <summary>
''' VB converts a Boolean? condition to Boolean with Nothing becoming False, before deciding whether to branch, continue (While) or stop (Until).
''' </summary>
Public Class NullableConditionTests

    <Theory>
    <InlineData(Nothing, 1, False, False)>
    <InlineData(Nothing, Nothing, False, False)>
    <InlineData(1, 2, True, False)>
    <InlineData(2, 2, False, True)>
    Public Sub ParenthesizedIfConditions(a As Integer?, b As Integer?, expectedLessThan As Boolean, expectedEquals As Boolean)
        Dim lessThan = False
        If (a < b) Then lessThan = True
        Assert.Equal(expectedLessThan, lessThan)

        Dim equals = False
        If ((a = b)) Then
            equals = True
        End If
        Assert.Equal(expectedEquals, equals)

        Assert.Equal(expectedLessThan, If((a < b), True, False))
    End Sub

    <Theory>
    <InlineData(Nothing, False, False)>
    <InlineData(True, True, False)>
    <InlineData(False, False, True)>
    Public Sub ParenthesizedBooleanIfConditions(flag As Boolean?, expectedFlag As Boolean, expectedNotFlag As Boolean)
        Dim result = False
        If (flag) Then result = True
        Assert.Equal(expectedFlag, result)

        result = False
        If (Not flag) Then
            result = True
        ElseIf ((flag)) Then
            result = False
        End If
        Assert.Equal(expectedNotFlag, result)
    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, "Fallback")>
    <InlineData(1, Nothing, "Fallback")>
    <InlineData(1, 2, "LessThan")>
    <InlineData(2, 2, "Equal")>
    Public Sub CatchWhenFilters(a As Integer?, b As Integer?, expected As String)
        Dim caught As String
        Try
            Throw New InvalidOperationException()
        Catch ex As Exception When a < b
            caught = "LessThan"
        Catch ex As Exception When (a = b)
            caught = "Equal"
        Catch ex As Exception
            caught = "Fallback"
        End Try
        Assert.Equal(expected, caught)
    End Sub

    <Theory>
    <InlineData(Nothing, "", "", "")>
    <InlineData(True, "1", "", "1")>
    <InlineData(False, "", "1", "")>
    Public Sub CatchWhenBooleanFilters(flag As Boolean?, expectedFlag As String, expectedNotFlag As String, expectedAndAlso As String)
        Assert.Equal(expectedFlag, CaughtWhen(Function() flag))
        Assert.Equal(expectedNotFlag, CaughtWhen(Function() Not flag))
        Assert.Equal(expectedAndAlso, CaughtWhen(Function() flag AndAlso True))
    End Sub

    Private Shared Function CaughtWhen(getFilter As Func(Of Boolean?)) As String
        Try
            Throw New InvalidOperationException()
        Catch ex As Exception When getFilter()
            Return "1"
        Catch ex As Exception
            Return ""
        End Try
    End Function

    <Fact>
    Public Sub QueryClausesTreatNothingAsFalse()
        Dim values As Integer?() = {1, Nothing, 3, 2}
        Dim limit As Integer? = 2
        Assert.Equal({1, 2}, (From v In values Where v <= limit Select v.Value).ToArray())
        Assert.Equal({1}, (From v In values Take While v <= limit Select v.Value).ToArray())
        Assert.Equal(New Integer?() {Nothing, 3, 2}, (From v In values Skip While v <= limit).ToArray())
        Assert.Equal(New Integer?() {1}, (From v In values Take While Not v > limit).ToArray())
    End Sub
End Class
