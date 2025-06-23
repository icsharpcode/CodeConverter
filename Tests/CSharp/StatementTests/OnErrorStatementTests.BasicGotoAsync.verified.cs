using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public bool SelfDivisionPossible(int x)
    {
        try
        {
            int i = (int)Math.Round(x / (double)x);
            return true;
        }
        catch
        {

            return Information.Err().Number == 6;
        }
    }
}