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

    <Theory>
    <InlineData(False, 0, False, 0, 3)>
    <InlineData(True, 0, True, 3, 0)>
    <InlineData(True, 1, False, 0, 3)>
    Public Sub NullableStructureWithWideningToBooleanConditions(hasValue As Boolean, value As Integer, expectedIf As Boolean, expectedWhile As Integer, expectedDoUntil As Integer)
        Dim s As ZeroIsTrue? = If(hasValue, New ZeroIsTrue With {.Value = value}, CType(Nothing, ZeroIsTrue?))
        Dim result = False
        If s Then result = True
        Assert.Equal(expectedIf, result)

        Dim count = 0
        While (s)
            count += 1
            If count = 3 Then Exit While
        End While
        Assert.Equal(expectedWhile, count)

        count = 0
        Do Until s
            count += 1
            If count = 3 Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)

        Dim calls = 0
        Dim getS = Function()
                       calls += 1
                       Return s
                   End Function
        result = False
        If getS() Then result = True
        Assert.Equal(expectedIf, result)
        Assert.Equal(1, calls)
    End Sub

    <Theory>
    <InlineData(False, False, False)>
    <InlineData(True, False, False)>
    <InlineData(True, True, True)>
    Public Sub NullableStructureConditionalExpressionConditions(useFirst As Boolean, firstHasValue As Boolean, expected As Boolean)
        Dim first As ZeroIsTrue? = If(firstHasValue, New ZeroIsTrue, CType(Nothing, ZeroIsTrue?))
        Dim second As ZeroIsTrue? = Nothing
        Dim result = False
        If If(useFirst, first, second) Then result = True
        Assert.Equal(expected, result)

        result = False
        If If(If(useFirst, first, second), second) Then result = True
        Assert.Equal(expected, result)

        Dim count = 0
        Do Until If(useFirst, first, second)
            count += 1
            If count = 3 Then Exit Do
        Loop
        Assert.Equal(If(expected, 0, 3), count)

        count = 0
        Do Until If(If(useFirst, first, second), second)
            count += 1
            If count = 3 Then Exit Do
        Loop
        Assert.Equal(If(expected, 0, 3), count)
    End Sub

    <Theory>
    <InlineData(Nothing, False, 3)>
    <InlineData(True, True, 0)>
    <InlineData(False, False, 3)>
    Public Sub StructureWithWideningToNullableBooleanConditions(converted As Boolean?, expectedIf As Boolean, expectedDoUntil As Integer)
        Dim ns As New ToNullableBoolean With {.Converted = converted}
        Dim s As ToNullableBoolean? = ns
        Dim result = False
        If ns Then result = True
        Assert.Equal(expectedIf, result)

        result = False
        If s Then result = True
        Assert.Equal(expectedIf, result)

        Dim count = 0
        Do Until s
            count += 1
            If count = 3 Then Exit Do
        Loop
        Assert.Equal(expectedDoUntil, count)
    End Sub

    <Theory>
    <InlineData(True, Nothing, 1)>
    <InlineData(True, 1, 2)>
    <InlineData(True, 2, 1)>
    <InlineData(False, 1, 2)>
    <InlineData(False, 2, 1)>
    <InlineData(False, 1, Nothing)>
    Public Sub NullableBooleanTernaryConditions(f As Boolean, x As Integer?, y As Integer?)
        Dim expected = x.HasValue AndAlso y.HasValue AndAlso If(f, x.Value < y.Value, x.Value > y.Value)
        Dim result = False
        If If(f, x < y, x > y) Then result = True
        Assert.Equal(expected, result)

        Dim count = 0
        Do Until If(f, x < y, x > y)
            count += 1
            If count = 3 Then Exit Do
        Loop
        Assert.Equal(If(expected, 0, 3), count)
    End Sub

    <Theory>
    <InlineData(True, Nothing)>
    <InlineData(True, True)>
    <InlineData(True, False)>
    <InlineData(False, Nothing)>
    <InlineData(False, True)>
    <InlineData(False, False)>
    Public Sub NullableBooleanTernaryUntilConditions(f As Boolean, b As Boolean?)
        Dim notB As Boolean? = Not b
        Dim expectedEitherWay = If(f, b, notB).GetValueOrDefault()
        Dim count = 0
        Do Until If(f, b, Not b)
            count += 1
            If count = 3 Then Exit Do
        Loop
        Assert.Equal(If(expectedEitherWay, 0, 3), count)

        count = 0
        Do
            count += 1
            If count = 3 Then Exit Do
        Loop Until If(f, b, b)
        Assert.Equal(If(b.GetValueOrDefault(), 1, 3), count)
    End Sub

    <Fact>
    Public Sub NothingStructureWithWideningToNullableBooleanConditionThrows()
        Dim s As ToNullableBoolean? = Nothing
        Assert.Throws(Of InvalidOperationException)(Sub()
                                                        If s Then Return
                                                    End Sub)
        Assert.Throws(Of InvalidOperationException)(Sub()
                                                        Do Until s : Exit Do : Loop
                                                    End Sub)
    End Sub
End Class

''' <summary>
''' VB doesn't lift a conversion to Boolean? for a Nothing operand
''' </summary>
Public Structure ToNullableBoolean
    Public Converted As Boolean?

    Public Shared Widening Operator CType(s As ToNullableBoolean) As Boolean?
        Return s.Converted
    End Operator
End Structure

''' <summary>
''' Its default value converts to True, so a condition that is Nothing must become False without converting the default
''' </summary>
Public Structure ZeroIsTrue
    Public Value As Integer

    Public Shared Widening Operator CType(s As ZeroIsTrue) As Boolean
        Return s.Value = 0
    End Operator
End Structure
