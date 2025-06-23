using System;

public partial class VisualBasicClass
{
    public void Rounding()
    {
        object o = 3.0f;
        var x = Math.Round(o, 2);
    }
}
1 target compilation errors:
CS1503: Argument 1: cannot convert from 'object' to 'double'