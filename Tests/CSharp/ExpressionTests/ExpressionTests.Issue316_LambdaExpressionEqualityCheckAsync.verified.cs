using System;
using System.Linq.Expressions;

internal partial class TestClass
{
    private void TestMethod(string a, string b)
    {
        Expression<Func<bool>> test = () => a == b;
        test.Compile()();
    }
}