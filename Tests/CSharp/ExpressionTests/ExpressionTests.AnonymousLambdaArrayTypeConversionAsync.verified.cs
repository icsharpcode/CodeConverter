using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class TargetTypeTestClass
{

    private static void Main()
    {
        Action[] actions = new[] { new Action(() => Debug.Print(1.ToString())), new Action(() => Debug.Print(2.ToString())) };
        var objects = new List<object>() { new Action(() => Debug.Print(3.ToString())), new Action(() => Debug.Print(4.ToString())) };
    }
}