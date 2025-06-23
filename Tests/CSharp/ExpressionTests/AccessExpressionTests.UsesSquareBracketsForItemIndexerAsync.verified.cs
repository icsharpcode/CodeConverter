using System.Data;

internal partial class TestClass
{
    public object GetItem(DataRow dr)
    {
        return dr["col1"];
    }
}