Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Xunit


Friend Class CTst
    Public Shared Property GetOperatorResult As Func(Of CTst, CTst, Boolean)

    Public Shared Operator <>(ByVal aCls1 As CTst, ByVal aCls2 As CTst) As Boolean
        Return Not GetOperatorResult()(aCls1, aCls2)
    End Operator
    Public Shared Operator =(ByVal aCls1 As CTst, ByVal aCls2 As CTst) As Boolean
        Return GetOperatorResult()(aCls1, aCls2)
    End Operator
End Class

Public Class ObjectEqualityTests

    <Fact>
    Public Sub ComparingToNothingDoesNotCallOperatorOverload()
        Dim Test1 As CTst

        CTst.GetOperatorResult = Function(a, b)
                                     Assert.Empty("operator should not be called")
                                     Return False
                                 End Function
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

    <Fact>
    Public Sub ConstrainedGenericCanBeEquatedToNothing()
        Dim callCount = 0
        CTst.GetOperatorResult = Function(a, b)
                                     callCount += 1
                                     Return Object.ReferenceEquals(a, b)
                                 End Function
        GenericEquatable(New CTst)
        Assert.Equal(callCount, 4)
    End Sub

    Private Sub GenericEquatable(Of T As CTst)(input As T)
        Assert.False(input = Nothing)
        Assert.True(input <> Nothing)
        input = Nothing
        Assert.True(input = Nothing)
        Assert.False(input <> Nothing)
    End Sub
End Class