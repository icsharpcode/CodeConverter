using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public partial class Issue213
{
    private static DateTime x = DateTime.Parse("1990-01-01");

    private void Y([Optional, DateTimeConstant(627667488000000000L/* Global.Issue213.x */)] ref DateTime opt)
    {
    }

    private void CallsY()
    {
        DateTime argopt = DateTime.Parse("1990-01-01");
        Y(opt: ref argopt);
    }
}