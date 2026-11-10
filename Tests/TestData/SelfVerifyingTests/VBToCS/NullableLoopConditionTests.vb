Imports System
Imports Xunit

''' <summary>
''' VB converts a Boolean? loop condition to Boolean with Nothing becoming False, before deciding whether to continue (While) or stop (Until).
''' So a While loop whose condition is Nothing doesn't run, and an Until loop whose condition is Nothing isn't done.
''' Each loop stops after <see cref="MaxIterations"/> so that a condition that never ends the loop shows up as that count.
''' </summary>
Public Class NullableLoopConditionTests
    Private Const MaxIterations As Integer = 3

    <Theory>
    <InlineData(Nothing, 1, 0)>
    <InlineData(1, Nothing, 0)>
    <InlineData(Nothing, Nothing, 0)>
    <InlineData(1, 2, 3)>
    <InlineData(2, 1, 0)>
    Public Sub WhileLessThan(a As Integer?, b As Integer?, expected As Integer)
        Dim count = 0
        While a < b
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expected, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, 0)>
    <InlineData(Nothing, Nothing, 0)>
    <InlineData(1, 1, 3)>
    <InlineData(2, 1, 0)>
    Public Sub WhileEquals(a As Integer?, b As Integer?, expected As Integer)
        Dim count = 0
        While (a = b)
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expected, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, 0)>
    <InlineData(Nothing, Nothing, 0)>
    <InlineData(1, 1, 0)>
    <InlineData(2, 1, 3)>
    Public Sub DoWhileNotEquals(a As Integer?, b As Integer?, expected As Integer)
        Dim count = 0
        Do While a <> b
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expected, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, 3)>
    <InlineData(1, Nothing, 3)>
    <InlineData(Nothing, Nothing, 3)>
    <InlineData(1, 2, 0)>
    <InlineData(2, 1, 3)>
    Public Sub DoUntilLessThan(a As Integer?, b As Integer?, expected As Integer)
        Dim count = 0
        Do Until a < b
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expected, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, 1)>
    <InlineData(1, 2, 3)>
    <InlineData(2, 1, 1)>
    Public Sub LoopWhileGreaterThanOrEqual(a As Integer?, b As Integer?, expected As Integer)
        Dim count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop While b >= a
        Assert.Equal(expected, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, 3)>
    <InlineData(1, 2, 1)>
    <InlineData(2, 1, 3)>
    Public Sub LoopUntilGreaterThan(a As Integer?, b As Integer?, expected As Integer)
        Dim count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop Until b > a
        Assert.Equal(expected, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 0, 0, 3, 1, 3, 3)>
    <InlineData(True, 3, 0, 0, 3, 1, 3)>
    <InlineData(False, 0, 3, 3, 1, 3, 1)>
    Public Sub BooleanVariable(flag As Boolean?, expectedWhile As Integer, expectedWhileNot As Integer, expectedDoUntil As Integer, expectedLoopWhile As Integer, expectedLoopUntil As Integer, expectedLoopUntilNot As Integer)
        Dim count = 0
        While flag
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expectedWhile, count)

        count = 0
        While Not flag
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expectedWhileNot, count)

        count = 0
        Do Until flag
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)

        count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop While flag
        Assert.Equal(expectedLoopWhile, count)

        count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop Until flag
        Assert.Equal(expectedLoopUntil, count)

        count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop Until Not flag
        Assert.Equal(expectedLoopUntilNot, count)
    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, 0, 3)>
    <InlineData(Nothing, 1, 0, 3)>
    <InlineData(True, Nothing, 0, 0)>
    <InlineData(True, 1, 3, 0)>
    <InlineData(False, 1, 0, 3)>
    Public Sub CombinedWithLogicalOperators(flag As Boolean?, a As Integer?, expectedWhileAndAlso As Integer, expectedUntilOrElse As Integer)
        Dim count = 0
        While flag AndAlso a < 2
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expectedWhileAndAlso, count)

        count = 0
        Do Until flag OrElse a > 2
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expectedUntilOrElse, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 1, 3, 3)>
    <InlineData(1, 2, 3, 1)>
    <InlineData(2, 1, 0, 3)>
    Public Sub UntilNotComparison(a As Integer?, b As Integer?, expectedDoUntil As Integer, expectedLoopUntil As Integer)
        Dim count = 0
        Do Until Not (a < b)
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)

        count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop Until Not a >= b
        Assert.Equal(expectedLoopUntil, count)
    End Sub

    <Fact>
    Public Sub ConditionWithSideEffectsEvaluatedOncePerIteration()
        Dim log As New System.Text.StringBuilder()
        Dim values As Integer?() = {1, 2, Nothing, 3}
        Dim index = 0
        Dim nextValue = Function()
                            log.Append("n")
                            index += 1
                            Return values(index - 1)
                        End Function
        Dim limit = Function() As Integer?
                        log.Append("l")
                        Return 3
                    End Function
        While nextValue() < limit()
            log.Append("b")
        End While
        Assert.Equal("nlbnlbnl", log.ToString())

        log.Clear()
        index = 0
        Dim limitValue As Integer? = 3
        Do Until nextValue() = limitValue
            log.Append("b")
        Loop
        Assert.Equal("nbnbnbn", log.ToString())
    End Sub

    <Fact>
    Public Sub WhileStopsWhenComparisonBecomesNothing()
        Dim currentDate As Date? = New Date(2000, 1, 1)
        Dim endDate As Date? = currentDate.Value.AddMinutes(2)
        Dim count = 0
        While currentDate <= endDate
            count += 1
            If count = 2 Then currentDate = Nothing Else currentDate = currentDate.Value.AddMinutes(1)
        End While
        Assert.Equal(2, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 2)>
    <InlineData(1, 1)>
    Public Sub TernaryIfOperator(a As Integer?, expected As Integer)
        Assert.Equal(expected, If(a < 2, 1, 2))
    End Sub
End Class
