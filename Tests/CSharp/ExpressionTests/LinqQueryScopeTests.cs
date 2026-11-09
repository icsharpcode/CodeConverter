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
                      orderby v ascending
                      select new { Value = v, Name = Enum.GetName(typeof(Field), v) }).ToArray();
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
                   Order By x Descending
                   Select y
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public void M()
    {
        var data = from x in new[] { 1, 2, 3 }
                   orderby x descending
                   select x * 2;
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
        Return From x As Long In ints Select x * 2
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class C
{
    public IEnumerable<long> M(int[] ints)
    {
        return from x in ints.Select(x => (long)x)
               select x * 2L;
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
}
