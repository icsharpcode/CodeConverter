using System.Collections.Generic;
using System.Linq;
using Xunit;

public static class ExtensionMethodTestExtensions
{
    public static string Twice(this string s) => s + s;

    public static void Increment(this ref int value) => value++;

    public static void Decrement(ref this int value) => value--;

    public static T OrDefault<T>(this T? value) where T : struct => value ?? default;

    public static int CountAbove(this IEnumerable<int> values, int threshold) => values.Count(v => v > threshold);
}

public class ExtensionMethodTests
{
    [Fact]
    public void ExtensionMethodOnReferenceType() => Assert.Equal("abab", "ab".Twice());

    [Fact]
    public void ExtensionMethodCalledAsStaticMethod() => Assert.Equal("abab", ExtensionMethodTestExtensions.Twice("ab"));

    [Fact]
    public void RefExtensionMethodsOnValueType()
    {
        var i = 1;
        i.Increment();
        i.Increment();
        i.Decrement();
        Assert.Equal(2, i);
    }

    [Fact]
    public void GenericExtensionMethod()
    {
        int? missing = null;
        Assert.Equal(0, missing.OrDefault());
        Assert.Equal(3, ((int?)3).OrDefault());
    }

    [Fact]
    public void ExtensionMethodOnInterface() => Assert.Equal(2, new List<int> { 1, 5, 7 }.CountAbove(4));
}
