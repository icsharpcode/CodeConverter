
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? a = default;
        bool? b = default;
        bool x = false;

        if ((a | b) == true)
            return;
        if (a.GetValueOrDefault() || b.GetValueOrDefault())
            return;
        if ((a | x) == true)
            return;
        if (a.GetValueOrDefault() || x)
            return;
        if ((x | a) == true)
            return;
        if (x || a.GetValueOrDefault())
            return;

        var res = a | b;
        res = a.GetValueOrDefault() ? true : b is not { } arg1 ? null : arg1 ? true : a;
        res = a | x;
        res = a.GetValueOrDefault() ? true : x ? true : a;
        res = x | a;
        res = x ? true : a;

    }
}