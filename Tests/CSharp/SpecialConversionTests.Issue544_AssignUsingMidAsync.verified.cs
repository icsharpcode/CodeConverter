using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue483
{
    private string numstr(double aDouble)
    {
        string str_Txt = Strings.Format(aDouble, "0.000000");
        StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, 1, ".");
        StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, ".".Length, ".");
        StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, aDouble.ToString().Length, aDouble.ToString());
        Console.WriteLine(aDouble);
        if (aDouble > 5.0d)
        {
            var midTmp = numstr(aDouble - 1.0d);
            StringType.MidStmtStr(ref str_Txt, Strings.Len(str_Txt) - 6, midTmp.Length, midTmp);
        }
        return str_Txt;
    }
}