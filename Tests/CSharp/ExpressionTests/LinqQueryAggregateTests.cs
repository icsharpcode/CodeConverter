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
}
