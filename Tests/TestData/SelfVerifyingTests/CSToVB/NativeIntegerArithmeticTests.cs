using Xunit;

/// <summary>
/// Values beyond Int32 (the tests run in a 64-bit process), so a conversion that narrows a native integer to Integer throws.
/// </summary>
public class NativeIntegerArithmeticTests
{
    private static readonly nint Low = (nint)0x1_0000_0000L;
    private static readonly nint High = (nint)0x1_0000_0010L;
    private static readonly nuint UnsignedLow = (nuint)0x1_0000_0000UL;
    private static readonly nuint UnsignedHigh = (nuint)0x1_0000_0010UL;

    [Fact]
    public void DistanceBetweenPointers()
    {
        nint distance = High - Low;
        Assert.Equal(16L, (long)distance);
    }

    [Fact]
    public void SumOfNativeIntegers() => Assert.Equal(0x2_0000_0010L, (long)(Low + High));

    [Fact]
    public void OffsetByInt()
    {
        int offset = 4;
        nint pointer = Low + offset;
        Assert.Equal(0x1_0000_0004L, (long)pointer);
        Assert.Equal(0x1_0000_0000L, (long)(pointer - offset));
    }

    [Fact]
    public void Comparisons()
    {
        Assert.True(High > Low);
        Assert.True(Low < High);
        Assert.True(High >= Low);
        Assert.False(High <= Low);
        Assert.True(High != Low);
        Assert.False(High == Low);
    }

    [Fact]
    public void MultiplicativeOperators()
    {
        Assert.Equal(0x2_0000_0020L, (long)(High * 2));
        Assert.Equal(0x8000_0008L, (long)(High / 2));
        Assert.Equal(16L, (long)(High % Low));
    }

    [Fact]
    public void BitwiseAndShiftOperators()
    {
        Assert.Equal(0x1_0000_0000L, (long)(High & Low));
        Assert.Equal(0x1_0000_0010L, (long)(High | Low));
        Assert.Equal(16L, (long)(High ^ Low));
        Assert.Equal(0x2_0000_0000L, (long)(Low << 1));
        Assert.Equal(0x1000_0000L, (long)(Low >> 4));
    }

    [Fact]
    public void UnaryOperators()
    {
        Assert.Equal(-0x1_0000_0000L, (long)(-Low));
        Assert.Equal(~0x1_0000_0000L, (long)(~Low));
    }

    [Fact]
    public void CompoundAssignments()
    {
        nint value = Low;
        value += High;
        Assert.Equal(0x2_0000_0010L, (long)value);
        value -= High;
        Assert.Equal(0x1_0000_0000L, (long)value);
        value *= 2;
        value /= 4;
        Assert.Equal(0x8000_0000L, (long)value);
        value <<= 1;
        value |= 1;
        Assert.Equal(0x1_0000_0001L, (long)value);
        value += 1;
        value++;
        Assert.Equal(0x1_0000_0003L, (long)value);
    }

    [Fact]
    public void UnsignedNativeIntegers()
    {
        nuint distance = UnsignedHigh - UnsignedLow;
        Assert.Equal(16UL, (ulong)distance);
        Assert.True(UnsignedHigh > UnsignedLow);
        Assert.Equal(0x2_0000_0010UL, (ulong)(UnsignedLow + UnsignedHigh));
    }

    [Fact]
    public void MixedWithLong()
    {
        long sum = High + 1L;
        Assert.Equal(0x1_0000_0011L, sum);
        long total = 1;
        total += High;
        Assert.Equal(0x1_0000_0011L, total);
    }
}
