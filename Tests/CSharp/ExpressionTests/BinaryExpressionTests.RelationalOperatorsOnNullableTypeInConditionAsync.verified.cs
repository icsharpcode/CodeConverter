
internal partial class TestClass
{
    private void TestMethod()
    {
        int? x = default;
        int? y = default;
        int a = 0;

        if (x.HasValue && y.HasValue && x.Value == y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value != y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value > y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value >= y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value < y.Value)
            return;
        if (x.HasValue && y.HasValue && x.Value <= y.Value)
            return;

        if (y.HasValue && a == y.Value)
            return;
        if (y.HasValue && a != y.Value)
            return;
        if (y.HasValue && a > y.Value)
            return;
        if (y.HasValue && a >= y.Value)
            return;
        if (y.HasValue && a < y.Value)
            return;
        if (y.HasValue && a <= y.Value)
            return;

        if (x.HasValue && x.Value == a)
            return;
        if (x.HasValue && x.Value != a)
            return;
        if (x.HasValue && x.Value > a)
            return;
        if (x.HasValue && x.Value >= a)
            return;
        if (x.HasValue && x.Value < a)
            return;
        if (x.HasValue && x.Value <= a)
            return;
    }
}