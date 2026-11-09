using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LinqQueryJoinTests : ConverterTestBase
{
    private const string EntityClasses = @"
Public Class MyEntity
    Property EntityId As Integer
    Property FavoriteColorId As Integer
    Property Name As String
End Class
Public Class MyColor
    Property ColorId As Integer
    Property Color As String
End Class";

    private const string CsEntityClasses = @"
public partial class MyEntity
{
    public int EntityId { get; set; }
    public int FavoriteColorId { get; set; }
    public string Name { get; set; }
}

public partial class MyColor
{
    public int ColorId { get; set; }
    public string Color { get; set; }
}";

    [Fact]
    public async Task Issue936_JoinConditionWithComputedOuterKeyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Policy
    Property Policy_ID As Integer
    Property Reinsurance_Year As Integer
End Class
Public Class C
    Public Function M(policies As List(Of Policy)) As IEnumerable(Of Integer)
        Return From pCurrent In policies
               Join pPrevious In policies On pCurrent.Reinsurance_Year - 1 Equals pPrevious.Reinsurance_Year
               Select pPrevious.Policy_ID
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Policy
{
    public int Policy_ID { get; set; }
    public int Reinsurance_Year { get; set; }
}

public partial class C
{
    public IEnumerable<int> M(List<Policy> policies)
    {
        return from pCurrent in policies
               join pPrevious in policies on pCurrent.Reinsurance_Year - 1 equals pPrevious.Reinsurance_Year
               select pPrevious.Policy_ID;
    }
}");
    }

    [Fact]
    public async Task JoinConditionWrittenInnerFirstIsSwappedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Policy
    Property Policy_ID As Integer
    Property Reinsurance_Year As Integer
End Class
Public Class C
    Public Function M(policies As List(Of Policy)) As IEnumerable(Of Integer)
        Return From pCurrent In policies
               Join pPrevious In policies On (pPrevious.Reinsurance_Year) Equals pCurrent.Reinsurance_Year - 1
               Select pPrevious.Policy_ID
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Policy
{
    public int Policy_ID { get; set; }
    public int Reinsurance_Year { get; set; }
}

public partial class C
{
    public IEnumerable<int> M(List<Policy> policies)
    {
        return from pCurrent in policies
               join pPrevious in policies on pCurrent.Reinsurance_Year - 1 equals pPrevious.Reinsurance_Year
               select pPrevious.Policy_ID;
    }
}");
    }

    [Fact]
    public async Task Issue1016_OrderByAfterSelectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(EntityClasses + @"
Public Class C
    Public Sub M()
        Dim entities As New List(Of MyEntity)
        Dim colors As New List(Of MyColor)

        Dim wrongOrder = (From e In entities
                          Join c In colors On c.ColorId Equals e.FavoriteColorId
                          Where e.FavoriteColorId > 0 And c.Color = ""green""
                          Select e, c
                          Order By e.Name).ToList
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;
" + CsEntityClasses + @"

public partial class C
{
    public void M()
    {
        var entities = new List<MyEntity>();
        var colors = new List<MyColor>();

        var wrongOrder = (from e in entities
                          join c in colors on e.FavoriteColorId equals c.ColorId
                          where e.FavoriteColorId > 0 & c.Color == ""green""
                          orderby e.Name
                          select new { e, c }).ToList();
    }
}");
    }

    [Fact]
    public async Task Issue931_GroupJoinWithoutSelectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(EntityClasses + @"
Public Class C
    Public Sub M()
        Dim entities As New List(Of MyEntity)
        Dim colors As New List(Of MyColor)

        Dim data = (From e In entities
                    Group Join c In colors On c.ColorId Equals e.FavoriteColorId Into Group1 = Group
                    From g1 In Group1.DefaultIfEmpty).ToList

        If data.Count > 0 Then
            Dim firstEntityId = data.First().e.EntityId
            Dim firstColorId = data.First().g1.ColorId
            Dim firstGroupColorId = data.First().Group1.First().ColorId
        End If
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;
" + CsEntityClasses + @"

public partial class C
{
    public void M()
    {
        var entities = new List<MyEntity>();
        var colors = new List<MyColor>();

        var data = (from e in entities
                    join c in colors on e.FavoriteColorId equals c.ColorId into Group1
                    from g1 in Group1.DefaultIfEmpty()
                    select new { e, Group1, g1 }).ToList();

        if (data.Count > 0)
        {
            int firstEntityId = data.First().e.EntityId;
            int firstColorId = data.First().g1.ColorId;
            int firstGroupColorId = data.First().Group1.First().ColorId;
        }
    }
}");
    }

    [Fact]
    public async Task Issue935_GroupJoinIntoGroupThenDefaultIfEmptyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Policy
    Property Location_State_Code As String
    Property Agent_Entity_ID As Integer
    Property Reinsurance_Year As Short
