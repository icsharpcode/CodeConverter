using System;

public partial class Compound
{
    public void Operators()
    {
        short aShort = 123;
        decimal aDec = 12.3m;
        aShort = (short)Math.Round(aShort * aDec);
        aShort = (short)(aShort / (long)Math.Round(aDec));
        aShort = (short)Math.Round(aShort / aDec);
        aShort = (short)Math.Round(aShort - aDec);
        aShort = (short)Math.Round(aShort + aDec);
    }
}