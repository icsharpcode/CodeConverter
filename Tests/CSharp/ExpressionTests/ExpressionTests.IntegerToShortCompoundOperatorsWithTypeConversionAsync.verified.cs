using System;

public partial class Compound
{
    public void Operators()
    {
        short aShort = 123;
        int anInt = 12;
        aShort = (short)(aShort * anInt);
        aShort = (short)(aShort / anInt);
        aShort = (short)Math.Round(aShort / (double)anInt);
        aShort = (short)(aShort - anInt);
        aShort = (short)(aShort + anInt);
    }
}