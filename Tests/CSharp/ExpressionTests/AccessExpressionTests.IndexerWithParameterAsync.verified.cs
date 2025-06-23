using System.Data;

public partial class A
{
    public string ReadDataSet(DataSet myData)
    {
        {
            var withBlock = myData.Tables[0].Rows[0];
            return withBlock["MY_COLUMN_NAME"].ToString();
        }
    }
}