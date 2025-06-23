using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class MyTestClass
{

    private int Prop { get; set; }
    private int Prop2 { get; set; }

    private bool TakesRef(ref int vrbTst)
    {
        vrbTst = Prop + 1;
        return vrbTst > 3;
    }

    private void TakesRefVoid(ref int vrbTst)
    {
        vrbTst = vrbTst + 1;
    }

    public void UsesRef(bool someBool, int someInt)
    {

        TakesRefVoid(ref someInt); // Convert directly
        int argvrbTst = 1;
        TakesRefVoid(ref argvrbTst); // Requires variable before
        int argvrbTst1 = Prop2;
        TakesRefVoid(ref argvrbTst1);
        Prop2 = argvrbTst1; // Requires variable before, and to assign back after

        bool a = TakesRef(ref someInt); // Convert directly
        int argvrbTst2 = 2;
        bool b = TakesRef(ref argvrbTst2); // Requires variable before
        int argvrbTst3 = Prop;
        bool c = TakesRef(ref argvrbTst3);
        Prop = argvrbTst3; // Requires variable before, and to assign back after

        bool localTakesRef() { int argvrbTst = 3 * Conversions.ToInteger(a); var ret = TakesRef(ref argvrbTst); return ret; }
        bool localTakesRef1() { int argvrbTst1 = Prop; var ret = TakesRef(ref argvrbTst1); Prop = argvrbTst1; return ret; }

        if (16 > someInt || TakesRef(ref someInt)) // Convert directly
        {
            Console.WriteLine(1);
        }
        else if (someBool && localTakesRef()) // Requires variable before (in local function)
        {
            someInt += 1;
        }
        else if (localTakesRef1()) // Requires variable before, and to assign back after (in local function)
        {
            someInt -= 2;
        }
        Console.WriteLine(someInt);
    }
}