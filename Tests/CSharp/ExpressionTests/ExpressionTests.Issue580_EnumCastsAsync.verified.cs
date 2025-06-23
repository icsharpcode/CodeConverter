using System;

public partial class EnumToString
{
    public enum Tes : short
    {
        None = 0,
        TEST2 = 2
    }
    private void TEest2(Tes aEnum)
    {
        string sxtr_Tmp = "Use" + ((short)aEnum).ToString();
        short si_Txt = (short)Math.Round(Math.Pow(2d, (double)Tes.TEST2));
    }
}