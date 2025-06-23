using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class SomeClass
{
    public void S([Optional, DefaultParameterValue(-1)] ref int x)
    {
        int i = 0;
        bool localF1(ref int x) { object argo = i; var ret = F1(x, ref argo); i = Conversions.ToInteger(argo); return ret; }
        bool localF2(ref int x) { object argo1 = i; var ret = F2(ref x, ref argo1); i = Conversions.ToInteger(argo1); return ret; }
        bool localF3(ref int x) { object argx = x; object argo2 = i; var ret = F3(ref argx, ref argo2); x = Conversions.ToInteger(argx); i = Conversions.ToInteger(argo2); return ret; }

        if (localF1(ref x))
        {
        }
        else if (localF2(ref x))
        {
        }
        else if (localF3(ref x))
        {
        }
    }

    public bool F1(int x, ref object o)
    {
        return default;
    }
    public bool F2(ref int x, ref object o)
    {
        return default;
    }
    public bool F3(ref object x, ref object o)
    {
        return default;
    }
}