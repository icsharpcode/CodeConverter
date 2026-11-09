using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LinqQueryAggregateTests : ConverterTestBase
{
    [Fact]
    public async Task AggregateWithMultipleAggregationsOverSimpleSourceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim agg = Aggregate x In xs Into s = Sum(x), c = Count()
        Console.WriteLine(agg.s + agg.c)
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var agg = new { s = xs.Sum(x => x), c = xs.Count() };
        Console.WriteLine(agg.s + agg.c);
    }
}");
    }

    [Fact]
    public async Task AggregateWithMultipleAggregationsEvaluatesSourceOnceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim agg = Aggregate x In xs Where x > 1 Into s = Sum(x), c = Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var agg = new[] { from x in xs where x > 1 select x }.Select(Group => new { s = Group.Sum(x => x), c = Group.Count() }).Single();
    }
}");
    }

    [Fact]
    public async Task AggregateClauseWithinQueryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Order
    Public Amount As Decimal
End Class
Public Class Customer
    Public Name As String
    Public Orders As New List(Of Order)
End Class
Public Class C
    Public Sub M(customers As List(Of Customer))
        Dim q = From c In customers
                Aggregate o In c.Orders Into Total = Sum(o.Amount)
                Where Total > 10
                Select c.Name, Total
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Order
{
    public decimal Amount;
}

public partial class Customer
{
    public string Name;
    public List<Order> Orders = new List<Order>();
}

public partial class C
{
    public void M(List<Customer> customers)
    {
        var q = from c in customers
                let Total = c.Orders.Sum(o => o.Amount)
                where Total > 10m
                select new { c.Name, Total };
    }
}");
    }

    [Fact]
    public async Task AggregateClauseOnlyUsedInImplicitSelectIsInlinedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Aggregate y In xs Where y > x Into Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x in xs
                select new { x, Count = (from y in xs where y > x select y).Count() };
    }
}");
    }

    [Fact]
    public async Task AggregateClauseWithMultipleAggregationsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Aggregate y In xs Where y > x Into Sum(y), Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x in xs
                let Group = from y in xs
                            where y > x
                            select y
                select new { x, Sum = Group.Sum(y => y), Count = Group.Count() };
    }
}");
    }

    /// <summary>
    /// A provider only sees lambdas, so the first source, a join's inner source and partition counts are ordinary code, as are an Aggregate clause's within a query compiled to delegates
    /// </summary>
    [Fact]
    public async Task ExpressionTreeBoundariesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq
Public Class C
    Sub M(q As IQueryable(Of String), q2 As IQueryable(Of String), list As String(), s As String)
        Dim g2 = From o In q Aggregate c In o Take If(o = s, 1, 2) Into Count() Select o, Count
        Dim g3 = Aggregate o In q Into Count(o = s)
        Dim g4 = From x In list Aggregate o In q Into Count(o = x) Where x = """" Select x
        Dim g5 = From o In q Aggregate i In list Into Count(i = o) Where o = s Select o
        Dim g6 = From o In q Aggregate i In q2 Into Count(i = o) Select o
        Dim g7a = From o In q.Take(If(s = """", 1, 2)) Select o
        Dim g7b = From o In q, i In If(s = """", list, list) Select o
        Dim g7c = From o In q Join i In If(s = """", list, list) On o Equals i Select o
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(IQueryable<string> q, IQueryable<string> q2, string[] list, string s)
    {
        var g2 = from o in q
                 select new { o, Count = o.Take(o == s ? 1 : 2).Count() };
        int g3 = q.Count(o => o == s);
        var g4 = from x in list
                 let Count = q.Count(o => o == x)
                 where string.IsNullOrEmpty(x)
                 select x;
        var g5 = from o in q
                 let Count = list.Count(i => i == o)
                 where o == s
                 select o;
        var g6 = from o in q
                 let Count = q2.Count(i => i == o)
                 select o;
        var g7a = from o in q.Take(string.IsNullOrEmpty(s) ? 1 : 2)
                  select o;
        var g7b = from o in q
                  from i in s == """" ? list : list
                  select o;
        var g7c = from o in q
                  join i in string.IsNullOrEmpty(s) ? list : list on o equals i
                  select o;
    }
}");
    }
}
