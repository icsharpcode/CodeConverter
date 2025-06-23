using System;

public partial class ConversionInComparisonOperatorTest
{
    public void Foo()
    {
        decimal SomeDecimal = 12.3m;
        double ACalc = 32.1d;
        if (ACalc > (double)(60m / SomeDecimal))
        {
            Console.WriteLine(1);
        }
    }
}