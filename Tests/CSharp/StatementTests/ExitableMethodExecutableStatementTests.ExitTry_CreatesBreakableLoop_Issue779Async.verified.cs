using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object[] ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        do
        {
            try
            {
                if (!To_Show_Cost())
                {
                    SomeCase *= 2;
                }

                SomeCase *= 3;

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(The_Cost_Center, 0, false)))
                {
                    SomeCase *= 5;
                    break;
                }

                bool exitTry = false;
                for (int i = 0, loopTo = ComboBox_CostCenter.Length - 1; i <= loopTo; i++)
                {
                    if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(7, The_Cost_Center, false)))
                    {
                        SomeCase *= 7;
                        exitTry = true;
                        break;
                    }
                }

                if (exitTry)
                {
                    break;
                }
            }
            finally
            {
            }
        }
        while (false);
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}