using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

/// <summary>
/// After a VB Group By, the range variables in scope are the keys and the aggregations. In C# the only range variable in scope is the group.
/// </summary>
public class LinqQueryGroupByTests : ConverterTestBase
{
    [Fact]
    public async Task Issue1272_GroupByMultipleKeysIntoAggregateAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Entity
    Public Property Property1 As String
    Public Property Property2 As String
    Public Property Value As Decimal
End Class
Public Class C
    Public Sub M(entityCollection As IEnumerable(Of Entity))
        Dim queryMax = From item In entityCollection
                       Where item.Property1 = ""dummy""
                       Group By item.Property1, item.Property2
                       Into ValueMax = Max(item.Value)
                       Select New With {
                           Property1, Property2, ValueMax
                       }
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Entity
{
    public string Property1 { get; set; }
    public string Property2 { get; set; }
    public decimal Value { get; set; }
}

public partial class C
{
    public void M(IEnumerable<Entity> entityCollection)
    {
        var queryMax = from item in entityCollection
                       where item.Property1 == ""dummy""
                       group item by new { item.Property1, item.Property2 } into Group
                       select new { Group.Key.Property1, Group.Key.Property2, ValueMax = Group.Max(item => item.Value) };
    }
}");
    }

    [Fact]
    public async Task Issue867_GroupByAggregateUsedInWhereAndSelectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C1
    Public Property C2p As C2
    Public Property A As Decimal
End Class
Public Class C2
    Public Property P1 As String
    Public Property P2 As String
    Public Property P3 As String
End Class
Public Class C
    Sub M(lst As List(Of C1))
        Dim tSummary = From t In lst
                       Group By t.C2p.P1, t.C2p.P2, t.C2p.P3 Into Summary = Sum(t.A)
                       Where Summary > 0
                       Select P1, P2, P3, Summary
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class C1
{
    public C2 C2p { get; set; }
    public decimal A { get; set; }
}

public partial class C2
{
    public string P1 { get; set; }
    public string P2 { get; set; }
    public string P3 { get; set; }
}

public partial class C
{
    public void M(List<C1> lst)
    {
        var tSummary = from t in lst
                       group t by new { t.C2p.P1, t.C2p.P2, t.C2p.P3 } into Group
                       let Summary = Group.Sum(t => t.A)
                       where Summary > 0m
                       select new { Group.Key.P1, Group.Key.P2, Group.Key.P3, Summary };
    }
}");
    }

    [Fact]
    public async Task Issue948_OrderByGroupKeyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Imports System.Linq

Class C
    Public Property MyString As String
End Class

Public Module Module1
    Public Sub Main()
        Dim list As New List(Of C)()
        Dim result = From f In list
                     Group f By f.MyString Into Group
                     Order By MyString
    End Sub
End Module", @"using System.Collections.Generic;
using System.Linq;

internal partial class C
{
    public string MyString { get; set; }
}

public static partial class Module1
{
    public static void Main()
    {
        var list = new List<C>();
        var result = from f in list
                     group f by f.MyString into Group
                     orderby Group.Key
                     select new { MyString = Group.Key, Group };
    }
}");
    }

    [Fact]
    public async Task Issue1014_GroupByMultiplePropertiesIntoNamedGroupAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class ConversionTest4
    Private Class MyEntity
        Property FavoriteNumber As Integer
        Property FavoriteColor As String
        Property Name As String
    End Class
    Private Sub BugRepro()
        Dim entities As New List(Of MyEntity)
        Dim result As IEnumerable(Of String) = From e In entities
                                               Group e By e.FavoriteNumber, e.FavoriteColor Into g1 = Group
                                               Select g1.FirstOrDefault().Name
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class ConversionTest4
{
    private partial class MyEntity
    {
        public int FavoriteNumber { get; set; }
        public string FavoriteColor { get; set; }
        public string Name { get; set; }
    }
    private void BugRepro()
    {
        var entities = new List<MyEntity>();
        var result = from e in entities
                     group e by new { e.FavoriteNumber, e.FavoriteColor } into g1
                     select g1.FirstOrDefault().Name;
    }
}");
    }

    [Fact]
    public async Task Issue298_GroupByNamedKeysSelectedIntoObjectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class P
    Public parentId As Integer
    Public ParentTypeDesc As String
