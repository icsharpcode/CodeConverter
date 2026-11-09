using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

/// <summary>
/// VB and C# differ in which range variables are in scope after each clause, and what the query returns when there's no final select.
/// </summary>
public class LinqQueryScopeTests : ConverterTestBase
{
    [Fact]
    public async Task Issue1163_SelectFollowedByWhereAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From d In {1.2, 2.2, 3.4, 5.6}
                   Select i = CType(Math.Floor(d), Integer)
                   Where i Mod 2 = 1
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from d in new[] { 1.2d, 2.2d, 3.4d, 5.6d }
                   let i = (int)Math.Round(Math.Floor(d))
                   where i % 2 == 1
                   select i;
    }
}");
    }

    [Fact]
    public async Task Issue1172_SelectFollowedByOrderByAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Enum Field
        A
        B
    End Enum
    Private _DisplayFields As Field() = {Field.B, Field.A}
    Public Sub M()
        Dim fields = (From v In _DisplayFields Select Value = v, Name = [Enum].GetName(GetType(Field), v) Order By Value Ascending).ToArray
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public enum Field
    {
        A,
        B
    }
    private Field[] _DisplayFields = new[] { Field.B, Field.A };
    public void M()
    {
        var fields = (from v in _DisplayFields
                      let Name = Enum.GetName(typeof(Field), v)
                      orderby v ascending
                      select new { Value = v, Name }).ToArray();
    }
}");
    }

    [Fact]
    public async Task SelectFollowedByWhereReferencingRenamedVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Select x = x * 2
                   Where x > 2
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }
                   let x1 = x * 2
                   where x1 > 2
                   select x1;
    }
}");
    }

    [Fact]
    public async Task SelectedValueOnlyUsedInFinalSelectIsInlinedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Let y = x * 2
                   Select y
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }
                   select x * 2;
    }
}");
    }

    [Fact]
    public async Task ValueOnlyUsedInFinalSelectAfterOrderByIsKeptInLetToKeepEvaluationOrderAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Let y = x * 2
                   Order By x Descending
                   Select y
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }
                   let y = x * 2
                   orderby x descending
                   select y;
    }
}");
    }

    [Fact]
    public async Task UnusedValueIsKeptInLetSinceVbEvaluatesItAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Let y = x.ToString()
                   Select x
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }
                   let y = x.ToString()
                   select x;
    }
}");
    }

    [Fact]
    public async Task ImplicitSelectOfMultipleRangeVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   From y In {""a"", ""b""}
                   Let z = x.ToString() & y
                   Where z.Length > 1
        For Each item In data
            Console.WriteLine(item.x & item.y & item.z)
        Next
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }
                   from y in new[] { ""a"", ""b"" }
                   let z = x.ToString() + y
                   where z.Length > 1
                   select new { x, y, z };
        foreach (var item in data)
            Console.WriteLine(item.x + item.y + item.z);
    }
}");
    }

    [Fact]
    public async Task DistinctAfterSelectOfMultipleRangeVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Select a = x Mod 2, b = x Mod 3
                   Distinct
                   Where a > 0
                   Select b
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x1 in (from x in new[] { 1, 2, 3 }
                               select new { a = x % 2, b = x % 3 }).Distinct()
                   where x1.a > 0
                   select x1.b;
    }
}");
    }

    [Fact]
    public async Task TakeWhileAfterSelectOfMultipleRangeVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Select x, y = x * 2
                   Take While y < 4
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = (from x in new[] { 1, 2, 3 }
                    select new { x, y = x * 2 }).TakeWhile(x1 => x1.y < 4);
    }
}");
    }

    [Fact]
    public async Task SkipThenWhereContinuesQueryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim data = From x In {1, 2, 3}
                   Skip 1
                   Where x > 2
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }.Skip(1)
                   where x > 2
                   select x;
    }
}");
    }

    [Fact]
    public async Task TypedFromWithReferenceConversionUsesCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Function M(objects As Object()) As IEnumerable(Of String)
        Return From c As C In objects Select s = c.ToString()
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class C
{
    public IEnumerable<string> M(object[] objects)
    {
        return from C c in objects
               select c.ToString();
    }
}");
    }

    [Fact]
    public async Task TypedFromWithValueConversionSelectsConvertedValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Function M(ints As Integer()) As IEnumerable(Of Long)
        Return From n As Long In ints Select n * 2
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class C
{
    public IEnumerable<long> M(int[] ints)
    {
        return from n in ints.Select(n => (long)n)
               select n * 2L;
    }
}");
    }

    [Fact]
    public async Task FromNonGenericCollectionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Function M(list As System.Collections.ArrayList) As IEnumerable(Of Object)
        Return From x In list Where x IsNot Nothing
    End Function
