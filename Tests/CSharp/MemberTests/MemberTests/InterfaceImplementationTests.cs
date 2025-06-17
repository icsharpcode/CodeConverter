using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // For Out/Optional attributes if used in test signatures
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests.MemberTests // As per prompt
{
    public class InterfaceImplementationTests : ConverterTestBase
    {
        [Fact]
        public async Task TestExplicitInterfaceOfParametrizedPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Interface IClass
    ReadOnly Property ReadOnlyPropToRename(i as Integer) As String
    WriteOnly Property WriteOnlyPropToRename(i as Integer) As String
    Property PropToRename(i as Integer) As String

    ReadOnly Property ReadOnlyPropNonPublic(i as Integer) As String
    WriteOnly Property WriteOnlyPropNonPublic(i as Integer) As String
    Property PropNonPublic(i as Integer) As String

    ReadOnly Property ReadOnlyPropToRenameNonPublic(i as Integer) As String
    WriteOnly Property WriteOnlyPropToRenameNonPublic(i as Integer) As String
    Property PropToRenameNonPublic(i as Integer) As String

End Interface

Class ChildClass
    Implements IClass

    Public ReadOnly Property ReadOnlyPropRenamed(i As Integer) As String Implements IClass.ReadOnlyPropToRename
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overridable WriteOnly Property WriteOnlyPropRenamed(i As Integer) As String Implements IClass.WriteOnlyPropToRename
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Public Overridable Property PropRenamed(i As Integer) As String Implements IClass.PropToRename
        Get
            Throw New NotImplementedException
        End Get
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Private ReadOnly Property ReadOnlyPropNonPublic(i As Integer) As String Implements IClass.ReadOnlyPropNonPublic
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Protected Friend Overridable WriteOnly Property WriteOnlyPropNonPublic(i As Integer) As String Implements IClass.WriteOnlyPropNonPublic
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Friend Overridable Property PropNonPublic(i As Integer) As String Implements IClass.PropNonPublic
        Get
            Throw New NotImplementedException
        End Get
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Protected Friend Overridable ReadOnly Property ReadOnlyPropRenamedNonPublic(i As Integer) As String Implements IClass.ReadOnlyPropToRenameNonPublic
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Private WriteOnly Property WriteOnlyPropRenamedNonPublic(i As Integer) As String Implements IClass.WriteOnlyPropToRenameNonPublic
        Set
            Throw New NotImplementedException
        End Set
    End Property

    Friend Overridable Property PropToRenameNonPublic(i As Integer) As String Implements IClass.PropToRenameNonPublic
        Get
            Throw New NotImplementedException
        End Get
        Set
            Throw New NotImplementedException
        End Set
    End Property
End Class
", @"using System;

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
}");
        }

        [Fact]
        public async Task Issue443_FixCaseForInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function FooDifferentCase(<Out> ByRef str2 As String) As Integer
End Interface

Public Class Foo
    Implements IFoo
    Function fooDifferentCase(<Out> ByRef str2 As String) As Integer Implements IFoo.FOODIFFERENTCASE
        str2 = 2.ToString()
        Return 3
    End Function
End Class", @"
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
");
        }

        [Fact]
        public async Task Issue444_FixNameForRenamedInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function FooDifferentName(ByRef str As String, i As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Function BarDifferentName(ByRef str As String, i As Integer) As Integer Implements IFoo.FooDifferentName
        Return 4
    End Function
End Class", @"
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
");
        }

        [Fact]
        public async Task IdenticalInterfaceMethodsWithRenamedInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Interface IBar
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Function Foo(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFooBar
        Return 4
    End Function

    Function Bar(ByRef str As String, i As Integer) As Integer Implements IBar.DoFooBar
        Return 2
    End Function

End Class", @"
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
");
        }

        [Fact]
        public async Task RenamedInterfaceCasingOnlyDifferenceConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Function DoFoo() As Integer
    Property Prop As Integer
End Interface

