using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public bool IsHybridApp()
    {
        return Conversions.ToBoolean(((dynamic)new object()).Session("hybrid") is not null && Operators.ConditionalCompareObjectEqual(((dynamic)new object()).Session("hybrid"), 1, false));
    }
}