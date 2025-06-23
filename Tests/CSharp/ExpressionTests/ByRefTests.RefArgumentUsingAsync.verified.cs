using System.Data.SqlClient;

public partial class Class1
{
    public void Foo()
    {
        using (var x = new SqlConnection())
        {
            var argx = x;
            Bar(ref argx);
        }
    }
    public void Bar(ref SqlConnection x)
    {

    }
}