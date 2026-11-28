using System;
using System.Collections.Generic;
using Xunit;

public class OverloadsTestItem
{
    public string Name;
}

public class OverloadsTestComparer : IEqualityComparer<OverloadsTestItem>
{
    public bool Equals(OverloadsTestItem x, OverloadsTestItem y) => x.Name == y.Name;

    public int GetHashCode(OverloadsTestItem obj) => obj.Name.GetHashCode();

    public bool SameName(OverloadsTestItem x, object name) => Equals(x.Name, name);
}

public class OverloadsTestBase
{
    public string this[string key] => "base " + key;

    public string Describe(object value) => "base object";

    public static string Create() => "base create";
}

public class OverloadsTestDerived : OverloadsTestBase
{
    public string this[int index] => "derived " + index;

    public string Describe(int value) => "derived int";

    public static string Create(string name) => "derived " + name;

    public string DescribeText() => Describe("text");

    public string LookupText() => this["key"];

    public static string CreateDefault() => Create();
}

public class OverloadsTestNamedIndexerBase
{
    [System.Runtime.CompilerServices.IndexerName("Value")]
    public string this[string key] => "base " + key;
}

public class OverloadsTestNamedIndexerDerived : OverloadsTestNamedIndexerBase
{
    [System.Runtime.CompilerServices.IndexerName("Value")]
    public string this[int index] => "derived " + index;

    public string LookupText() => this["key"];
}

public interface IOverloadsTestBase
{
    string Run(int i);
}

public interface IOverloadsTestDerived : IOverloadsTestBase
{
    string Run(string s);
}

public class OverloadsTestRunner : IOverloadsTestDerived
{
    public string Run(int i) => "int";

    public string Run(string s) => "string";
}

public class OverloadsTests
{
    [Fact]
    public void ComparerEqualsKeepsObjectEquals()
    {
        var comparer = new OverloadsTestComparer();
        Assert.True(comparer.Equals(new OverloadsTestItem { Name = "a" }, new OverloadsTestItem { Name = "a" }));
        Assert.True(comparer.SameName(new OverloadsTestItem { Name = "a" }, "a"));
    }

    [Fact]
    public void BaseMethodStaysCallable() => Assert.Equal("base object", new OverloadsTestDerived().DescribeText());

    [Fact]
    public void BaseIndexerStaysCallable() => Assert.Equal("base key", new OverloadsTestDerived().LookupText());

    [Fact]
    public void BaseNamedIndexerStaysCallable()
    {
        var derived = new OverloadsTestNamedIndexerDerived();
        Assert.Equal("base key", derived.LookupText());
        Assert.Equal("derived 1", derived[1]);
    }

    [Fact]
    public void NamedIndexerKeepsItsName() => Assert.NotNull(typeof(OverloadsTestNamedIndexerBase).GetProperty("Value"));

    [Fact]
    public void BaseStaticMethodStaysCallable() => Assert.Equal("base create", OverloadsTestDerived.CreateDefault());

    [Fact]
    public void BaseInterfaceMethodStaysCallable()
    {
        IOverloadsTestDerived runner = new OverloadsTestRunner();
        Assert.Equal("int", runner.Run(1));
        Assert.Equal("string", runner.Run("s"));
    }
}
