using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class BinaryExpressionRefParameter
{
    public static void Main()
    {
        object wide = 7;
        int argarg = Conversions.ToInteger(wide);
        LogAndReset(ref argarg);
        wide = argarg;
        object[] wideArray = new object[] { 3, 4, 4 };
        var tmp = wideArray;
        int argarg1 = Conversions.ToInteger(tmp[1]);
        LogAndReset(ref argarg1);
        tmp[1] = argarg1;
        short narrow = 3;
        int argarg2 = narrow;
        LogAndReset(ref argarg2);
        narrow = (short)argarg2;
        int argarg3 = 7 + 3;
        LogAndReset(ref argarg3);
    }

    public static void LogAndReset(ref int arg)
    {
        Console.WriteLine(arg);
        arg = 0;
    }
}