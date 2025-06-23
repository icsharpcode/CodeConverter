using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class InterfaceTests : ConverterTestBase
{

    [Fact]
    public async Task Issue443_FixCaseForInterfaceMembersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooDifferentCase(out string str2);
}

public partial class Foo : IFoo
{
    public int FooDifferentCase(out string str2)
    {
        str2 = 2.ToString();
        return 3;
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Issue444_FixNameForRenamedInterfaceMembersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooDifferentName(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int BarDifferentName(ref string str, int i)
    {
        return 4;
    }

    int IFoo.FooDifferentName(ref string str, int i) => BarDifferentName(ref str, i);
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IdenticalInterfaceMethodsWithRenamedInterfaceMembersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFooBar(ref string str, int i);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i);
}

public partial class FooBar : IFoo, IBar
{

    public int Foo(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFooBar(ref string str, int i) => Foo(ref str, i);

    public int Bar(ref string str, int i)
    {
        return 2;
    }

    int IBar.DoFooBar(ref string str, int i) => Bar(ref str, i);

}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfaceCasingOnlyDifferenceConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo();
    int Prop { get; set; }
}

public partial class Foo : IFoo
{

    private int doFoo()
    {
        return 4;
    }

    int IFoo.DoFoo() => doFoo();

    private int prop { get; set; }
    int IFoo.Prop { get => prop; set => prop = value; }

    private int Consumer()
    {
        var foo = new Foo();
        IFoo interfaceInstance = foo;
        return foo.doFoo() + foo.doFoo() + interfaceInstance.DoFoo() + interfaceInstance.DoFoo() + foo.prop + foo.prop + interfaceInstance.Prop + interfaceInstance.Prop;
    }

}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfaceCasingOnlyDifferenceForVirtualMemberConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo();
    int Prop { get; set; }
}

public abstract partial class BaseFoo : IFoo
{

    protected internal virtual int doFoo()
    {
        return 4;
    }

    int IFoo.DoFoo() => doFoo();

    protected internal virtual int prop { get; set; }
    int IFoo.Prop { get => prop; set => prop = value; }

}

public partial class Foo : BaseFoo
{

    protected internal override int doFoo()
    {
        return 5;
    }

    protected internal override int prop { get; set; }

    private int Consumer()
    {
        var foo = new Foo();
        IFoo interfaceInstance = foo;
        BaseFoo baseClass = foo;
        return foo.doFoo() + foo.doFoo() + interfaceInstance.DoFoo() + interfaceInstance.DoFoo() + baseClass.doFoo() + baseClass.doFoo() + foo.prop + foo.prop + interfaceInstance.Prop + interfaceInstance.Prop + baseClass.prop + baseClass.prop;
    }
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfaceCasingOnlyDifferenceWithOverloadedPropertyConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IUserContext
{
    string GroupID { get; }
}

public partial interface IFoo
{
    string ConnectedGroupId { get; }
}

public abstract partial class BaseFoo : IUserContext
{

    protected internal string ConnectedGroupID { get; private set; }
    string IUserContext.GroupID { get => ConnectedGroupID; }

}

public partial class Foo : BaseFoo, IFoo
{

