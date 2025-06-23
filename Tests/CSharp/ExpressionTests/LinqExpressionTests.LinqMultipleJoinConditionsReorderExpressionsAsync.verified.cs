using System.Collections.Generic;
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
}