End Class
Public Class StateRow
    Property State_Code As String
    Property State_Name As String
End Class
Public Class Entity
    Property Entity_ID As Integer
End Class
Public Class State
    Property State_Code As String
    Property State_Name As String
End Class
Public Class C
    Public Shared Function Get_List_of_States(policies As List(Of Policy), states As List(Of StateRow), entities As List(Of Entity), Reinsurance_Year As Int16, Optional Agent_Entity_ID As Integer = 0) As List(Of State)
        Dim results As List(Of State) = (From p In policies
                                         Join st In states On p.Location_State_Code Equals st.State_Code
                                         Group Join agent In entities On agent.Entity_ID Equals p.Agent_Entity_ID Into Group
                                         From agent In Group.DefaultIfEmpty()
                                         Where p.Reinsurance_Year = Reinsurance_Year And (Agent_Entity_ID = 0 Or agent.Entity_ID = Agent_Entity_ID)
                                         Order By st.State_Name
                                         Select New State With {
                                             .State_Code = st.State_Code,
                                             .State_Name = st.State_Name
                                         }
                                         Distinct).ToList
        Return results
    End Function
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Policy
{
    public string Location_State_Code { get; set; }
    public int Agent_Entity_ID { get; set; }
    public short Reinsurance_Year { get; set; }
}

public partial class StateRow
{
    public string State_Code { get; set; }
    public string State_Name { get; set; }
}

public partial class Entity
{
    public int Entity_ID { get; set; }
}

public partial class State
{
    public string State_Code { get; set; }
    public string State_Name { get; set; }
}

public partial class C
{
    public static List<State> Get_List_of_States(List<Policy> policies, List<StateRow> states, List<Entity> entities, short Reinsurance_Year, int Agent_Entity_ID = 0)
    {
        var results = (from p in policies
                       join st in states on p.Location_State_Code equals st.State_Code
                       join agent in entities on p.Agent_Entity_ID equals agent.Entity_ID into Group
                       from agent in Group.DefaultIfEmpty()
                       where p.Reinsurance_Year == Reinsurance_Year & (Agent_Entity_ID == 0 | agent.Entity_ID == Agent_Entity_ID)
                       orderby st.State_Name
                       select new State()
                       {
                           State_Code = st.State_Code,
                           State_Name = st.State_Name
                       }).Distinct().ToList();
        return results;
    }
}");
    }

    [Fact]
    public async Task GroupJoinWithAggregatesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Customer
    Public CustomerID As String
    Public CompanyName As String
End Class
Public Class Order
    Public CustomerID As String
    Public Total As Decimal
End Class
Public Class C
    Private Shared Sub M(customers As List(Of Customer), orders As List(Of Order))
        Dim customerList = From cust In customers
                           Group Join ord In orders On
                           cust.CustomerID Equals ord.CustomerID
                           Into CustomerOrders = Group,
                                OrderTotal = Sum(ord.Total)
                           Select cust.CompanyName, cust.CustomerID,
                                  CustomerOrders, OrderTotal
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Customer
{
    public string CustomerID;
    public string CompanyName;
}

public partial class Order
{
    public string CustomerID;
    public decimal Total;
}

