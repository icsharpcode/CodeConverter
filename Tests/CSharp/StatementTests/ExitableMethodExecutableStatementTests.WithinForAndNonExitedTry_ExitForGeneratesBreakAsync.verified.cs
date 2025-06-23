using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object[] ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        for (int i = 0, loopTo = ComboBox_CostCenter.Length - 1; i <= loopTo; i++)
        {
            try
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(7, The_Cost_Center, false)))
                {
                    SomeCase *= 7;
                    break;
                }
            }
            finally
            {
            }
        }
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}