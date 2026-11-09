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
End Class
