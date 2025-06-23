using System.Data;

public partial class Issue765
{
    public void GetByName(IDataReader dataReader)
    {
        object foo;
        foo = dataReader["foo"];
    }
}