using System;
using System.Linq.Expressions;

internal partial class TestClass
{
    private void TestMethod(object a)
    {
        Expression<Func<bool>> test = () => a == default;
        test.Compile()();
    }
}