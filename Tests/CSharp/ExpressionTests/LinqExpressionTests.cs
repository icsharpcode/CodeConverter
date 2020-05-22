using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests
{
    public class LinqExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task Linq1Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Private Shared Sub SimpleQuery()
    Dim numbers = {7, 9, 5, 3, 6}
    Dim res = From n In numbers Where n > 5 Select n
    For Each n In res
        Console.WriteLine(n)
    Next
End Sub",
                @"private static void SimpleQuery()
{
    var numbers = new[] { 7, 9, 5, 3, 6 };
    var res = from n in numbers
              where n > 5
              select n;
    foreach (var n in res)
        Console.WriteLine(n);
}");
        }

        [Fact]
        public async Task Linq2Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Shared Sub Linq40()
    Dim numbers As Integer() = {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
    Dim numberGroups = From n In numbers Group n By __groupByKey1__ = n Mod 5 Into g = Group Select New With {Key .Remainder = __groupByKey1__, Key .Numbers = g}
    
    For Each g In numberGroups
        Console.WriteLine($""Numbers with a remainder of { g.Remainder} when divided by 5:"")

        For Each n In g.Numbers
            Console.WriteLine(n)
        Next
    Next
End Sub",
                @"public static void Linq40()
{
    var numbers = new[] { 5, 4, 1, 3, 9, 8, 6, 7, 2, 0 };
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
}");
        }

        [Fact()]
        public async Task Linq3Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class Product
    Public Category As String
    Public ProductName As String
End Class

Class Test

    Public Function GetProductList As Product()
        Return Nothing
    End Function

    Public Sub Linq102()
        Dim categories As String() = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
        Dim products As Product() = GetProductList()
        Dim q = From c In categories Join p In products On c Equals p.Category Select New With {Key .Category = c, p.ProductName}

        For Each v In q
            Console.WriteLine($""{v.ProductName}: {v.Category}"")
        Next
    End Sub
End Class",
                @"using System;
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
        var categories = new string[] { ""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood"" };
        var products = GetProductList();
        var q = from c in categories
                join p in products on c equals p.Category
                select new { Category = c, p.ProductName };
        foreach (var v in q)
            Console.WriteLine($""{v.ProductName}: {v.Category}"");
    }
}");
        }

        [Fact]
        public async Task Linq4Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class Product
    Public Category As String
    Public ProductName As String
End Class

Class Test
    Public Function GetProductList As Product()
        Return Nothing
    End Function

    Public Sub Linq103()
        Dim categories As String() = New String() {""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood""}
        Dim products = GetProductList()
        Dim q = From c In categories Group Join p In products On c Equals p.Category Into ps = Group Select New With {Key .Category = c, Key .Products = ps}

        For Each v In q
            Console.WriteLine(v.Category & "":"")

            For Each p In v.Products
                Console.WriteLine(""   "" & p.ProductName)
            Next
        Next
    End Sub
End Class", @"using System;
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
        var categories = new string[] { ""Beverages"", ""Condiments"", ""Vegetables"", ""Dairy Products"", ""Seafood"" };
        var products = GetProductList();
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
}");
        }

        [Fact]
        public async Task Linq5Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Private Shared Function FindPicFilePath(AList As List(Of FileInfo), picId As String) As String
    For Each FileInfo As FileInfo In From FileInfo1 In AList Where FileInfo1.Name.Substring(0, 6) = picId
        Return FileInfo.FullName
    Next
    Return String.Empty
End Function", @"private static string FindPicFilePath(List<FileInfo> AList, string picId)
{
    foreach (FileInfo FileInfo in from FileInfo1 in AList
                                  where (FileInfo1.Name.Substring(0, 6) ?? """") == (picId ?? """")
                                  select FileInfo1)
        return FileInfo.FullName;
    return string.Empty;
}");
        }

        [Fact]
        public async Task LinqAsEnumerableAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Data

Public Class AsEnumerableTest
    Public Sub FillImgColor()
        Dim dtsMain As New DataSet
        For Each i_ColCode As Integer In 
            From CurRow In dtsMain.Tables(""tb_Color"") Select CInt(CurRow.Item(""i_ColCode""))
        Next
    End Sub
End Class", @"using System.Data;
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
}");
        }

        [Fact]
        public async Task LinqMultipleFromsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Private Shared Sub LinqSub()
    Dim _result = From _claimProgramSummary In New List(Of List(Of List(Of List(Of String))))()
                  From _claimComponentSummary In _claimProgramSummary.First()
                  From _lineItemCalculation In _claimComponentSummary.Last()
                  Select _lineItemCalculation
End Sub", @"private static void LinqSub()
{
    var _result = from _claimProgramSummary in new List<List<List<List<string>>>>()
                  from _claimComponentSummary in _claimProgramSummary.First()
                  from _lineItemCalculation in _claimComponentSummary.Last()
                  select _lineItemCalculation;
}");
        }

        [Fact]
        public async Task LinqNoFromsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class VisualBasicClass
    Public Shared Sub X(objs As List(Of Object))
        Dim MaxObj As Integer = Aggregate o In objs Into Max(o.GetHashCode())
        Dim CountWhereObj As Integer = Aggregate o In objs Where o.GetHashCode() > 3 Into Count()
    End Sub
