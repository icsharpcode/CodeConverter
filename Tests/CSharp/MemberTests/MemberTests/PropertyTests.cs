using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;
using System; // For NotImplementedException

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests.MemberTests;

public class PropertyTests : ConverterTestBase
{
    [Fact]
    public async Task TestAbstractMethodAndPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"MustInherit Class TestClass
    Public MustOverride Sub TestMethod()
    Public MustOverride ReadOnly Property AbstractProperty As String
End Class", @"
internal abstract partial class TestClass
{
    public abstract void TestMethod();
    public abstract string AbstractProperty { get; }
}");
    }

    [Fact]
    public async Task TestAbstractReadOnlyAndWriteOnlyPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"MustInherit Class TestClass
        Public MustOverride ReadOnly Property ReadOnlyProp As String
        Public MustOverride WriteOnly Property WriteOnlyProp As String
End Class

Class ChildClass
    Inherits TestClass

    Public Overrides ReadOnly Property ReadOnlyProp As String
    Public Overrides WriteOnly Property WriteOnlyProp As String
        Set
        End Set
    End Property
End Class
", @"
internal abstract partial class TestClass
{
    public abstract string ReadOnlyProp { get; }
    public abstract string WriteOnlyProp { set; }
}

internal partial class ChildClass : TestClass
{

    public override string ReadOnlyProp { get; }
    public override string WriteOnlyProp
    {
        set
        {
        }
    }
}");
    }

    [Fact]
    public async Task TestReadOnlyOrWriteOnlyPropertyImplementedByNormalPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Interface IClass
    ReadOnly Property ReadOnlyPropParam(i as Integer) As Integer
    ReadOnly Property ReadOnlyProp As Integer

    WriteOnly Property WriteOnlyPropParam(i as Integer) As Integer
    WriteOnly Property WriteOnlyProp As Integer
End Interface

Class ChildClass
    Implements IClass

    Public Overridable Property RenamedPropertyParam(i As Integer) As Integer Implements IClass.ReadOnlyPropParam
        Get
            Return 1
        End Get
        Set
        End Set
    End Property

    Public Overridable Property RenamedReadOnlyProperty As Integer Implements IClass.ReadOnlyProp ' Comment moves because this line gets split
        Get
            Return 2
        End Get
        Set
        End Set
    End Property

    Public Overridable Property RenamedWriteOnlyPropParam(i As Integer) As Integer Implements IClass.WriteOnlyPropParam
        Get
            Return 1
        End Get
        Set
        End Set
    End Property

    Public Overridable Property RenamedWriteOnlyProperty As Integer Implements IClass.WriteOnlyProp ' Comment moves because this line gets split
        Get
            Return 2
        End Get
        Set
        End Set
    End Property
End Class
", @"
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
}");
    }

    [Fact]
    public async Task SetterProperty1053Async()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Property Prop(ByVal i As Integer) As String
    Get
        Static bGet As Boolean
        bGet = False
    End Get

    Set(ByVal s As String)
        Static bSet As Boolean
        bSet = False
    End Set
End Property
", @"
internal partial class SurroundingClass
{
    private bool _Prop_bGet;
    private bool _Prop_bSet;

    public string get_Prop(int i)
    {
        _Prop_bGet = false;
        return default;
    }

    public void set_Prop(int i, string value)
    {
        _Prop_bSet = false;
    }

}");
    }

    [Fact]
    public async Task StaticLocalsInPropertyGetterAndSetterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"
Public Property Prop As String
    Get
        Static b As Boolean
        b = True
    End Get

    Set(ByVal s As String)
        Static b As Boolean
        b = False
    End Set
End Property
", @"
internal partial class SurroundingClass
{
    private bool _Prop_b;
    private bool _Prop_b1;

    public string Prop
    {
        get
        {
            _Prop_b = true;
            return default;
        }

        set
        {
            _Prop_b1 = false;
        }
    }

}");
    }

    [Fact]
    public async Task TestReadOnlyAndWriteOnlyParametrizedPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Interface IClass
    ReadOnly Property ReadOnlyProp(i as Integer) As String
    WriteOnly Property WriteOnlyProp(i as Integer) As String
End Interface

Class ChildClass
    Implements IClass

    Public Overridable ReadOnly Property ReadOnlyProp(i As Integer) As String Implements IClass.ReadOnlyProp
        Get
            Throw New NotImplementedException
        End Get
    End Property

    Public Overridable WriteOnly Property WriteOnlyProp(i As Integer) As String Implements IClass.WriteOnlyProp
        Set
            Throw New NotImplementedException
        End Set
    End Property
End Class
", @"using System;

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
}");
    }

    [Fact]
    public async Task TestPropertyStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Readonly Property OtherName() As Integer
        Get
            Static sPrevPosition As Integer = 3 ' Comment moves with declaration
            Console.WriteLine(sPrevPosition)
            Return sPrevPosition
        End Get
    End Property
    Readonly Property OtherName(x As Integer) as Integer
        Get
            Static sPrevPosition As Integer
            sPrevPosition += 1
            Return sPrevPosition
        End Get
    End Property
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _OtherName_sPrevPosition = 3; // Comment moves with declaration
    public int OtherName
    {
        get
        {
            Console.WriteLine(_OtherName_sPrevPosition);
            return _OtherName_sPrevPosition;
        }
    }

    private int _OtherName_sPrevPosition1 = default;
    public int get_OtherName(int x)
    {
        _OtherName_sPrevPosition1 += 1;
        return _OtherName_sPrevPosition1;
    }
}");
    }
}
