using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class C
{
    public enum OrderStatus
    {
        Pending = 0,
        Fullfilled = 1
    }

    public void Test1()
    {
        object val = "1";
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)val;
        OrderStatus? null2 = (OrderStatus?)val;
    }
    public void Test2()
    {
        string val = "1";
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)Conversions.ToInteger(val);
        OrderStatus? null2 = (OrderStatus?)Conversions.ToInteger(val);
    }
    public void Test3()
    {
        object val = 1;
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)val;
        OrderStatus? null2 = (OrderStatus?)val;
    }
    public void Test4()
    {
        object val = 1.5m;
        OrderStatus os1 = (OrderStatus)Conversions.ToInteger(val);
        OrderStatus os2 = (OrderStatus)Conversions.ToInteger(val);

        OrderStatus? null1 = (OrderStatus?)val;
        OrderStatus? null2 = (OrderStatus?)val;
    }
}