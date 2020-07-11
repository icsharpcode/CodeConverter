Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Xunit


Friend Class CTst
    Public Shared Operator <>(ByVal aCls1 As CTst, ByVal aCls2 As CTst) As Boolean
        Assert.Empty("<> operator should not be called")
        Return True
    End Operator
    Public Shared Operator =(ByVal aCls1 As CTst, ByVal aCls2 As CTst) As Boolean
        Assert.Empty("= operator should not be called")
        Return True
    End Operator
End Class

Public Class ObjectEqualityTests

    <Fact>
    Public Sub ComparingToNothingDoesNotCallOperatorOverload()
        Dim Test1 As CTst

        If Not Test1 Is Nothing Then
            Assert.Empty("First branch should not be taken")
        ElseIf Test1 IsNot Nothing Then
            Assert.Empty("Second branch should not be taken")
        ElseIf Test1 Is Nothing Then
            Assert.True(Test1 Is Nothing)
            Assert.False(Not Test1 Is Nothing)
            Assert.False(Test1 IsNot Nothing)
        Else
            Assert.Empty("Else branch should not be taken")
        End If
    End Sub

    <Fact>
    Public Sub GenericCanBeAssignedNothing()
        Generic(5, False)
        Generic("5", True)
    End Sub

    Private Sub Generic(Of T)(input As T, isReference As Boolean)
        input = Nothing
        If isReference Then Assert.True(input Is Nothing) Else Assert.True(input IsNot Nothing)
    End Sub
End Class