End Class
Public Class PT
    Public parentId As Integer
    Public Sequence As Integer
End Class
Public Class C
    Sub M(_Parent As List(Of P), _ParentType As List(Of PT))
        Dim gridDataSource = (From p In _Parent
                              Join pt In _ParentType On pt.parentId Equals p.parentId
                              Group p By key = p.parentId, desc = p.ParentTypeDesc, sequence = pt.Sequence Into Group
                              Select New With {.parentId = key,
                                               .ParentTypeDesc = desc,
                                               .Sequence = sequence}).OrderBy(Function(c) c.Sequence)
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class P
{
    public int parentId;
    public string ParentTypeDesc;
}

public partial class PT
{
    public int parentId;
    public int Sequence;
}

public partial class C
{
    public void M(List<P> _Parent, List<PT> _ParentType)
    {
        var gridDataSource = (from p in _Parent
                              join pt in _ParentType on p.parentId equals pt.parentId
                              group p by new { key = p.parentId, desc = p.ParentTypeDesc, sequence = pt.Sequence } into Group
                              select new
                              {
                                  parentId = Group.Key.key,
                                  ParentTypeDesc = Group.Key.desc,
                                  Sequence = Group.Key.sequence
                              }).OrderBy(c => c.Sequence);
    }
}");
    }

    [Fact]
    public async Task Issue1180_WhereOnNamedGroupAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Dim ID As Integer
    Public Sub New(pId As Integer)
        ID = pId
    End Sub

    Public Sub M()
        Dim q = {New C(1), New C(2)}
        Dim result = From x in q Group By x.ID Into g = Group Where g.AsEnumerable.Count > 1 Select ID
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    private int ID;
    public C(int pId)
    {
        ID = pId;
    }

    public void M()
    {
        C[] q = new[] { new C(1), new C(2) };
        var result = from x in q
                     group x by x.ID into g
                     where g.AsEnumerable().Count() > 1
                     select g.Key;
    }
}");
    }

    [Fact]
    public async Task GroupItemsByKeyIntoAggregatesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Entity
    Public Property P1 As String
    Public Property P2 As String
    Public Property Value As Decimal
End Class
Public Class C
    Public Sub M(es As List(Of Entity))
        Dim q = From item In es
                Group v = item.Value, item.P1 By k = item.P2 Into s = Sum(v), Group, Count()
    End Sub
End Class", @"using System;
using System.Collections.Generic;
using System.Linq;

public partial class Entity
{
    public string P1 { get; set; }
    public string P2 { get; set; }
    public decimal Value { get; set; }
}

public partial class C
{
    public void M(List<Entity> es)
    {
        var q = from item in es
                group new { v = item.Value, item.P1 } by item.P2 into Group
                select new { k = Group.Key, s = Group.Sum(x => x.v), Group, Count = Group.Count() };
    }
}");
    }

    [Fact]
    public async Task GroupByCompoundElementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim q = From x In {1, 2}
                From y In {3, 4}
                Group By k = x Mod 2 Into Total = Sum(x * y)
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var q = from x in new[] { 1, 2 }
                from y in new[] { 3, 4 }
                group new { x, y } by x % 2 into Group
                select new { k = Group.Key, Total = Group.Sum(x1 => x1.x * x1.y) };
    }
}");
    }

    [Fact]
    public async Task GroupByWithoutGroupAggregationAvoidsNameClashAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M()
        Dim Group = 1
        Dim q = From x In {1, 2}
                Group By x Into Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M()
    {
        int Group = 1;
        var q = from x in new[] { 1, 2 }
                group x by x into Group1
                select new { x = Group1.Key, Count = Group1.Count() };
    }
}");
    }

    [Fact]
    public async Task AggregationUsedOnceInWhereIsSubstitutedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Sub M(xs As String())
        Dim q = From x In xs Group By x.Length Into Count() Where Count > 1 Select Length
        Dim r = From x In xs Group By x.Length Into Count() Order By Count Descending Select Length
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public void M(string[] xs)
    {
        var q = from x in xs
                group x by x.Length into Group
                where Group.Count() > 1
                select Group.Key;
        var r = from x in xs
                group x by x.Length into Group
                orderby Group.Count() descending
                select Group.Key;
    }
}");
    }
}
