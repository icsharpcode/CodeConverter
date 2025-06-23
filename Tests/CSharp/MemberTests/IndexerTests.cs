using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class IndexerTests : ConverterTestBase
{
    [Fact]
    public async Task InterfaceImplementationOfIndexerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int this[string str] { get; set; }
}

public partial class Foo : IFoo
{

    public virtual int this[string str]
    {
        get
        {
            return 1;
        }
        set
        {
        }
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task InterfaceImplementationOfIndexerAsAbstractAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int this[string str] { get; set; }
}

public abstract partial class Foo : IFoo
{

    public abstract int this[string str] { get; set; }
}

public partial class FooChild : Foo
{

    public override int this[string str]
    {
        get
        {
            return 1;
        }
        set
        {
        }
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedImplementationOfIndexerWithAbstractAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int this[string str] { get; set; }
}

public abstract partial class Foo : IFoo
{

    public abstract int this[string str] { get; set; }
}

public partial class FooChild : Foo
{

    public override int this[string str]
    {
        get
        {
            return 1;
        }
        set
        {
        }
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ReadOnlyImplementationOfIndexerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int this[string str] { get; }
}

public partial class Foo : IFoo
{

    public virtual int this[string str]
    {
        get
        {
            return 2;
        }
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WriteOnlyImplementationOfIndexerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int this[string str] { set; }
}

public partial class Foo : IFoo
{

    public virtual int this[string str]
    {
        set
        {
        }
    }
}
", extension: "cs")
            );
        }
    }
}