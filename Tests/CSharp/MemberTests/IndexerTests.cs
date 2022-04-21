using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class IndexerTests : ConverterTestBase
{
    [Fact]
    public async Task InterfaceImplementationOfIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Interface IFoo
    Default Property Item(str As String) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Default Overridable Public Property Item(str As String) As Integer Implements IFoo.Item
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class", @"
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
");
    }

    [Fact]
    public async Task InterfaceImplementationOfIndexerAsAbstractAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Interface IFoo
    Default Property Item(str As String) As Integer
End Interface

Public MustInherit Class Foo
    Implements IFoo

    Default Public MustOverride Property Item(str As String) As Integer Implements IFoo.Item
End Class

Public Class FooChild
    Inherits Foo

    Default Public Overrides Property Item(str As String) As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class", @"
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
");
    }

    [Fact]
    public async Task RenamedImplementationOfIndexerWithAbstractAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Interface IFoo
    Default Property Item(str As String) As Integer
End Interface

Public MustInherit Class Foo
    Implements IFoo

    Default Public MustOverride Property ItemRenamed(str As String) As Integer Implements IFoo.Item
End Class

Public Class FooChild
    Inherits Foo

    Default Public Overrides Property ItemRenamed(str As String) As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
End Class", @"
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
");
    }

    [Fact]
    public async Task ReadOnlyImplementationOfIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Interface IFoo
    Default ReadOnly Property Item(str As String) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Default Public Overridable ReadOnly Property Item(str As String) As Integer Implements IFoo.Item
    Get
        Return 2
    End Get
    End Property
End Class", @"
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
");
    }

    [Fact]
    public async Task WriteOnlyImplementationOfIndexerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Interface IFoo
    Default WriteOnly Property Item(str As String) As Integer
End Interface

Public Class Foo
    Implements IFoo

    Default Public Overridable WriteOnly Property Item(str As String) As Integer Implements IFoo.Item
    Set
    End Set
    End Property
End Class", @"
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
");
    }
}