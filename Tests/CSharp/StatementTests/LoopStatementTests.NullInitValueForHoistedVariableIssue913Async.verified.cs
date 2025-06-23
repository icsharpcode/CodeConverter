using System.Collections.Generic;
using System.Diagnostics;

public partial class VisualBasicClass
{
    private static void ProblemsWithPullingVariablesOut()
    {
        // example 1
        var b = default(long);
        foreach (var a in new List<string>())
        {
            if (string.IsNullOrEmpty(a))
            {
                b = 1L;
            }
            DoSomeImportantStuff(b);
        }

        // example 2
        var c = default(string);
        var d = default(long);
        while (true)
        {
            if (string.IsNullOrEmpty(c))
            {
                d = 1L;
            }

            DoSomeImportantStuff(d);
            break;
        }
    }

    private static void ProblemsWithPullingVariablesOut_AlwaysWriteBeforeRead()
    {
        // example 1
        foreach (var a in new List<string>())
        {
            long b;
            if (string.IsNullOrEmpty(a))
            {
                b = 1L;
            }
            DoSomeImportantStuff();
        }

        // example 2
        var c = default(string);
        while (true)
        {
            long d;
            if (string.IsNullOrEmpty(c))
            {
                d = 1L;
            }

            DoSomeImportantStuff();
            break;
        }
    }
    private static void DoSomeImportantStuff()
    {
        Debug.Print("very important");
    }
    private static void DoSomeImportantStuff(long b)
    {
        Debug.Print("very important");
    }
}