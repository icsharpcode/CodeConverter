Option Strict Off

Imports System
Imports Xunit

''' <summary>
''' With Option Strict Off, a condition of any type convertible to Boolean is allowed.
''' VB converts a nullable condition to Boolean? first, so Nothing becomes False rather than throwing.
''' Each loop stops after <see cref="MaxIterations"/> so that a condition that never ends the loop shows up as that count.
''' </summary>
Public Class NullableConditionOptionStrictOffTests
    Private Const MaxIterations As Integer = 3

    <Theory>
    <InlineData(Nothing, False, 0, 3)>
    <InlineData(0, False, 0, 3)>
    <InlineData(2, True, 3, 0)>
    Public Sub NullableIntegerConditions(x As Integer?, expectedIf As Boolean, expectedWhile As Integer, expectedDoUntil As Integer)
        Dim result = False
        If x Then result = True
        Assert.Equal(expectedIf, result)

        Dim count = 0
        While x
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expectedWhile, count)

        count = 0
        Do Until x
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)

        count = 0
        Do
            count += 1
            If count = MaxIterations Then Exit Do
        Loop While (x)
        Assert.Equal(If(expectedWhile = 0, 1, 3), count)
    End Sub

    <Theory>
    <InlineData(Nothing, False)>
    <InlineData(0.0, False)>
    <InlineData(0.5, True)>
    Public Sub NullableDoubleIfCondition(x As Double?, expected As Boolean)
        Dim result = False
        If x Then
            result = True
        End If
        Assert.Equal(expected, result)
    End Sub

    <Fact>
    Public Sub NullableToBooleanOutsideConditionsThrowsForNothing()
        Dim x As Integer? = Nothing
        Assert.Throws(Of InvalidOperationException)(Sub()
                                                        Dim b As Boolean = x
                                                    End Sub)
        Assert.Throws(Of InvalidOperationException)(Sub()
                                                        If IsTrue(x) Then Return
                                                    End Sub)
        Assert.Throws(Of InvalidOperationException)(Sub()
                                                        If CBool(x) Then Return
                                                    End Sub)
    End Sub

    Private Shared Function IsTrue(b As Boolean) As Boolean
        Return b
    End Function

    <Theory>
    <InlineData(False, 0, False, 3)>
    <InlineData(True, 0, True, 0)>
    <InlineData(True, 1, False, 3)>
    Public Sub NullableStructureWithWideningToBooleanConditions(hasValue As Boolean, value As Integer, expectedIf As Boolean, expectedDoUntil As Integer)
        Dim s As ZeroIsTrueStrictOff? = If(hasValue, New ZeroIsTrueStrictOff With {.Value = value}, CType(Nothing, ZeroIsTrueStrictOff?))
        Dim result = False
        If s Then result = True
        Assert.Equal(expectedIf, result)

        Dim count = 0
        Do Until s
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 0, 3)>
    <InlineData("True", 3, 0)>
    <InlineData(2, 3, 0)>
    Public Sub ObjectConditions(o As Object, expectedWhile As Integer, expectedDoUntil As Integer)
        Dim count = 0
        While o
            count += 1
            If count = MaxIterations Then Exit While
        End While
        Assert.Equal(expectedWhile, count)

        count = 0
        Do Until o
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)
    End Sub

    <Theory>
    <InlineData(Nothing, 3)>
    <InlineData("0", 3)>
    <InlineData(2, 0)>
    Public Sub ObjectComparisonCondition(o As Object, expected As Integer)
        Dim count = 0
        Do Until o > 1
            count += 1
            If count = MaxIterations Then Exit Do
        Loop
        Assert.Equal(expected, count)
    End Sub
End Class

Public Structure ZeroIsTrueStrictOff
    Public Value As Integer

    Public Shared Widening Operator CType(s As ZeroIsTrueStrictOff) As Boolean
        Return s.Value = 0
    End Operator
End Structure
