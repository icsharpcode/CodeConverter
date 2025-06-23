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
            bool continueFor = false;
            while (CurVal < 3)
            {
                bool breakFor = false;
                switch (CurVal)
                {
                    case 6:
                        {
                            continueFor = breakFor = true;
                            break;
                        }
                }

                if (breakFor)
                {
                    break;
                }
            }

            if (continueFor)
            {
                continue;
            }
            bool continueFor1 = false;
            bool exitFor1 = false;
            while (CurVal < 4)
            {
                bool breakFor1 = false;
                bool exitFor = false;
                switch (CurVal)
                {
                    case 7:
                        {
                            continueFor1 = breakFor1 = true;
                            break;
                        }
                    case 8:
                        {
                            exitFor1 = exitFor = true;
                            break;
                        }
                }

                if (breakFor1)
                {
                    break;
                }

                if (exitFor)
                {
                    break;
                }
            }

            if (continueFor1)
            {
                continue;
            }

            if (exitFor1)
            {
                break;
            }
            Console.WriteLine();
        }
        Console.WriteLine(i_Total.ToString());
    }
}