using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public void Foo()
    {
        DateTime x = Conversions.ToDate("2019-09-04");
    }
}