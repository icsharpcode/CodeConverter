using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private void TestMethod()
    {
        object test(object a) => Operators.MultiplyObject(a, 2);
        object test2(object a, object b)
        {
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectGreater(b, 0, false)))
                return Operators.DivideObject(a, b);
            return 0;
        };

        object test3(object a, object b) => Operators.ModObject(a, b);
        test(3);
    }
}