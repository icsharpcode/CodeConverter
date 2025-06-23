using System.Data;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class AsEnumerableTest
{
    public void FillImgColor()
    {
        var dtsMain = new DataSet();
        foreach (int i_ColCode in from CurRow in dtsMain.Tables["tb_Color"].AsEnumerable()
                                  select Conversions.ToInteger(CurRow["i_ColCode"]))
        {
        }
    }
}