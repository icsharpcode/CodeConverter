using System.Data.SqlClient;

internal partial class TestClass
{
    private void Save()
    {
        using (var cmd = new SqlCommand())
        {
            cmd.ExecuteNonQuery();
            cmd?.ExecuteNonQuery();
            cmd.ExecuteNonQuery();
            cmd?.ExecuteNonQuery();
        }
    }
}