End Class", @"using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class C
{
    public IEnumerable<object> M(ArrayList list)
    {
        return from object x in list
               where x != null
               select x;
    }
}");
    }

    [Fact]
    public async Task ExplicitIdentitySelectIsKeptAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(arr As Integer())
        Dim q = From x In arr Select x
        q = q.Where(Function(v) v > 1)
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] arr)
    {
        var q = from x in arr
                select x;
        q = q.Where(v => v > 1);
    }
}");
    }

    [Fact]
    public async Task GeneratedNamesAvoidEnclosingLambdaParametersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim f = Function(x As Integer) (From a In {1, 2}
                                        From b In {3}
                                        Distinct
                                        Where a > x).Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M()
    {
        int f(int x) => (from x1 in (from a in new[] { 1, 2 } from b in new[] { 3 } select new { a, b }).Distinct() where x1.a > x select x1).Count();
    }
}");
    }

    [Fact]
    public async Task SubstitutionAvoidsLambdaParameterWithSameNameAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(ys As Integer())
        Dim q = From x In {1, 5}
                Select y = x
                Where ys.Any(Function(x) x > y)
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] ys)
    {
        var q = from x in new[] { 1, 5 }
                let y = x
                where ys.Any(x => x > y)
                select y;
    }
}");
    }

    [Fact]
    public async Task TypedFromOverNonGenericCollectionCastsThenConvertsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Function M(list As System.Collections.ArrayList) As IEnumerable(Of Integer)
        Return From x As Integer In list Select x + 1
    End Function
End Class", @"using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public IEnumerable<int> M(ArrayList list)
    {
        return from x in list.Cast<object>().Select(x => Conversions.ToInteger(x))
               select x + 1;
    }
}");
    }

    [Fact]
    public async Task TypedFromWithVariableNamedAsTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Structure S
    Public Value As Integer
End Structure
Public Class C
    Public Function M(objects As Object()) As IEnumerable(Of Integer)
        Return From S As S In objects Select S.Value
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial struct S
{
    public int Value;
}

public partial class C
{
    public IEnumerable<int> M(object[] objects)
    {
        return from S in objects.Select(x => (S)x)
               select S.Value;
    }
}");
    }

    [Fact]
    public async Task ValueReferencedBeforeDistinctUsesLetAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Let y = x * 2
                Where y > 2
                Distinct
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = (from x in xs
                 let y = x * 2
                 where y > 2
                 select new { x, y }).Distinct();
    }
}");
    }

    [Fact]
    public async Task FinalSelectReferenceWithinLambdaUsesLetAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer(), ys As Integer())
        Dim q = From x In xs
                Let y = x * 2
                Select ys.Count(Function(z) z > y)
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs, int[] ys)
    {
        var q = from x in xs
                let y = x * 2
                select ys.Count(z => z > y);
    }
}");
    }

    [Fact]
    public async Task EarlierLambdaParameterWithSameNameDoesNotPreventSubstitutionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer(), ys As Integer())
        Dim q = From x In xs
                Where ys.Any(Function(y) y > x)
                Select y = x
                Where y > 0
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] xs, int[] ys)
    {
        var q = from x in xs
                where ys.Any(y => y > x)
                where x > 0
                select x;
    }
}");
    }

    [Fact]
    public async Task MethodCallFollowedByFinalSelectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Select a = x Mod 2, b = x
                Distinct
                Select b
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x1 in (from x in xs
                            select new { a = x % 2, b = x }).Distinct()
                select x1.b;
    }
}");
    }

    [Fact]
    public async Task GroupByFollowedByMethodCallThenMoreClausesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Group By k = x Mod 2 Into Count()
                Distinct
                Where Count > 1
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x1 in (from x in xs
                            group x by x % 2 into Group
                            select new { k = Group.Key, Count = Group.Count() }).Distinct()
                where x1.Count > 1
                select x1;
    }
}");
    }

    [Fact]
    public async Task TwoGroupBysContinueInOrderAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Group By k = x Mod 4 Into Count()
                Group By Count Into Keys = Group
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x in xs
                group x by x % 4 into Group
                let Count = Group.Count()
                group new { k = Group.Key, Count } by Count into Keys
                select new { Count = Keys.Key, Keys };
    }
}");
    }

    [Fact]
    public async Task MaterializedCompoundElementHasNoBlankLineAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer(), ys As Integer())
        Dim q = From x In xs
                From y In ys
                Distinct
                Where x > y
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] xs, int[] ys)
    {
        var q = from x1 in (from x in xs
                            from y in ys
                            select new { x, y }).Distinct()
                where x1.x > x1.y
                select x1;
    }
}");
    }

    [Fact]
    public async Task GeneratedElementNameAvoidsMemberNamesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From a In xs
                Group By x = a Mod 2 Into Count()
                Distinct
                Where x > 0
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x1 in (from a in xs
                            group a by a % 2 into Group
                            select new { x = Group.Key, Count = Group.Count() }).Distinct()
                where x1.x > 0
                select x1;
    }
}");
    }

    [Fact]
    public async Task LaterLambdaWithUnrelatedParameterDoesNotPreventSubstitutionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer(), ys As Integer())
        Dim q = From x In xs
                Select y = x
                Where ys.Any(Function(z) z > y)
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] xs, int[] ys)
    {
        var q = from x in xs
                where ys.Any(z => z > x)
                select x;
    }
}");
    }

    [Fact]
    public async Task LaterLambdaParameterMatchingAMemberNameDoesNotPreventSubstitutionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Item
    Public Name As String