public partial class C
{
    private static void M(List<Customer> customers, List<Order> orders)
    {
        var customerList = from cust in customers
                           join ord in orders on cust.CustomerID equals ord.CustomerID into CustomerOrders
                           select new { cust.CompanyName, cust.CustomerID, CustomerOrders, OrderTotal = CustomerOrders.Sum(ord => ord.Total) };
    }
}");
    }

    [Fact]
    public async Task GroupJoinAggregateWithoutGroupIsUsedLaterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer())
        Dim q = From x In xs
                Group Join y In ys On x Equals y Into Count()
                Where Count > 1
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys)
    {
        var q = from x in xs
                join y in ys on x equals y into Group
                let Count = Group.Count()
                where Count > 1
                select new { x, Count };
    }
}");
    }

    [Fact]
    public async Task NestedJoinIsFlattenedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Join y In ys
                    Join z In zs On z Equals y
                On x Equals y
                Select x, y, z
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join y in ys on x equals y
                join z in zs on y equals z
                select new { x, y, z };
    }
}");
    }

    [Fact]
    public async Task TypedJoinVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Id As Integer
    Private Shared Sub M(xs As Integer(), objs As Object())
        Dim q = From x In xs Join c As C In objs On x Equals c.Id Select x, c
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    public int Id;
    private static void M(int[] xs, object[] objs)
    {
        var q = from x in xs
                join C c in objs on x equals c.Id
                select new { x, c };
    }
}");
    }

    [Fact]
    public async Task NestedJoinWithConditionOnBothJoinedVariablesJoinsSubqueryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Join y In ys
                    Join z In zs On z Equals y
                On x Equals y + z
                Select x, y, z
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join x1 in from y in ys
                           join z in zs on y equals z
                           select new { y, z } on x equals x1.y + x1.z
                select new { x, x1.y, x1.z };
    }
}");
    }

    [Fact]
    public async Task JoinNestedWithinGroupJoinJoinsSubqueryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Group Join y In ys
                    Join z In zs On z Equals y
                On x Equals y
                Into Group, Total = Sum(y + z)
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join x1 in from y in ys
                           join z in zs on y equals z
                           select new { y, z } on x equals x1.y into Group
                select new { x, Group, Total = Group.Sum(x2 => x2.y + x2.z) };
    }
}");
    }

    [Fact]
    public async Task GroupJoinNestedWithinJoinJoinsSubqueryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Join y In ys
                    Group Join z In zs On z Equals y Into c = Count()
                On x Equals y
                Where c > 0
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join x1 in from y in ys
                           join z in zs on y equals z into Group
                           select new { y, c = Group.Count() } on x equals x1.y
                where x1.c > 0
                select new { x, x1.y, x1.c };
    }
}");
    }

    [Fact]
    public async Task NestedJoinConditionSkippingAJoinedVariableJoinsSubqueryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Join y In ys
                    Join z In zs On z Equals y
                On x Equals z
                Select x, y, z
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join x1 in from y in ys
                           join z in zs on y equals z
                           select new { y, z } on x equals x1.z
                select new { x, x1.y, x1.z };
    }
}");
    }

    [Fact]
    public async Task NestedJoinConditionOnlyInvalidOnceBothVariablesAreInScopeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Join y In ys
                    Join z In zs On z Equals y
                On x Equals y And x Equals y + z
                Select x, y, z
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join x1 in from y in ys
                           join z in zs on y equals z
                           select new { y, z } on new { key0 = x, key1 = x } equals new { key0 = x1.y, key1 = x1.y + x1.z }
                select new { x, x1.y, x1.z };
    }
}");
    }

    [Fact]
    public async Task TwoJoinSubqueriesGetDistinctNamesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Private Shared Sub M(xs As Integer(), ys As Integer(), zs As Integer())
        Dim q = From x In xs
                Join y In ys
                    Join z In zs On z Equals y
                On x Equals y + z
                Join v In ys
                    Join w In zs On w Equals v
                On x Equals v + w
    End Sub
End Class", @"using System.Linq;

public partial class C
{
    private static void M(int[] xs, int[] ys, int[] zs)
    {
        var q = from x in xs
                join x1 in from y in ys
                           join z in zs on y equals z
                           select new { y, z } on x equals x1.y + x1.z
                join x2 in from v in ys
                           join w in zs on v equals w
                           select new { v, w } on x equals x2.v + x2.w
                select new { x, x1.y, x1.z, x2.v, x2.w };
    }
}");
    }

    [Fact]
    public async Task TypedGroupJoinVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class C
    Public Id As Integer
    Private Shared Sub M(xs As Integer(), objs As Object())
        Dim q = From x In xs
                Group Join c As C In objs On x Equals c.Id Into Count()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class C
{
    public int Id;
    private static void M(int[] xs, object[] objs)
    {
        var q = from x in xs
                join C c in objs on x equals c.Id into Group
                select new { x, Count = Group.Count() };
    }
}");
    }
}
