using System.Data;
using System.Linq;

internal partial class TestClass
{
    private readonly DataTable _myTable;

    public void TestMethod()
    {
        var dataRow = _myTable.AsEnumerable().ElementAtOrDefault(0);
    }
}