    protected internal new string ConnectedGroupID
    {
        get
        {
            return """" ?? base.ConnectedGroupID;
        }
    }

    string IFoo.ConnectedGroupId { get => ConnectedGroupID; } // Comment moves because this line gets split

    private string Consumer()
    {
        var foo = new Foo();
        IFoo ifoo = foo;
        BaseFoo baseFoo = foo;
        IUserContext iUserContext = foo;
        return foo.ConnectedGroupID + foo.ConnectedGroupID + ifoo.ConnectedGroupId + ifoo.ConnectedGroupId + baseFoo.ConnectedGroupID + baseFoo.ConnectedGroupID + iUserContext.GroupID + iUserContext.GroupID;
    }

}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedMethodImplementsMultipleInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFooBar(ref string str, int i);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i);
}

public partial class FooBar : IFoo, IBar
{

    public int Foo(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFooBar(ref string str, int i) => Foo(ref str, i);
    int IBar.DoFooBar(ref string str, int i) => Foo(ref str, i);

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task IdenticalInterfacePropertiesWithRenamedInterfaceMembersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooBarProp { get; set; }
}

public partial interface IBar
{
    int FooBarProp { get; set; }
}

public partial class FooBar : IFoo, IBar
{

    public int Foo { get; set; }
    int IFoo.FooBarProp { get => Foo; set => Foo = value; }

    public int Bar { get; set; }
    int IBar.FooBarProp { get => Bar; set => Bar = value; }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationRequiredMethodParameters_749_Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFooBar(ref string str, int i);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i);
}

public partial class FooBar : IFoo, IBar
{

    public int Foo(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFooBar(ref string str, int i) => Foo(ref str, i);

    public int Bar(ref string str, int i)
    {
        return 2;
    }

    int IBar.DoFooBar(ref string str, int i) => Bar(ref str, i);

}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalParameters_1062_Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface InterfaceWithOptionalParameters
{
    void S(int i = 0);
}

public partial class ImplInterfaceWithOptionalParameters : InterfaceWithOptionalParameters
{
    public void InterfaceWithOptionalParameters_S(int i = 0)
    {
    }

    void InterfaceWithOptionalParameters.S(int i = 0) => InterfaceWithOptionalParameters_S(i);
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OptionalParameterWithReservedName_1092_Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class WithOptionalParameters
{
    public void S1(object a = null, string @default = """")
    {
    }

    public void S()
    {
        S1(@default: ""a"");
    }
}
", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalParametersAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int get_ExplicitProp(string str = """");
    void set_ExplicitProp(string str = """", int value = default);
    int ExplicitFunc(string str2 = """", int i2 = 1);
}

public partial class Foo : IFoo
{

    private int ExplicitFunc(string str = """", int i2 = 1)
    {
        return 5;
    }

    int IFoo.ExplicitFunc(string str = """", int i2 = 1) => ExplicitFunc(str, i2);

    private int get_ExplicitProp(string str = """")
    {
        return 5;
    }
    private void set_ExplicitProp(string str = """", int value = default)
    {
    }

    int IFoo.get_ExplicitProp(string str = """") => get_ExplicitProp(str);
    void IFoo.set_ExplicitProp(string str = """", int value = default) => set_ExplicitProp(str, value);
}
", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalMethodParameters_749_Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFooBar(ref string str, int i = 4);
}

public partial interface IBar
{
    int DoFooBar(ref string str, int i = 8);
}

public partial class FooBar : IFoo, IBar
{

    public int Foo(ref string str, int i = 4)
    {
        return 4;
    }

    int IFoo.DoFooBar(ref string str, int i = 4) => Foo(ref str, i);

    public int Bar(ref string str, int i = 8)
    {
        return 2;
    }

    int IBar.DoFooBar(ref string str, int i = 8) => Bar(ref str, i);

}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfaceMethodFullyQualifiedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace TestNamespace
{
    public partial interface IFoo
    {
        int DoFoo(ref string str, int i);
    }
}

public partial class Foo : TestNamespace.IFoo
{

    public int DoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int TestNamespace.IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfacePropertyFullyQualifiedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace TestNamespace
{
    public partial interface IFoo
    {
        int FooProp { get; set; }
    }
}

public partial class Foo : TestNamespace.IFoo
{

    public int FooPropRenamed { get; set; }
    int TestNamespace.IFoo.FooProp { get => FooPropRenamed; set => FooPropRenamed = value; }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfaceMethodConsumerCasingRenamedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int DoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.DoFooRenamed(ref str, i) + bar.DoFoo(ref str, i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfacePropertyConsumerCasingRenamedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{

    public int FooPropRenamed { get; set; }
    int IFoo.FooProp { get => FooPropRenamed; set => FooPropRenamed = value; }

}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.FooPropRenamed + bar.FooProp;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task InterfaceMethodCasingRenamedConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo(string str, int i);
}

public partial class Foo : IFoo
{

    public int DoFoo(string str, int i)
    {
        return 4;
    }
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(string str, int i)
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.DoFoo(str, i) + bar.DoFoo(str, i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task InterfacePropertyCasingRenamedConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{

    public int FooProp { get; set; }

}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.FooProp + bar.FooProp;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task InterfaceRenamedMethodConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int DoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.DoFooRenamed(ref str, i) + bar.DoFoo(ref str, i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task InterfaceRenamedPropertyConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{

    public int FooPropRenamed { get; set; }
    int IFoo.FooProp { get => FooPropRenamed; set => FooPropRenamed = value; }

}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.FooPropRenamed + bar.FooProp;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PartialInterfaceRenamedMethodConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int DoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
}

public partial class FooConsumer
{
    public int DoFooRenamedConsumer(ref string str, int i)
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.DoFooRenamed(ref str, i) + bar.DoFoo(ref str, i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PartialInterfaceRenamedPropertyConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooProp { get; set; }
}

public partial class Foo : IFoo
{

    public int FooPropRenamed { get; set; }
    int IFoo.FooProp { get => FooPropRenamed; set => FooPropRenamed = value; }

}

public partial class FooConsumer
{
    public int GetFooRenamed()
    {
        var foo = new Foo();
        IFoo bar = foo;
        return foo.FooPropRenamed + bar.FooProp;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfaceMethodMyClassConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int DoFoo(ref string str, int i);
}

public partial class Foo : IFoo
{

    public int MyClassDoFooRenamed(ref string str, int i)
    {
        return 4;
    }

    int IFoo.DoFoo(ref string str, int i) => DoFooRenamed(ref str, i);
    public virtual int DoFooRenamed(ref string str, int i) => MyClassDoFooRenamed(ref str, i); // Comment ends up out of order, but attached to correct method

    public int DoFooRenamedConsumer(ref string str, int i)
    {
        return MyClassDoFooRenamed(ref str, i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RenamedInterfacePropertyMyClassConsumerAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

public partial interface IFoo
{
    int DoFoo { get; }
    int DoBar { set; }
}

public partial class Foo : IFoo
{

    public int MyClassDoFooRenamed
    {
        get
        {
            return 4;
        }
    }

    int IFoo.DoFoo { get => DoFooRenamed; }

    public virtual int DoFooRenamed  // Comment ends up out of order, but attached to correct method
    {
        get
        {
            return MyClassDoFooRenamed;
        }
    }

    public int MyClassDoBarRenamed
    {
        set
        {
            throw new Exception();
        }
    }

    int IFoo.DoBar { set => DoBarRenamed = value; }

    public virtual int DoBarRenamed  // Comment ends up out of order, but attached to correct method
    {
        set
        {
            MyClassDoBarRenamed = value;
        }
    }

    public void DoFooRenamedConsumer()
    {
        MyClassDoBarRenamed = MyClassDoFooRenamed;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial class Foo : IFoo
{

    private int ExplicitFunc(ref string str, int i)
    {
        return 5;
    }

    int IFoo.ExplicitFunc(ref string str, int i) => ExplicitFunc(ref str, i);

    private int get_ExplicitProp(string str)
    {
        return 5;
    }
    private void set_ExplicitProp(string str, int value)
    {
    }

    int IFoo.get_ExplicitProp(string str) => get_ExplicitProp(str);
    void IFoo.set_ExplicitProp(string str, int value) => set_ExplicitProp(str, value);
}
", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PropertyInterfaceImplementationKeepsVirtualModifierAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int get_PropParams(string str);
    void set_PropParams(string str, int value);
    int Prop { get; set; }
}

public partial class Foo : IFoo
{

    public virtual int get_PropParams(string str)
    {
        return 5;
    }
    public virtual void set_PropParams(string str, int value)
    {
    }

    public virtual int Prop
    {
        get
        {
            return 5;
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
    public async Task PrivateAutoPropertyImplementsMultipleInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}

public partial class Foo : IFoo, IBar
{

    private int ExplicitProp { get; set; }
    int IFoo.ExplicitProp { get => ExplicitProp; set => ExplicitProp = value; }
    int IBar.ExplicitProp { get => ExplicitProp; set => ExplicitProp = value; }
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task ImplementMultipleRenamedPropertiesFromInterfaceAsAbstractAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}
public abstract partial class Foo : IFoo, IBar
{

    protected abstract int ExplicitPropRenamed1 { get; set; }
    int IFoo.ExplicitProp { get => ExplicitPropRenamed1; set => ExplicitPropRenamed1 = value; }
    protected abstract int ExplicitPropRenamed2 { get; set; }
    int IBar.ExplicitProp { get => ExplicitPropRenamed2; set => ExplicitPropRenamed2 = value; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationForVirtualMemberFromAnotherClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    void Save();
    int Prop { get; set; }
}

public abstract partial class BaseFoo
{
    protected virtual void OnSave()
    {
    }

    protected virtual int MyProp { get; set; } = 5;
}

public partial class Foo : BaseFoo, IFoo
{

    protected override void OnSave()
    {
    }

    void IFoo.Save() => OnSave();

    protected override int MyProp { get; set; } = 6;
    int IFoo.Prop { get => MyProp; set => MyProp = value; }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationWhereOnlyOneInterfaceMemberIsRenamedAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    void Save();
    int A { get; set; }
}

public partial interface IBar
{
    void OnSave();
    int B { get; set; }
}

public partial class Foo : IFoo, IBar
{

    public virtual void Save()
    {
    }

    void IFoo.Save() => Save();
    void IBar.OnSave() => Save();

    public virtual int A { get; set; }
    int IFoo.A { get => A; set => A = value; }
    int IBar.B { get => A; set => A = value; }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationWhereMemberShadowsBaseAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    void Save();
    int Prop { get; set; }
}

public abstract partial class BaseFoo
{
    public virtual void OnSave()
    {
    }

    public virtual int MyProp { get; set; } = 5;
}

public partial class Foo : BaseFoo, IFoo
{

    public new void OnSave()
    {
    }

    void IFoo.Save() => OnSave();

    public new int MyProp { get; set; } = 6;
    int IFoo.Prop { get => MyProp; set => MyProp = value; }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PrivatePropertyAccessorBlocksImplementsMultipleInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int ExplicitProp { get; set; }
}

public partial interface IBar
{
    int ExplicitProp { get; set; }
}

public partial class Foo : IFoo, IBar
{

    private int ExplicitProp
    {
        get
        {
            return 5;
        }
        set
        {
        }
    }

    int IFoo.ExplicitProp { get => ExplicitProp; set => ExplicitProp = value; }
    int IBar.ExplicitProp { get => ExplicitProp; set => ExplicitProp = value; } // Comment moves because this line gets split
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NonPublicImplementsInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FriendProp { get; set; }
    void ProtectedSub();
    int PrivateFunc();
    void ProtectedInternalSub();
    void AbstractSub();
}

public partial interface IBar
{
    int FriendProp { get; set; }
    void ProtectedSub();
    int PrivateFunc();
    void ProtectedInternalSub();
    void AbstractSub();
}

public abstract partial class BaseFoo : IFoo, IBar
{

    internal virtual int FriendProp
    {
        get
        {
            return 5;
        }
        set
        {
        }
    }

    int IFoo.FriendProp { get => FriendProp; set => FriendProp = value; }
    int IBar.FriendProp { get => FriendProp; set => FriendProp = value; } // Comment moves because this line gets split

    protected void ProtectedSub()
    {
    }

    void IFoo.ProtectedSub() => ProtectedSub();
    void IBar.ProtectedSub() => ProtectedSub();

    private int PrivateFunc()
    {
        return default;
    }

    int IFoo.PrivateFunc() => PrivateFunc();
    int IBar.PrivateFunc() => PrivateFunc();

    protected internal virtual void ProtectedInternalSub()
    {
    }

    void IFoo.ProtectedInternalSub() => ProtectedInternalSub();
    void IBar.ProtectedInternalSub() => ProtectedInternalSub();

    protected abstract void AbstractSubRenamed();
    void IFoo.AbstractSub() => AbstractSubRenamed();
    void IBar.AbstractSub() => AbstractSubRenamed();
}

public partial class Foo : BaseFoo
{

    protected internal override void ProtectedInternalSub()
    {
    }

    protected override void AbstractSubRenamed()
    {
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExplicitPropertyImplementationWithDirectAccessAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int ExplicitProp { get; set; }
    int ExplicitReadOnlyProp { get; }
}

public partial class Foo : IFoo
{

    public int ExplicitPropRenamed { get; set; }
    int IFoo.ExplicitProp { get => ExplicitPropRenamed; set => ExplicitPropRenamed = value; }
    public int ExplicitRenamedReadOnlyProp { get; private set; }
    int IFoo.ExplicitReadOnlyProp { get => ExplicitRenamedReadOnlyProp; }

    private void Consumer()
    {
        ExplicitPropRenamed = 5;
        ExplicitRenamedReadOnlyProp = 10;
    }

}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ReadonlyRenamedPropertyImplementsMultipleInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int ExplicitProp { get; }
}

public partial interface IBar
{
    int ExplicitProp { get; }
}

public partial class Foo : IFoo, IBar
{

    public int ExplicitPropRenamed { get; private set; }
    int IFoo.ExplicitProp { get => ExplicitPropRenamed; }
    int IBar.ExplicitProp { get => ExplicitPropRenamed; }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task WriteonlyPropertyImplementsMultipleInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int ExplicitProp { set; }
}

public partial interface IBar
{
    int ExplicitProp { set; }
}

public partial class Foo : IFoo, IBar
{

    public int ExplicitPropRenamed
    {
        set
        {
        }
    }

    int IFoo.ExplicitProp { set => ExplicitPropRenamed = value; }
    int IBar.ExplicitProp { set => ExplicitPropRenamed = value; } // Comment moves because this line gets split
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task PrivateMethodAndParameterizedPropertyImplementsMultipleInterfacesAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial interface IBar
{
    int get_ExplicitProp(string str);
    void set_ExplicitProp(string str, int value);
    int ExplicitFunc(ref string str2, int i2);
}

public partial class Foo : IFoo, IBar
{

    private int ExplicitFunc(ref string str, int i)
    {
        return 5;
    }

    int IFoo.ExplicitFunc(ref string str, int i) => ExplicitFunc(ref str, i);
    int IBar.ExplicitFunc(ref string str, int i) => ExplicitFunc(ref str, i);

    private int get_ExplicitProp(string str)
    {
        return 5;
    }
    private void set_ExplicitProp(string str, int value)
    {
    }

    int IFoo.get_ExplicitProp(string str) => get_ExplicitProp(str);
    int IBar.get_ExplicitProp(string str) => get_ExplicitProp(str);
    void IFoo.set_ExplicitProp(string str, int value) => set_ExplicitProp(str, value);
    void IBar.set_ExplicitProp(string str, int value) => set_ExplicitProp(str, value);
}", extension: "cs")
            );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Issue444_InternalMemberDelegatingMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial interface IFoo
{
    int FooDifferentName(ref string str, int i);
}

internal partial class Foo : IFoo
{

    public int BarDifferentName(ref string str, int i)
    {
        return 4;
    }

    int IFoo.FooDifferentName(ref string str, int i) => BarDifferentName(ref str, i);
}
", extension: "cs")
            );
        }
    }



    [Fact]
    public async Task TestReadOnlyOrWriteOnlyPropertyImplementedByNormalPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
internal partial interface IClass
{
    int get_ReadOnlyPropParam(int i);
    int ReadOnlyProp { get; }

    void set_WriteOnlyPropParam(int i, int value);
    int WriteOnlyProp { set; }
}

internal partial class ChildClass : IClass
{

    public virtual int get_RenamedPropertyParam(int i)
    {
        return 1;
    }
    public virtual void set_RenamedPropertyParam(int i, int value)
    {
    }
    int IClass.get_ReadOnlyPropParam(int i) => get_RenamedPropertyParam(i);

    public virtual int RenamedReadOnlyProperty
    {
        get
        {
            return 2;
        }
        set
        {
        }
    }

    int IClass.ReadOnlyProp { get => RenamedReadOnlyProperty; } // Comment moves because this line gets split

    public virtual int get_RenamedWriteOnlyPropParam(int i)
    {
        return 1;
    }
    public virtual void set_RenamedWriteOnlyPropParam(int i, int value)
    {
    }
    void IClass.set_WriteOnlyPropParam(int i, int value) => set_RenamedWriteOnlyPropParam(i, value);

    public virtual int RenamedWriteOnlyProperty
    {
        get
        {
            return 2;
        }
        set
        {
        }
    }

    int IClass.WriteOnlyProp { set => RenamedWriteOnlyProperty = value; } // Comment moves because this line gets split
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestReadOnlyAndWriteOnlyParametrizedPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial interface IClass
{
    string get_ReadOnlyProp(int i);
    void set_WriteOnlyProp(int i, string value);
}

internal partial class ChildClass : IClass
{

    public virtual string get_ReadOnlyProp(int i)
    {
        throw new NotImplementedException();
    }

    public virtual void set_WriteOnlyProp(int i, string value)
    {
        throw new NotImplementedException();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TestExplicitInterfaceOfParametrizedPropertyAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

internal partial interface IClass
{
    string get_ReadOnlyPropToRename(int i);
    void set_WriteOnlyPropToRename(int i, string value);
    string get_PropToRename(int i);
    void set_PropToRename(int i, string value);

    string get_ReadOnlyPropNonPublic(int i);
    void set_WriteOnlyPropNonPublic(int i, string value);
    string get_PropNonPublic(int i);
    void set_PropNonPublic(int i, string value);

    string get_ReadOnlyPropToRenameNonPublic(int i);
    void set_WriteOnlyPropToRenameNonPublic(int i, string value);
    string get_PropToRenameNonPublic(int i);
    void set_PropToRenameNonPublic(int i, string value);

}

internal partial class ChildClass : IClass
{

    public string get_ReadOnlyPropRenamed(int i)
    {
        throw new NotImplementedException();
    }
    string IClass.get_ReadOnlyPropToRename(int i) => get_ReadOnlyPropRenamed(i);

    public virtual void set_WriteOnlyPropRenamed(int i, string value)
    {
        throw new NotImplementedException();
    }
    void IClass.set_WriteOnlyPropToRename(int i, string value) => set_WriteOnlyPropRenamed(i, value);

    public virtual string get_PropRenamed(int i)
    {
        throw new NotImplementedException();
    }
    public virtual void set_PropRenamed(int i, string value)
    {
        throw new NotImplementedException();
    }

    string IClass.get_PropToRename(int i) => get_PropRenamed(i);
    void IClass.set_PropToRename(int i, string value) => set_PropRenamed(i, value);

    private string get_ReadOnlyPropNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    string IClass.get_ReadOnlyPropNonPublic(int i) => get_ReadOnlyPropNonPublic(i);

    protected internal virtual void set_WriteOnlyPropNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }
    void IClass.set_WriteOnlyPropNonPublic(int i, string value) => set_WriteOnlyPropNonPublic(i, value);

    internal virtual string get_PropNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    internal virtual void set_PropNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }

    string IClass.get_PropNonPublic(int i) => get_PropNonPublic(i);
    void IClass.set_PropNonPublic(int i, string value) => set_PropNonPublic(i, value);

    protected internal virtual string get_ReadOnlyPropRenamedNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    string IClass.get_ReadOnlyPropToRenameNonPublic(int i) => get_ReadOnlyPropRenamedNonPublic(i);

    private void set_WriteOnlyPropRenamedNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }
    void IClass.set_WriteOnlyPropToRenameNonPublic(int i, string value) => set_WriteOnlyPropRenamedNonPublic(i, value);

    internal virtual string get_PropToRenameNonPublic(int i)
    {
        throw new NotImplementedException();
    }
    internal virtual void set_PropToRenameNonPublic(int i, string value)
    {
        throw new NotImplementedException();
    }

    string IClass.get_PropToRenameNonPublic(int i) => get_PropToRenameNonPublic(i);
    void IClass.set_PropToRenameNonPublic(int i, string value) => set_PropToRenameNonPublic(i, value);
}", extension: "cs")
            );
        }
    }
}