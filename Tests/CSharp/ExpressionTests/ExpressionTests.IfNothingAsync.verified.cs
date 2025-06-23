using System;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass
{
    private object SomeDate = "";
    private DateTime? SomeDateDateNothing;
    private object isNotNothing;
    private object isSomething;

    public VisualBasicClass()
    {
        SomeDateDateNothing = string.IsNullOrEmpty(Conversions.ToString(SomeDate)) ? default : DateTime.Parse(Conversions.ToString(SomeDate));
        isNotNothing = SomeDateDateNothing is not null;
        isSomething = new DateTime() is var arg1 && SomeDateDateNothing.HasValue ? SomeDateDateNothing.Value == arg1 : (bool?)null;
    }
}