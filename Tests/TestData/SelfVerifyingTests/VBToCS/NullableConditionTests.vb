Imports System
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
End Class
