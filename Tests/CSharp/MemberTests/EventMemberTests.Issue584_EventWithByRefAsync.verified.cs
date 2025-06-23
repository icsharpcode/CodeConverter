using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue584RaiseEventByRefDemo
{
    public event ConversionNeededEventHandler ConversionNeeded;

    public delegate void ConversionNeededEventHandler(int ai_OrigID, ref int NewID);

    public int TestConversion(object ai_ID)
    {
        var i_NewValue = default(int);
        ConversionNeeded?.Invoke(Conversions.ToInteger(ai_ID), ref i_NewValue);
        return i_NewValue;
    }
}
