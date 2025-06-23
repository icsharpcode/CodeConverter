using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void Test()
    {
        var LstTmp = new List<int>();
        LstTmp.Add(5);
        LstTmp.Add(6);
        LstTmp.Add(7);
        var i_Total = default(int);
        foreach (int CurVal in LstTmp)
        {
            i_Total += CurVal;
            bool exitFor = false;
            switch (CurVal)
            {
                case 6:
                    {
                        exitFor = true;
                        break;
                    }
            }

            if (exitFor)
            {
                break;
            }
        }
        Console.WriteLine(i_Total.ToString());
    }
}