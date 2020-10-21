using Microsoft.VisualBasic.CompilerServices;
using Xunit;


public partial class WhyWeNeedToCastNothing
{
    [Fact]
    public static void CorrectOverloadChosen()
    {
        Assert.Equal(4011, Identity((int?)default));
        Assert.Equal(4011, Identity((int?)null));
        Assert.Equal("null", Identity(default(string)));
        Assert.Equal("null", Identity((string)null));
    }

    public static int? Identity(int? vbInitValue)
    {
        return !vbInitValue.HasValue ? 4011 : vbInitValue;
    }

    public static string Identity(string vbInitValue)
    {
        return vbInitValue == null ? "null" : vbInitValue;
    }
}