End Class
Public Class C
    Public Sub M(items As Item(), names As String())
        Dim q = From item In items
                Select n = item.Name
                Where names.Any(Function(Name) Name = n)
    End Sub
End Class", @"using System.Linq;

public partial class Item
{
    public string Name;
}

public partial class C
{
    public void M(Item[] items, string[] names)
    {
        var q = from item in items
                where names.Any(Name => (Name ?? """") == (item.Name ?? """"))
                select item.Name;
    }
}");
    }

    [Fact]
    public async Task NestedQueryGeneratedNameAvoidsEnclosingRangeVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Select Smaller = (From a In xs, b In xs Distinct Where a < x).Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x in xs
                select (from x1 in (from a in xs
                                    from b in xs
                                    select new { a, b }).Distinct()
                        where x1.a < x
                        select x1).Count();
    }
}");
    }

    [Fact]
    public async Task ComparisonAfterQueryGetsVisualBasicSemanticsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Function M(xs As String(), s As String) As Boolean
        Dim q = From x In xs Where x = s
        Return s = """"
    End Function
End Class", @"using System.Linq;

public partial class C
{
    public bool M(string[] xs, string s)
    {
        var q = from x in xs
                where x == s
                select x;
        return string.IsNullOrEmpty(s);
    }
}");
    }

    [Fact]
    public async Task NestedQueryWithinExpressionTreeQueryKeepsSimpleComparisonAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Option Compare Text
Imports System.Linq

Public Class C
    Public Function M(xs As IQueryable(Of String), ys As IQueryable(Of String)) As IQueryable(Of String)
        Return From x In xs Where (From y In ys Where y = x).Any()
    End Function
End Class", @"using System.Linq;

public partial class C
{
    public IQueryable<string> M(IQueryable<string> xs, IQueryable<string> ys)
    {
        return from x in xs
               where (from y in ys
                      where y == x
                      select y).Any()
               select x;
    }
}", incompatibleWithAutomatedCommentTesting: true); // Option statement comments are lost
    }

    [Fact]
    public async Task NestedQueryGeneratedNameAvoidsRenamedEnclosingRangeVariablesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As Integer())
        Dim q = From x In xs
                Select x = x * 2
                Where (From a In xs, b In xs Distinct Where a < x).Any()
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M(int[] xs)
    {
        var q = from x in xs
                let x1 = x * 2
                where (from x2 in (from a in xs
                                   from b in xs
                                   select new { a, b }).Distinct()
                       where x2.a < x1
                       select x2).Any()
                select x1;
    }
}");
    }
}
