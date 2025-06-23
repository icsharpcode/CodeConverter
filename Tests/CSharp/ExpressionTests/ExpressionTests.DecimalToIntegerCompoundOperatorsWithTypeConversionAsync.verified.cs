using System;

public partial class Compound
{
    public void Operators()
    {
        int anInt = 123;
        decimal aDec = 12.3m;
        anInt = (int)Math.Round(anInt * aDec);
        anInt = (int)(anInt / (long)Math.Round(aDec));
        anInt = (int)Math.Round(anInt / aDec);
        anInt = (int)Math.Round(anInt - aDec);
        anInt = (int)Math.Round(anInt + aDec);
    }
}