Public Class Foo
    Implements IFoo

    Private Function doFoo() As Integer Implements IFoo.DoFoo
        Return 4
    End Function

    Private Property prop As Integer Implements IFoo.Prop

    Private Function Consumer() As Integer
        Dim foo As New Foo()
        Dim interfaceInstance As IFoo = foo
        Return foo.doFoo() + foo.DoFoo() +
               interfaceInstance.doFoo() + interfaceInstance.DoFoo() +
               foo.prop + foo.Prop +
               interfaceInstance.prop + interfaceInstance.Prop
    End Function

End Class", @"
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
");
        }

        [Fact]
        public async Task RenamedInterfaceCasingOnlyDifferenceForVirtualMemberConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Function DoFoo() As Integer
    Property Prop As Integer
End Interface

Public MustInherit Class BaseFoo
    Implements IFoo

    Protected Friend Overridable Function doFoo() As Integer Implements IFoo.DoFoo
        Return 4
    End Function

    Protected Friend Overridable Property prop As Integer Implements IFoo.Prop

End Class

Public Class Foo
    Inherits BaseFoo

    Protected Friend Overrides Function DoFoo() As Integer
        Return 5
    End Function

    Protected Friend Overrides Property Prop As Integer

    Private Function Consumer() As Integer
        Dim foo As New Foo()
        Dim interfaceInstance As IFoo = foo
        Dim baseClass As BaseFoo = foo
        Return foo.doFoo() +  foo.DoFoo() +
               interfaceInstance.doFoo() + interfaceInstance.DoFoo() +
               baseClass.doFoo() + baseClass.DoFoo() +
               foo.prop + foo.Prop +
               interfaceInstance.prop + interfaceInstance.Prop +
               baseClass.prop + baseClass.Prop
    End Function
End Class", @"
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
");
        }

        [Fact]
        public async Task RenamedInterfaceCasingOnlyDifferenceWithOverloadedPropertyConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IUserContext
    ReadOnly Property GroupID As String
End Interface

Public Interface IFoo
    ReadOnly Property ConnectedGroupId As String
End Interface

Public MustInherit Class BaseFoo
    Implements IUserContext

    Protected Friend ReadOnly Property ConnectedGroupID() As String Implements IUserContext.GroupID

End Class

