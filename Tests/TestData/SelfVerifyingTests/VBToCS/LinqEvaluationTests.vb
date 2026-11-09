Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Xunit

''' <summary>
''' VB evaluates a Let or Select value once for each element reaching that clause, in order. Substituting the value elsewhere must not change that.
''' </summary>
Public Class LinqEvaluationTests
    Private ReadOnly _log As New List(Of String)
    Private Function Log(Of T)(tag As String, value As T) As T
        _log.Add(tag & value.ToString())
        Return value
    End Function
    Private ReadOnly Property Logged As String
        Get
            Return String.Join(",", _log)
        End Get
    End Property

    <Fact>
    Sub TestLetUsedInFinalSelectAfterOrderByIsEvaluatedInSourceOrder()
        Dim q = (From x In {3, 1, 2} Let y = Log("y", x) Order By x Select y).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestUnusedLetIsEvaluated()
        Dim q = (From x In {3, 1, 2} Let y = Log("y", x) Select x).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetUsedInFinalSelectAfterWhereIsEvaluatedBeforeFiltering()
        Dim q = (From x In {3, 1, 2} Let y = Log("y", x) Where x > 1 Select y).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetUsedInWhere()
        Dim q = (From x In {3, 1, 2} Let y = Log("y", x) Where y > 1 Select x).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetUsedInFinalSelectLambdaIsEvaluatedOnce()
        Dim q = (From x In {3, 1} Let y = Log("y", x) Select f = New Func(Of Integer)(Function() y)).ToList()
        Dim r = q.Select(Function(f) f()).Sum() + q.Select(Function(f) f()).Sum()
        Assert.Equal("y3,y1", Logged)
    End Sub

    <Fact>
    Sub TestSelectBeforeTakeIsEvaluatedForTakenElements()
        Dim q = (From x In {3, 1, 2} Select y = Log("y", x) Take 2).ToList()
        Assert.Equal("y3,y1", Logged)
    End Sub

    <Fact>
    Sub TestLetBeforeDistinctIsEvaluatedForEachElement()
        Dim q = (From x In {3, 3, 2} Let y = Log("y", x) Distinct Select y).ToList()
        Assert.Equal("y3,y3,y2", Logged)
    End Sub

    <Fact>
    Sub TestGroupByAggregateUsedTwiceIsEvaluatedOncePerGroup()
        Dim q = (From x In {1, 2, 3} Group By k = x Mod 2 Into s = Sum(Log("s", x)) Where s > 0 Select s, t = s).ToList()
        Assert.Equal("s1,s3,s2", Logged)
    End Sub

    <Fact>
    Sub TestSelectBeforeOrderByIsEvaluatedInSourceOrder()
        Dim q = (From x In {3, 1, 2} Select y = Log("y", x) Order By y).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetUsedInConditionalInFinalSelectIsEvaluatedForEachElement()
        Dim q = (From x In {3, 1, 2} Let y = Log("y", x) Select If(x > 1, y, 0)).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetUsedInShortCircuitInFinalSelectIsEvaluatedForEachElement()
        Dim q = (From x In {3, 1, 2} Let y = Log("y", x > 2) Select x > 1 AndAlso y).ToList()
        Assert.Equal("yTrue,yFalse,yFalse", Logged)
    End Sub

    <Fact>
    Sub TestUnnamedSelectBeforeWhereIsEvaluatedForEachElement()
        Dim flag = False
        Dim q = (From x In {3, 1, 2} Select Log("y", x) Where flag Distinct).ToList()
        Assert.Equal("y3,y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetDroppedByLaterSelectIsEvaluated()
        Dim q = (From x In {1, 2} Let y = Log("y", x) Select z = x Distinct).ToList()
        Assert.Equal("y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestAggregationDroppedByLaterSelectIsEvaluated()
        Dim q = (From x In {1, 2} Group By k = x Mod 2 Into s = Sum(Log("s", x)) Select k Distinct).ToList()
        Assert.Equal("s1,s2", Logged)
    End Sub

    <Fact>
    Sub TestLetsSelectedInReverseOrderAreEvaluatedInDeclarationOrder()
        Dim q = (From x In {1, 2} Let y = Log("y", x), z = Log("z", x) Select z, y).ToList()
        Assert.Equal("y1,z1,y2,z2", Logged)
    End Sub

    <Fact>
    Sub TestLetIsEvaluatedBeforeCallEarlierInFinalSelect()
        Dim q = (From x In {1, 2} Let y = Log("y", x) Select Log("g", x) + y).ToList()
        Assert.Equal("y1,g1,y2,g2", Logged)
    End Sub

    <Fact>
    Sub TestAggregationsSelectedInReverseOrderAreEvaluatedInDeclarationOrder()
        Dim q = (From x In {1, 2} Group By k = 0 Into a = Sum(Log("a", x)), b = Sum(Log("b", x)) Select b, a).ToList()
        Assert.Equal("a1,a2,b1,b2", Logged)
    End Sub

    <Fact>
    Sub TestLetLambdaInvoked()
        Dim q = From x In {1, 2} Let f = Function(a As Integer) a + x Select f(10)
        Assert.Equal("11,12", String.Join(",", q))
    End Sub

    Private Function G() As Integer
        Return Log("g", 0)
    End Function

    <Fact>
    Sub TestLetIsEvaluatedBeforeCallWithoutParenthesesEarlierInFinalSelect()
        Dim q = (From x In {1, 2} Let y = Log("y", x) Select a = G, b = y).ToList()
        Assert.Equal("y1,g0,y2,g0", Logged)
    End Sub

    <Fact>
    Sub TestNameOfLetDoesNotEvaluateIt()
        Dim q = (From x In {1, 2} Let y = Log("y", x) Select NameOf(y)).ToList()
        Assert.Equal("y,y", String.Join(",", q))
        Assert.Equal("y1,y2", Logged)
    End Sub

    <Fact>
    Sub TestLetKeptBetweenSubstitutedLetAndItsUse()
        Dim q = (From x In {1, 2} Let a = Log("a", x) Let b = Log("b", x) Select a).ToList()
        Assert.Equal("a1,b1,a2,b2", Logged)
    End Sub

    <Fact>
    Sub TestLetKeptBetweenLetAndDistinct()
        Dim q = (From x In {1, 2} Let a = Log("a", x) Let b = Log("b", x) Let c = b + b Distinct).ToList()
        Assert.Equal("a1,b1,a2,b2", Logged)
    End Sub

    <Fact>
    Sub TestAggregationKeptBetweenSubstitutedAggregationAndItsUse()
        Dim q = (From x In {1, 2} Group By k = 0 Into a = Sum(Log("a", x)), b = Sum(Log("b", x)) Select a, c = b + b).ToList()
        Assert.Equal("a1,a2,b1,b2", Logged)
    End Sub

    <Fact>
    Sub TestNameOfMemberOfLet()
        Dim q = (From x In {1} Let a = Log("a", x) Select n = NameOf(a.ToString)).ToList()
        Assert.Equal("ToString", q.Single())
        Assert.Equal("a1", Logged)
    End Sub
End Class
