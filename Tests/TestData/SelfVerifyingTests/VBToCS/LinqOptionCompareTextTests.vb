Option Compare Text

Imports System
Imports System.Linq
Imports Xunit

''' <summary>
''' Option Compare Text only affects string comparison operators, not the equality and ordering used by query operators themselves
''' </summary>
Public Class LinqOptionCompareTextTests

    Private Shared ReadOnly Words As String() = {"a", "A", "b", "B"}

    <Fact>
    Sub TestWhereEqualityIgnoresCase()
        Assert.Equal(2, (From w In Words Where w = "a").Count())
    End Sub

    <Fact>
    Sub TestWhereInequalityIgnoresCase()
        Assert.Equal(2, (From w In Words Where w <> "A").Count())
    End Sub

    <Fact>
    Sub TestWhereRelationalComparisonIgnoresCase()
        Assert.Equal(2, (From w In Words Where w < "B").Count())
    End Sub

    <Fact>
    Sub TestLetComparisonIgnoresCase()
        Dim q = From w In Words Let isA = (w = "a") Where isA
        Assert.Equal(2, q.Count())
    End Sub

    <Fact>
    Sub TestGroupByIsCaseSensitive()
        Assert.Equal(4, (From w In Words Group By w Into Count()).Count())
    End Sub

    <Fact>
    Sub TestDistinctIsCaseSensitive()
        Assert.Equal(4, (From w In Words Distinct).Count())
    End Sub

    <Fact>
    Sub TestJoinIsCaseSensitive()
        Assert.Equal(1, (From w In Words Join v In {"a"} On w Equals v).Count())
    End Sub

    <Fact>
    Sub TestOrderByUsesDefaultComparer()
        Assert.Equal("a,A,b,B", String.Join(",", From w In {"b", "A", "a", "B"} Order By w))
    End Sub

    <Fact>
    Sub TestQueryContainingAggregateOverQueryableIgnoresCase()
        Dim queryable = {1, 2}.AsQueryable()
        Assert.Equal(1, (From w In {"A", "b"} Where w = "a" Aggregate i In queryable Into Sum(i) Select w).Count())
    End Sub

    <Fact>
    Sub TestQueryContainingExpressionTreeLambdaIgnoresCase()
        Dim queryable = Words.AsQueryable()
        Assert.Equal(2, (From w In Words Where w = "a" AndAlso queryable.Any(Function(z) z.Length = w.Length)).Count())
    End Sub

End Class