End Class", @"using System.Collections.Generic;
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
}");
        }

        [Fact]
        public async Task LinqPartitionDistinctAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Private Shared Function FindPicFilePath() As IEnumerable(Of String)
    Dim words = {""an"", ""apple"", ""a"", ""day"", ""keeps"", ""the"", ""doctor"", ""away""}

    Return From word In words
            Skip 1
            Skip While word.Length >= 1
            Take While word.Length < 5
            Take 2
            Distinct
End Function", @"private static IEnumerable<string> FindPicFilePath()
{
    var words = new[] { ""an"", ""apple"", ""a"", ""day"", ""keeps"", ""the"", ""doctor"", ""away"" };
    return words.Skip(1).SkipWhile(word => word.Length >= 1).TakeWhile(word => word.Length < 5).Take(2).Distinct();
}");
        }

        [Fact(Skip = "Issue #29 - Aggregate not supported")]
        public async Task LinqAggregateSumAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Private Shared Sub ASub()
    Dim expenses() As Double = {560.0, 300.0, 1080.5, 29.95, 64.75, 200.0}
    Dim totalExpense = Aggregate expense In expenses Into Sum()
End Sub", @"private static void ASub()
{
    double[] expenses = {560.0, 300.0, 1080.5, 29.95, 64.75, 200.0};
    var totalExpense = expenses.Sum();
}");
        }

        [Fact(Skip = "Issue #29 - Group join not supported")]
        public async Task LinqGroupJoinAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Private Shared Sub ASub()
    Dim customerList = From cust In customers
                       Group Join ord In orders On
                       cust.CustomerID Equals ord.CustomerID
                       Into CustomerOrders = Group,
                            OrderTotal = Sum(ord.Total)
                       Select cust.CompanyName, cust.CustomerID,
                              CustomerOrders, OrderTotal
End Sub", @"private static void ASub()
{
    var customerList = from cust in customers
                       join ord in orders on cust.CustomerID equals ord.CustomerID into CustomerOrders
                       let OrderTotal = Sum(ord.Total) //TODO Figure out exact C# syntax for this query
                       select new { cust.CompanyName, cust.CustomerID, CustomerOrders, OrderTotal };
}");
        }

        [Fact()]
        public async Task LinqGroupByTwoThingsAnonymouslyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
    Sub Foo()
        Dim xs As New List(Of String)
        Dim y = From x In xs Group By x.Length, x.Count() Into Group
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;

public partial class Class1
{
    public void Foo()
    {
        var xs = new List<string>();
        var y = from x in xs
                group x by new { x.Length, Count = x.Count() };
    }
}");
            // Current characterization is slightly wrong, I think it still needs this on the end "into g select new { Length = g.Key.Length, Count = g.Key.Count, Group = g.AsEnumerable() }"
        }

        [Fact]
        public async Task LinqGroupByAnonymousAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Runtime.CompilerServices ' Removed by simplifier

Public Class AccountEntry
    Public Property LookupAccountEntryTypeId As Object
    Public Property LookupAccountEntrySourceId As Object
    Public Property SponsorId As Object
    Public Property LookupFundTypeId As Object
    Public Property StartDate As Object
    Public Property SatisfiedDate As Object
    Public Property InterestStartDate As Object
    Public Property ComputeInterestFlag As Object
    Public Property SponsorClaimRevision As Object
    Public Property Amount As Decimal
    Public Property AccountTransactions As List(Of Object)
    Public Property AccountEntryClaimDetails As List(Of AccountEntry)
End Class

Module Ext
    <Extension>
    Public Function Reduce(ByVal accountEntries As IEnumerable(Of AccountEntry)) As IEnumerable(Of AccountEntry)
        Return (
            From _accountEntry In accountEntries
                Where _accountEntry.Amount > 0D
                Group By _keys = New With
                    {
                    Key .LookupAccountEntryTypeId = _accountEntry.LookupAccountEntryTypeId,
                    Key .LookupAccountEntrySourceId = _accountEntry.LookupAccountEntrySourceId,
                    Key .SponsorId = _accountEntry.SponsorId,
                    Key .LookupFundTypeId = _accountEntry.LookupFundTypeId,
                    Key .StartDate = _accountEntry.StartDate,
                    Key .SatisfiedDate = _accountEntry.SatisfiedDate,
                    Key .InterestStartDate = _accountEntry.InterestStartDate,
                    Key .ComputeInterestFlag = _accountEntry.ComputeInterestFlag,
                    Key .SponsorClaimRevision = _accountEntry.SponsorClaimRevision
                    } Into Group
                Select New AccountEntry() With
                    {
                    .LookupAccountEntryTypeId = _keys.LookupAccountEntryTypeId,
                    .LookupAccountEntrySourceId = _keys.LookupAccountEntrySourceId,
                    .SponsorId = _keys.SponsorId,
                    .LookupFundTypeId = _keys.LookupFundTypeId,
                    .StartDate = _keys.StartDate,
                    .SatisfiedDate = _keys.SatisfiedDate,
                    .ComputeInterestFlag = _keys.ComputeInterestFlag,
                    .InterestStartDate = _keys.InterestStartDate,
                    .SponsorClaimRevision = _keys.SponsorClaimRevision,
                    .Amount = Group.Sum(Function(accountEntry) accountEntry.Amount),
                    .AccountTransactions = New List(Of Object)(),
                    .AccountEntryClaimDetails =
                        (From _accountEntry In Group From _claimDetail In _accountEntry.AccountEntryClaimDetails
                            Select _claimDetail).Reduce().ToList
                    }
            )
    End Function
End Module", @"using System.Collections.Generic;
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
               where _accountEntry.Amount > 0M
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
}");
        }
    }
}
