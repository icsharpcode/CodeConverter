using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class BinaryExpressionOutParameter
{
    public static void Main()
    {
        object wide = 7;
        int argarg = Conversions.ToInteger(wide);
        Zero(out argarg);
        wide = argarg;
        short narrow = 3;
        int argarg1 = narrow;
        Zero(out argarg1);
        narrow = (short)argarg1;
        int argarg2 = 7 + 3;
        Zero(out argarg2);
    }

    public static void Zero(out int arg)
    {
        arg = 0;
    }
}