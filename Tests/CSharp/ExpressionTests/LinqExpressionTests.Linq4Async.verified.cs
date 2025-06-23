using System;
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
        string[] categories = new string[] { "Beverages", "Condiments", "Vegetables", "Dairy Products", "Seafood" };
        Product[] products = GetProductList();
        var q = from c in categories
                join p in products on c equals p.Category into ps
                select new { Category = c, Products = ps };

        foreach (var v in q)
        {
            Console.WriteLine(v.Category + ":");

            foreach (var p in v.Products)
                Console.WriteLine("   " + p.ProductName);
        }
    }
}