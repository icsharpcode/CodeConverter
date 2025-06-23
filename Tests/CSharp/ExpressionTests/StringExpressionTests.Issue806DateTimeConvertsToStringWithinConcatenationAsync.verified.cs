using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Issue806
{
    public void Foo()
    {
        string x = Conversions.ToString(DateTime.Parse("2022-01-01")) + " 15:00";
    }
}