Imports System
Imports System.Linq
Imports Xunit

''' <summary>
''' A query compiled to delegates (rather than an expression tree for a provider like EF) runs VB's comparison semantics
''' </summary>
Public Class LinqComparisonTests

    Private Shared ReadOnly Numbers As Integer?() = {1, 2, Nothing}
    Private Shared ReadOnly Words As String() = {"", Nothing, "a"}

    <Fact>
    Sub TestNullableEquals()
        Assert.Equal(1, (From n In Numbers Where n = 1).Count())
    End Sub

    <Fact>
    Sub TestNullableNotEquals()
        Assert.Equal(1, (From n In Numbers Where n <> 1).Count())
    End Sub

    <Fact>
    Sub TestNullableGreaterThan()
        Assert.Equal(1, (From n In Numbers Where n > 1).Count())
    End Sub

    <Fact>
    Sub TestBothNullableEquals()
        Dim other As Integer? = Nothing
        Assert.Equal(0, (From n In Numbers Where n = other).Count())
    End Sub

    <Fact>
    Sub TestNotNullableEquals()
        Assert.Equal(1, (From n In Numbers Where Not (n = 1)).Count())
    End Sub

    <Fact>
    Sub TestNullableAndAlso()
        Assert.Equal(1, (From n In Numbers Where n > 1 AndAlso n < 3).Count())
    End Sub

    <Fact>
    Sub TestNullableOrElse()
        Assert.Equal(2, (From n In Numbers Where n < 2 OrElse n > 1).Count())
    End Sub

    Private _calls As Integer

    Private Function Called(value As Boolean) As Boolean
        _calls += 1
        Return value
    End Function

    <Fact>
    Sub TestNullableAndAlsoStillCallsRightWhenLeftIsNothing()
        Dim q = (From n In Numbers Where n > 1 AndAlso Called(True)).ToList()
        Assert.Equal(1, q.Count)
        Assert.Equal(2, _calls)
    End Sub

    <Fact>
    Sub TestEmptyStringEqualsNothing()
        Assert.Equal(2, (From w In Words Where w = "").Count())
    End Sub

    <Fact>
    Sub TestStringVariableEqualsNothing()
        Dim s As String = Nothing
        Assert.Equal(2, (From w In Words Where w = s).Count())
    End Sub

    <Fact>
    Sub TestNullableSelectedComparison()
        Dim q = (From n In Numbers Select isOne = (n = 1)).ToList()
        Assert.Equal("True,False,", String.Join(",", q))
    End Sub


    Private Shared ReadOnly Flags As Boolean?() = {True, Nothing, True}

    <Fact>
    Sub TestTakeWhileNullableBoolean()
        Assert.Equal(1, (From f In Flags Take While f).Count())
    End Sub

    <Fact>
    Sub TestNullableBooleanAndAlso()
        Assert.Equal(2, (From f In Flags Where f AndAlso True).Count())
    End Sub

    <Fact>
    Sub TestNullableBooleanOrElse()
        Assert.Equal(2, (From f In Flags Where f OrElse False).Count())
    End Sub

    Private ReadOnly Property Check As Boolean
        Get
            _calls += 1
            Return True
        End Get
    End Property

    Private Function CheckCall() As Boolean
        _calls += 1
        Return True
    End Function

    <Fact>
    Sub TestNullableAndAlsoCallWithoutParenthesesWhenLeftIsNothing()
        Dim q = (From n In Numbers Where n > 1 AndAlso CheckCall).ToList()
        Assert.Equal(1, q.Count)
        Assert.Equal(2, _calls)
    End Sub

    <Fact>
    Sub TestIfNullableAndAlsoCallWithoutParenthesesWhenLeftIsNothing()
        Dim n As Integer? = Nothing
        If n = 1 AndAlso CheckCall Then _calls += 10
        Assert.Equal(1, _calls)
    End Sub

    <Fact>
    Sub TestAggregateCountWithNullablePredicate()
        Dim nums = New Integer?() {1, 2, 3}
        Dim c = Aggregate n In nums Into Count(n > 1)
        Assert.Equal(2, c)
    End Sub

    <Fact>
    Sub TestGroupByAnyWithNullablePredicate()
        Dim nums = New Integer?() {1, 2, 3}
        Dim q = From n In nums Group By k = n Mod 2 Into Any(n = 1)
        Assert.Equal(2, q.Count())
    End Sub

    <Fact>
    Sub TestIfNullableAndAlsoThrowingRightWhenLeftIsNothing()
        Dim n As Integer? = Nothing
        Dim z = 0
        Assert.Throws(Of DivideByZeroException)(Sub()
                                                     If n = 1 AndAlso 1 \ z = 0 Then _calls += 1
                                                 End Sub)
    End Sub
End Class
