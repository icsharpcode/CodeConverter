
internal partial class TestClass
{
    private void TestMethod()
    {
        bool? a = default;
        bool? b = default;
        bool x = false;

        if ((a & b) == true)
            return;
        if ((a.HasValue && !a.Value ? false : !b.HasValue ? null : b.Value ? a : false) == true)
            return;
        if ((a & x) == true)
            return;
        if ((!a.HasValue || a.Value) && x && a.HasValue)
            return;
        if ((x & a) == true)
            return;
        if (x && a.GetValueOrDefault())
            return;

        var res = a & b;
        res = a.HasValue && !a.Value ? false : !b.HasValue ? null : b.Value ? a : false;
        res = a & x;
        res = a.HasValue && !a.Value ? false : x ? a : false;
        res = x & a;
        res = x ? a : false;

    }
}