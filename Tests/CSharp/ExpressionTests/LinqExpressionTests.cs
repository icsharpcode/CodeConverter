using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LinqExpressionTests : ConverterTestBase
{
    [Fact]
    public async Task Issue895_LinqWhereAfterGroupAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Collections.Generic;
using System.Linq;

public partial class Issue895
{
    private static void LinqWithGroup()
    {
        var numbers = new List<int>() { 1, 2, 3, 4, 4 };
        var duplicates = from x in numbers
                         group x by x into Group
                         where Group.Count() > 1
                         select Group;
        Console.WriteLine(duplicates.Count());
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Characterize_Issue948_GroupByMember_Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
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
                     group f by f.MyString into @group
                     orderby MyString
                     select @group;
    }
}
1 target compilation errors:
CS0103: The name 'MyString' does not exist in the current context", extension: "cs")
            );
        }
        // BUG: Order by should be on @group.Key
    }

    [Fact]
    public async Task Issue736_LinqEarlySelectAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
using System.Collections.Generic;
using System.Linq;

public partial class Issue635
{
    private object foo;
    private List<Issue635> l;
    private object listSelectWhere;

    public Issue635()
    {
        listSelectWhere = from t in
                              from t in l
                              select t.foo
                          where 1 == 2
                          select t;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue635_LinqDistinctOrderByAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
using System.Collections.Generic;
using System.Linq;

public partial class Issue635
{
    private List<int> l;
    private object listSortedDistinct;

    public Issue635()
    {
        listSortedDistinct = (from x in l
                              orderby x
                              select x).Distinct();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Linq1Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private static void SimpleQuery()
{
    int[] numbers = new[] { 7, 9, 5, 3, 6 };
    var res = from n in numbers
              where n > 5
              select n;
    foreach (var n in res)
        Console.WriteLine(n);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Linq2Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"public static void Linq40()
{
    int[] numbers = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
    var numberGroups = from n in numbers
                       group n by (n % 5) into g
                       let __groupByKey1__ = g.Key
                       select new { Remainder = __groupByKey1__, Numbers = g };

    foreach (var g in numberGroups)
    {
        Console.WriteLine($""Numbers with a remainder of {g.Remainder} when divided by 5:"");

        foreach (var n in g.Numbers)
            Console.WriteLine(n);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Linq3Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Linq;

internal partial class Product
{
    public string Category;
    public string ProductName;
}

internal partial class Test
{

    public Product[] GetProductList()
    {
        return null;
    }

    public void Linq102()
    {
        string[] categories = new string[] { ""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood"" };
        Product[] products = GetProductList();
        var q = from c in categories
                join p in products on c equals p.Category
                select new { Category = c, p.ProductName };

        foreach (var v in q)
            Console.WriteLine($""{v.ProductName}: {v.Category}"");
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Linq4Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Linq;

internal partial class Product
{
    public string Category;
    public string ProductName;
}

internal partial class Test
{
    public Product[] GetProductList()
    {
        return null;
    }

    public void Linq103()
    {
        string[] categories = new string[] { ""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood"" };
        Product[] products = GetProductList();
        var q = from c in categories
                join p in products on c equals p.Category into ps
                select new { Category = c, Products = ps };

        foreach (var v in q)
        {
            Console.WriteLine(v.Category + "":"");

            foreach (var p in v.Products)
                Console.WriteLine(""   "" + p.ProductName);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Linq5Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private static string FindPicFilePath(List<FileInfo> AList, string picId)
{
    foreach (FileInfo FileInfo in from FileInfo1 in AList
                                  where FileInfo1.Name.Substring(0, 6) == picId
                                  select FileInfo1)
        return FileInfo.FullName;
    return string.Empty;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqAsEnumerableAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Data;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class AsEnumerableTest
{
    public void FillImgColor()
    {
        var dtsMain = new DataSet();
        foreach (int i_ColCode in from CurRow in dtsMain.Tables[""tb_Color""].AsEnumerable()
                                  select Conversions.ToInteger(CurRow[""i_ColCode""]))
        {
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqMultipleFromsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private static void LinqSub()
{
    var _result = from _claimProgramSummary in new List<List<List<List<string>>>>()
                  from _claimComponentSummary in _claimProgramSummary.First()
                  from _lineItemCalculation in _claimComponentSummary.Last()
                  select _lineItemCalculation;
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqNoFromsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

public partial class VisualBasicClass
{
    public static void X(List<object> objs)
    {
        int MaxObj = objs.Max(o => o.GetHashCode());
        int CountWhereObj = (from o in objs
                             where o.GetHashCode() > 3
                             select o).Count();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqPartitionDistinctAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private static IEnumerable<string> FindPicFilePath()
{
    string[] words = new[] { ""an"", ""apple"", ""a"", ""day"", ""keeps"", ""the"", ""doctor"", ""away"" };

    return words.Skip(1).SkipWhile(word => word.Length >= 1).TakeWhile(word => word.Length < 5).Take(2).Distinct();
}", extension: "cs")
            );
        }
    }

    [Fact(Skip = "Issue #29 - Aggregate not supported")]
    public async Task LinqAggregateSumAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private static void ASub()
{
    double[] expenses = {560.0, 300.0, 1080.5, 29.95, 64.75, 200.0};
    var totalExpense = expenses.Sum();
}", extension: "cs")
            );
        }
    }

    [Fact(Skip = "Issue #29 - Group join not supported")]
    public async Task LinqGroupJoinAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private static void ASub()
{
    var customerList = from cust in customers
                       join ord in orders on cust.CustomerID equals ord.CustomerID into CustomerOrders
                       let OrderTotal = Sum(ord.Total) //TODO Figure out exact C# syntax for this query
                       select new { cust.CompanyName, cust.CustomerID, CustomerOrders, OrderTotal };
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqJoinReorderExpressionsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

internal partial class Customer
{
    public string CustomerID;
    public string CompanyName;
}

internal partial class Order
{
    public string CustomerID;
    public string Total;
}

internal partial class Test
{
    private static void ASub()
    {
        var customers = new List<Customer>();
        var orders = new List<Order>();
        var customerList = from cust in customers
                           join ord in orders on cust.CustomerID equals ord.CustomerID
                           select new { cust.CompanyName, ord.Total };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqMultipleJoinConditionsReorderExpressionsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

internal partial class Customer
{
    public string CustomerID;
    public string CompanyName;
}

internal partial class Order
{
    public string CustomerID;
    public string Total;
}

internal partial class Test
{
    private static void ASub()
    {
        var customers = new List<Customer>();
        var orders = new List<Order>();
        var customerList = from cust in customers
                           join ord in orders on new { key0 = cust.CustomerID, key1 = cust.CompanyName } equals new { key0 = ord.CustomerID, key1 = ord.Total }
                           select new { cust.CompanyName, ord.Total };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqMultipleIdentifierOnlyJoinConditionsReorderExpressionsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

internal partial class Customer
{
    public string CustomerID;
    public string CompanyName;
}

internal partial class Order
{
    public Customer Customer;
    public string Total;
}

internal partial class Test
{
    private static void ASub()
    {
        var customers = new List<Customer>();
        var orders = new List<Order>();
        var customerList = from cust in customers
                           join ord in orders on new { key0 = cust, key1 = cust.CompanyName } equals new { key0 = ord.Customer, key1 = ord.Total }
                           select new { cust.CompanyName, ord.Total };
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqGroupByTwoThingsAnonymouslyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var xs = new List<string>();
        var y = from x in xs
                group x by new { x.Length, Count = x.Count() };
    }
}", extension: "cs")
            );
        }
        // Current characterization is slightly wrong, I think it still needs this on the end "into g select new { Length = g.Key.Length, Count = g.Key.Count, Group = g.AsEnumerable() }"
    }

    [Fact]
    public async Task LinqSelectVariableDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
using System.Linq;

public partial class Class717
{
    public void Main()
    {
        var arr = new int[2];
        arr[0] = 0;
        arr[1] = 1;

        var r = from e in arr
                let p = $""value: {e}""
                let l = p.Substring(1)
                select l;

        foreach (var m in r)
            Console.WriteLine(m);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LinqGroupByAnonymousAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

public partial class AccountEntry
{
    public object LookupAccountEntryTypeId { get; set; }
    public object LookupAccountEntrySourceId { get; set; }
    public object SponsorId { get; set; }
    public object LookupFundTypeId { get; set; }
    public object StartDate { get; set; }
    public object SatisfiedDate { get; set; }
    public object InterestStartDate { get; set; }
    public object ComputeInterestFlag { get; set; }
    public object SponsorClaimRevision { get; set; }
    public decimal Amount { get; set; }
    public List<object> AccountTransactions { get; set; }
    public List<AccountEntry> AccountEntryClaimDetails { get; set; }
}

internal static partial class Ext
{
    public static IEnumerable<AccountEntry> Reduce(this IEnumerable<AccountEntry> accountEntries)
    {
        return from _accountEntry in accountEntries
               where _accountEntry.Amount > 0m
               group _accountEntry by new
               {
                   _accountEntry.LookupAccountEntryTypeId,
                   _accountEntry.LookupAccountEntrySourceId,
                   _accountEntry.SponsorId,
                   _accountEntry.LookupFundTypeId,
                   _accountEntry.StartDate,
                   _accountEntry.SatisfiedDate,
                   _accountEntry.InterestStartDate,
                   _accountEntry.ComputeInterestFlag,
                   _accountEntry.SponsorClaimRevision
               } into Group
               let _keys = Group.Key
               select new AccountEntry()
               {
                   LookupAccountEntryTypeId = _keys.LookupAccountEntryTypeId,
                   LookupAccountEntrySourceId = _keys.LookupAccountEntrySourceId,
                   SponsorId = _keys.SponsorId,
                   LookupFundTypeId = _keys.LookupFundTypeId,
                   StartDate = _keys.StartDate,
                   SatisfiedDate = _keys.SatisfiedDate,
                   ComputeInterestFlag = _keys.ComputeInterestFlag,
                   InterestStartDate = _keys.InterestStartDate,
                   SponsorClaimRevision = _keys.SponsorClaimRevision,
                   Amount = Group.Sum(accountEntry => accountEntry.Amount),
                   AccountTransactions = new List<object>(),
                   AccountEntryClaimDetails = (from _accountEntry in Group
                                               from _claimDetail in _accountEntry.AccountEntryClaimDetails
                                               select _claimDetail).Reduce().ToList()
               };
    }
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task LinqCommasToFromAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

public partial class VisualBasicClass
{
    public void Main()
    {
        var list1 = new List<int>() { 1, 2, 3 };
        var list2 = new List<int>() { 2, 4, 5 };

        var qs = from n in list1
                 from x in list2
                 where x == n
                 select new { x, n };
    }
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task Issue1011_LinqExpressionWithNullableCharacterizationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

public partial class ConversionTest2
{
    private partial class MyEntity
    {
        public int? FavoriteNumber { get; set; }
        public string Name { get; set; }
    }
    private void BugRepro()
    {

        var entities = new List<MyEntity>();

        string result = (from e in entities
                         where e.FavoriteNumber == 123
                         select e.Name).Single();

    }
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task AnExpressionTreeMayNotContainIsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System.Collections.Generic;
using System.Linq;

public partial class ConversionTest6
{
    private partial class MyEntity
    {
        public string Name { get; set; }
        public string FavoriteString { get; set; }
    }
    public void BugRepro()
    {

        var entities = new List<MyEntity>(); // If this was a DbSet from EFCore, then the 'is' below needs to be converted to == to avoid an error. Instead of detecting dbset, we'll just do this for all queries

        var data = (from e in entities
                    where e.Name == null || e.FavoriteString != null
                    select e).ToList();
    }
}", extension: "cs")
            );
        }
    }
}