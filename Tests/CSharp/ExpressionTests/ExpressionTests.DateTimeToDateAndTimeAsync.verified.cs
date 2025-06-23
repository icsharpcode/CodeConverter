using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        var x = DateAndTime.DateAdd("m", 5d, DateTime.Now);
    }
}