Public Class Foo
    Inherits BaseFoo
    Implements IFoo

    Protected Friend Overloads ReadOnly Property ConnectedGroupID As String Implements IFoo.ConnectedGroupId ' Comment moves because this line gets split
        Get
            Return If("""", MyBase.ConnectedGroupID())
        End Get
    End Property

    Private Function Consumer() As String
        Dim foo As New Foo()
        Dim ifoo As IFoo = foo
        Dim baseFoo As BaseFoo = foo
        Dim iUserContext As IUserContext = foo
        Return foo.ConnectedGroupID & foo.ConnectedGroupId &
               iFoo.ConnectedGroupID & iFoo.ConnectedGroupId &
               baseFoo.ConnectedGroupID & baseFoo.ConnectedGroupId &
               iUserContext.GroupId & iUserContext.GroupID
    End Function

End Class", @"
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
");
        }

        [Fact]
        public async Task RenamedMethodImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Interface IBar
    Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Function Foo(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFooBar, IBar.DoFooBar
        Return 4
    End Function

End Class", @"
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

}");
        }

        [Fact]
        public async Task IdenticalInterfacePropertiesWithRenamedInterfaceMembersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
        Property FooBarProp As Integer
    End Interface

Public Interface IBar
    Property FooBarProp As Integer
End Interface

Public Class FooBar
    Implements IFoo, IBar

    Property Foo As Integer Implements IFoo.FooBarProp

    Property Bar As Integer Implements IBar.FooBarProp

End Class", @"
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

}");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationRequiredMethodParameters_749_Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
  Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Interface IBar
  Function DoFooBar(ByRef str As String, i As Integer) As Integer
End Interface

Public Class FooBar
  Implements IFoo, IBar

  Function Foo(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFooBar
    Return 4
  End Function

  Function Bar(ByRef str As String, i As Integer) As Integer Implements IBar.DoFooBar
    Return 2
  End Function

End Class", @"
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
");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationOptionalParameters_1062_Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface InterfaceWithOptionalParameters
    Sub S(Optional i As Integer = 0)
End Interface

Public Class ImplInterfaceWithOptionalParameters : Implements InterfaceWithOptionalParameters
    Public Sub InterfaceWithOptionalParameters_S(Optional i As Integer = 0) Implements InterfaceWithOptionalParameters.S
    End Sub
End Class", @"
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
");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationOptionalParametersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
  Property ExplicitProp(Optional str As String = """") As Integer
  Function ExplicitFunc(Optional str2 As String = """", Optional i2 As Integer = 1) As Integer
End Interface

Public Class Foo
  Implements IFoo

  Private Function ExplicitFunc(Optional str As String = """", Optional i2 As Integer = 1) As Integer Implements IFoo.ExplicitFunc
    Return 5
  End Function

  Private Property ExplicitProp(Optional str As String = """") As Integer Implements IFoo.ExplicitProp
    Get
      Return 5
    End Get
    Set(value As Integer)
    End Set
  End Property
End Class", @"
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
");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationOptionalMethodParameters_749_Async()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
  Function DoFooBar(ByRef str As String, Optional i As Integer = 4) As Integer
End Interface

Public Interface IBar
  Function DoFooBar(ByRef str As String, Optional i As Integer = 8) As Integer
End Interface

Public Class FooBar
  Implements IFoo, IBar

  Function Foo(ByRef str As String, Optional i As Integer = 4) As Integer Implements IFoo.DoFooBar
    Return 4
  End Function

  Function Bar(ByRef str As String, Optional i As Integer = 8) As Integer Implements IBar.DoFooBar
    Return 2
  End Function

End Class", @"
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
");
        }

        [Fact]
        public async Task RenamedInterfaceMethodFullyQualifiedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace TestNamespace
    Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface
End Namespace

Public Class Foo
    Implements TestNamespace.IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements TestNamespace.IFoo.DoFoo
        Return 4
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task RenamedInterfacePropertyFullyQualifiedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Namespace TestNamespace
    Public Interface IFoo
        Property FooProp As Integer
    End Interface
End Namespace

Public Class Foo
    Implements TestNamespace.IFoo

    Property FooPropRenamed As Integer Implements TestNamespace.IFoo.FooProp

End Class", @"
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

}");
        }

        [Fact]
        public async Task RenamedInterfaceMethodConsumerCasingRenamedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.DOFOORENAMED(str, i) + bar.DoFoo(str, i)
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task RenamedInterfacePropertyConsumerCasingRenamedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp

End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.FOOPROPRENAMED + bar.FooProp
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task InterfaceMethodCasingRenamedConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function dofoo(str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(str As String, i As Integer) As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.dofoo(str, i) + bar.DoFoo(str, i)
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task InterfacePropertyCasingRenamedConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property fooprop As Integer Implements IFoo.FooProp

End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.fooprop + bar.FooProp
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task InterfaceRenamedMethodConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.DoFooRenamed(str, i) + bar.DoFoo(str, i)
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task InterfaceRenamedPropertyConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp

End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.FooPropRenamed + bar.FooProp
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task PartialInterfaceRenamedMethodConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Partial Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo
        Return 4
    End Function
End Class

Public Class FooConsumer
    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.DoFooRenamed(str, i) + bar.DoFoo(str, i)
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task PartialInterfaceRenamedPropertyConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Partial Interface IFoo
        Property FooProp As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Property FooPropRenamed As Integer Implements IFoo.FooProp

End Class

Public Class FooConsumer
    Function GetFooRenamed() As Integer
        Dim foo As New Foo
        Dim bar As IFoo = foo
        Return foo.FooPropRenamed + bar.FooProp
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task RenamedInterfaceMethodMyClassConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        Function DoFoo(ByRef str As String, i As Integer) As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Overridable Function DoFooRenamed(ByRef str As String, i As Integer) As Integer Implements IFoo.DoFoo ' Comment ends up out of order, but attached to correct method
        Return 4
    End Function

    Function DoFooRenamedConsumer(ByRef str As String, i As Integer) As Integer
        Return MyClass.DoFooRenamed(str, i)
    End Function
End Class", @"
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
}");
        }

        [Fact]
        public async Task RenamedInterfacePropertyMyClassConsumerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Public Interface IFoo
        ReadOnly Property DoFoo As Integer
        WriteOnly Property DoBar As Integer
    End Interface

Public Class Foo
    Implements IFoo

    Overridable ReadOnly Property DoFooRenamed As Integer Implements IFoo.DoFoo  ' Comment ends up out of order, but attached to correct method
        Get
            Return 4
        End Get
    End Property

    Overridable WriteOnly Property DoBarRenamed As Integer Implements IFoo.DoBar  ' Comment ends up out of order, but attached to correct method
        Set
            Throw New Exception()
        End Set
    End Property

    Sub DoFooRenamedConsumer()
        MyClass.DoBarRenamed = MyClass.DoFooRenamed
    End Sub
End Class", @"using System;

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
}");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Private Function ExplicitFunc(ByRef str As String, i As Integer) As Integer Implements IFoo.ExplicitFunc
        Return 5
    End Function

    Private Property ExplicitProp(str As String) As Integer Implements IFoo.ExplicitProp
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class", @"
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
");
        }

        [Fact]
        public async Task PropertyInterfaceImplementationKeepsVirtualModifierAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property PropParams(str As String) As Integer
    Property Prop() As Integer
End Interface

Public Class Foo
    Implements IFoo

    Public Overridable Property PropParams(str As String) As Integer Implements IFoo.PropParams
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Overridable Property Prop As Integer Implements IFoo.Prop
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class", @"
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
");
        }

        [Fact]
        public async Task PrivateAutoPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp As Integer
End Interface

Public Interface IBar
    Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    Private Property ExplicitProp As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
End Class", @"
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
}");
        }


        [Fact]
        public async Task ImplementMultipleRenamedPropertiesFromInterfaceAsAbstractAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Property ExplicitProp As Integer
End Interface
Public Interface IBar
    Property ExplicitProp As Integer
End Interface
Public MustInherit Class Foo
    Implements IFoo, IBar

    Protected MustOverride Property ExplicitPropRenamed1 As Integer Implements IFoo.ExplicitProp
    Protected MustOverride Property ExplicitPropRenamed2 As Integer Implements IBar.ExplicitProp
End Class", @"
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
}");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationForVirtualMemberFromAnotherClassAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Sub Save()
    Property Prop As Integer
End Interface

Public MustInherit Class BaseFoo
    Protected Overridable Sub OnSave()
    End Sub

    Protected Overridable Property MyProp As Integer = 5
End Class

Public Class Foo
    Inherits BaseFoo
    Implements IFoo

    Protected Overrides Sub OnSave() Implements IFoo.Save
    End Sub

    Protected Overrides Property MyProp As Integer = 6 Implements IFoo.Prop

End Class", @"
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

}");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationWhereOnlyOneInterfaceMemberIsRenamedAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Sub Save()
    Property A As Integer
End Interface

Public Interface IBar
    Sub OnSave()
    Property B As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    Public Overridable Sub Save() Implements IFoo.Save, IBar.OnSave
    End Sub

    Public Overridable Property A As Integer Implements IFoo.A, IBar.B

End Class", @"
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

}");
        }

        [Fact]
        public async Task ExplicitInterfaceImplementationWhereMemberShadowsBaseAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Sub Save()
    Property Prop As Integer
End Interface

Public MustInherit Class BaseFoo
    Public Overridable Sub OnSave()
    End Sub

    Public Overridable Property MyProp As Integer = 5
End Class

Public Class Foo
    Inherits BaseFoo
    Implements IFoo

    Public Shadows Sub OnSave() Implements IFoo.Save
    End Sub

    Public Shadows Property MyProp As Integer = 6 Implements IFoo.Prop

End Class", @"
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

}");
        }

        [Fact]
        public async Task PrivatePropertyAccessorBlocksImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp As Integer
End Interface

Public Interface IBar
    Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    Private Property ExplicitProp As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp ' Comment moves because this line gets split
        Get
          Return 5
        End Get
        Set
        End Set
    End Property
End Class", @"
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
}");
        }

        [Fact]
        public async Task NonPublicImplementsInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property FriendProp As Integer
    Sub ProtectedSub()
    Function PrivateFunc() As Integer
    Sub ProtectedInternalSub()
    Sub AbstractSub()
End Interface

Public Interface IBar
    Property FriendProp As Integer
    Sub ProtectedSub()
    Function PrivateFunc() As Integer
    Sub ProtectedInternalSub()
    Sub AbstractSub()
End Interface

Public MustInherit Class BaseFoo
    Implements IFoo, IBar

    Friend Overridable Property FriendProp As Integer Implements IFoo.FriendProp, IBar.FriendProp ' Comment moves because this line gets split
        Get
          Return 5
        End Get
        Set
        End Set
    End Property

    Protected Sub ProtectedSub() Implements IFoo.ProtectedSub, IBar.ProtectedSub
    End Sub

    Private Function PrivateFunc() As Integer Implements IFoo.PrivateFunc, IBar.PrivateFunc
    End Function

    Protected Friend Overridable Sub ProtectedInternalSub() Implements IFoo.ProtectedInternalSub, IBar.ProtectedInternalSub
    End Sub

    Protected MustOverride Sub AbstractSubRenamed() Implements IFoo.AbstractSub, IBar.AbstractSub
End Class

Public Class Foo
    Inherits BaseFoo

    Protected Friend Overrides Sub ProtectedInternalSub()
    End Sub

    Protected Overrides Sub AbstractSubRenamed()
    End Sub
End Class
", @"
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
}");
        }

        [Fact]
        public async Task ExplicitPropertyImplementationWithDirectAccessAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"
Public Interface IFoo
    Property ExplicitProp As Integer
    ReadOnly Property ExplicitReadOnlyProp As Integer
End Interface

Public Class Foo
    Implements IFoo

    Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp
    ReadOnly Property ExplicitRenamedReadOnlyProp As Integer Implements IFoo.ExplicitReadOnlyProp

    Private Sub Consumer()
        _ExplicitPropRenamed = 5
        _ExplicitRenamedReadOnlyProp = 10
    End Sub

End Class", @"
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

}");
        }

        [Fact]
        public async Task ReadonlyRenamedPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    ReadOnly Property ExplicitProp As Integer
End Interface

Public Interface IBar
    ReadOnly Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    ReadOnly Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
End Class", @"
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
}");
        }

        [Fact]
        public async Task WriteonlyPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    WriteOnly Property ExplicitProp As Integer
End Interface

Public Interface IBar
    WriteOnly Property ExplicitProp As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    WriteOnly Property ExplicitPropRenamed As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp ' Comment moves because this line gets split
        Set
        End Set
    End Property
End Class", @"
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
}");
        }

        [Fact]
        public async Task PrivateMethodAndParameterizedPropertyImplementsMultipleInterfacesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Interface IBar
    Property ExplicitProp(str As String) As Integer
    Function ExplicitFunc(ByRef str2 As String, i2 As Integer) As Integer
End Interface

Public Class Foo
    Implements IFoo, IBar

    Private Function ExplicitFunc(ByRef str As String, i As Integer) As Integer Implements IFoo.ExplicitFunc, IBar.ExplicitFunc
        Return 5
    End Function

    Private Property ExplicitProp(str As String) As Integer Implements IFoo.ExplicitProp, IBar.ExplicitProp
        Get
            Return 5
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class", @"
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
}");
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Issue444_InternalMemberDelegatingMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Public Interface IFoo
    Function FooDifferentName(ByRef str As String, i As Integer) As Integer
End Interface

Friend Class Foo
    Implements IFoo

    Function BarDifferentName(ByRef str As String, i As Integer) As Integer Implements IFoo.FooDifferentName
        Return 4
    End Function
End Class", @"
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
");
        }
    }
}
