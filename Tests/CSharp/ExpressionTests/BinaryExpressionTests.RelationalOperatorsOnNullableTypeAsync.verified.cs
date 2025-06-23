
internal partial class TestClass
{
    private void TestMethod()
    {
        int? x = default;
        int? y = default;
        int a = 0;

        var res = x.HasValue && y.HasValue ? x.Value == y.Value : (bool?)null;
        res = x.HasValue && y.HasValue ? x.Value != y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value > y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value >= y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value < y.Value : null;
        res = x.HasValue && y.HasValue ? x.Value <= y.Value : null;

        res = y.HasValue ? a == y.Value : null;
        res = y.HasValue ? a != y.Value : null;
        res = y.HasValue ? a > y.Value : null;
        res = y.HasValue ? a >= y.Value : null;
        res = y.HasValue ? a < y.Value : null;
        res = y.HasValue ? a <= y.Value : null;

        res = x.HasValue ? x.Value == a : null;
        res = x.HasValue ? x.Value != a : null;
        res = x.HasValue ? x.Value > a : null;
        res = x.HasValue ? x.Value >= a : null;
        res = x.HasValue ? x.Value < a : null;
        res = x.HasValue ? x.Value <= a : null;
    }
}