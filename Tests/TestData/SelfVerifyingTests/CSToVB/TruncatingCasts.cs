using Xunit;

public partial class TruncatingCasts
{
    [Fact]
    public static void TruncatesTowardsZero()
    {
        double positive = 0.6d;
        double negative = -0.6d;
        double aboveOne = 1.9d;
        decimal decimalValue = 2.9m;

        Assert.Equal(0, (int)positive);
        Assert.Equal(0, (int)negative);
        Assert.Equal(1, (int)aboveOne);
        Assert.Equal(-1, (int)-aboveOne);
        Assert.Equal(0, Truncated(negative));

        long truncatedDecimal = (long)decimalValue;
        Assert.Equal(2L, truncatedDecimal);
    }

    private static int Truncated(double value)
    {
        return (int)value;
    }
}
