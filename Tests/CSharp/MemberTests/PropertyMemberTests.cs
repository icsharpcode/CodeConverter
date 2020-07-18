using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests
{
    public class PropertyMemberTests : ConverterTestBase
    {


        [Fact]
        public async Task TestPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Property Test As Integer

    Public Property Test2 As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Public Property Test3 As Integer
        Get
            If 7 = Integer.Parse(""7"") Then Exit Property
            Return Me.m_test3
        End Get
        Set(ByVal value As Integer)
            If 7 = Integer.Parse(""7"") Then Exit Property
            Me.m_test3 = value
        End Set
    End Property
End Class", @"
internal partial class TestClass
{
    public int Test { get; set; }

    public int Test2
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int Test3
    {
        get
        {
            if (7 == int.Parse(""7""))
                return default;
            return m_test3;
        }

        set
        {
            if (7 == int.Parse(""7""))
                return;
            m_test3 = value;
        }
    }
}
1 source compilation errors:
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.");
        }

        [Fact]
        public async Task TestParameterizedPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Property FirstName As String
    Public Property LastName As String

    Public Property FullName(ByVal lastNameFirst As Boolean, ByVal isFirst As Boolean) As String
        Get
            If lastNameFirst Then
                Return LastName & "" "" & FirstName
            Else
                Return FirstName & "" "" & LastName
            End If
        End Get

        Friend Set
            If isFirst Then FirstName = Value
        End Set
    End Property

    Public Overrides Function ToString() As String
        FullName(False, True) = ""hello""
        Return FullName(False, True)
    End Function
End Class", @"
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool lastNameFirst, bool isFirst)
    {
        if (lastNameFirst)
        {
            return LastName + "" "" + FirstName;
        }
        else
        {
            return FirstName + "" "" + LastName;
        }
    }

    internal void set_FullName(bool lastNameFirst, bool isFirst, string value)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(false, true, ""hello"");
        return get_FullName(false, true);
    }
}", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for parameterized property
        }

        [Fact]
        public async Task TestOptionalParameterizedPropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Public Property FirstName As String
    Public Property LastName As String

    Public Property FullName(Optional ByVal isFirst As Boolean = False) As String
        Get
            Return FirstName & "" "" & LastName
        End Get

        Friend Set
            If isFirst Then FirstName = Value
        End Set
    End Property

    Public Overrides Function ToString() As String
        FullName(True) = ""hello2""
        FullName() = ""hello3""
        FullName = ""hello4""
        Return FullName
    End Function
End Class", @"
internal partial class TestClass
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string get_FullName(bool isFirst)
    {
        return FirstName + "" "" + LastName;
    }

    internal void set_FullName(bool isFirst, string value)
    {
        if (isFirst)
            FirstName = value;
    }

    public override string ToString()
    {
        set_FullName(true, ""hello2"");
        set_FullName(false, ""hello3"");
        set_FullName(false, ""hello4"");
        return get_FullName(false);
    }
}",
hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for parameterized property
        }

        [Fact]
        public async Task TestParameterizedPropertyAndGenericInvocationAndEnumEdgeCasesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class ParameterizedPropertiesAndEnumTest
    Public Enum MyEnum
        First
    End Enum

    Public Property MyProp(ByVal blah As Integer) As String
        Get
            Return blah
        End Get
        Set
        End Set
    End Property


    Public Sub ReturnWhatever(ByVal m As MyEnum)
        Dim enumerableThing = Enumerable.Empty(Of String)
        Select Case m
            Case -1
                Exit Sub
            Case MyEnum.First
                Exit Sub
            Case 3
                Me.MyProp(4) = enumerableThing.ToArray()(m)
                Exit Sub
        End Select
    End Sub
End Class", @"using System.Linq;

public partial class ParameterizedPropertiesAndEnumTest
{
    public enum MyEnum
    {
        First
    }

    public string get_MyProp(int blah)
    {
        return blah.ToString();
    }

    public void set_MyProp(int blah, string value)
    {
    }

    public void ReturnWhatever(MyEnum m)
    {
        var enumerableThing = Enumerable.Empty<string>();
        switch (m)
        {
            case (MyEnum)(-1):
                {
                    return;
                }

            case MyEnum.First:
                {
                    return;
                }

            case (MyEnum)3:
                {
                    set_MyProp(4, enumerableThing.ToArray()[(int)m]);
                    return;
                }
        }
    }
}", hasLineCommentConversionIssue: true);//TODO: Improve comment mapping for parameterized property
        }

        [Fact]
        public async Task PropertyWithMissingTypeDeclarationAsync()//TODO Check object is the inferred type
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class MissingPropertyType
                ReadOnly Property Max
                    Get
                        Dim mx As Double = 0
                        Return mx
                    End Get
                End Property
End Class", @"
internal partial class MissingPropertyType
{
    public object Max
    {
        get
        {
            double mx = 0;
            return mx;
        }
    }
}");
        }

        [Fact]
        public async Task TestReadWriteOnlyInterfacePropertyAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Interface Foo
    ReadOnly Property P1() As String
    WriteOnly Property P2() As String
End Interface", @"
public partial interface Foo
{
    string P1 { get; }
    string P2 { set; }
}");
        }

        [Fact]
        public async Task SynthesizedBackingFieldAccessAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Shared Property First As Integer

    Private Second As Integer = _First
End Class", @"
internal partial class TestClass
{
    private static int First { get; set; }

    private int Second = First;
}");
        }

        [Fact]
        public async Task PropertyInitializersAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private ReadOnly Property First As New List(Of String)
    Private Property Second As Integer = 0
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    private List<string> First { get; set; } = new List<string>();
    private int Second { get; set; } = 0;
}");
        }

        [Fact]
        public async Task TestIndexerAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Private _Items As Integer()

    Default Public Property Item(ByVal index As Integer) As Integer
        Get
            Return _Items(index)
        End Get
        Set(ByVal value As Integer)
            _Items(index) = value
        End Set
    End Property

    Default Public ReadOnly Property Item(ByVal index As String) As Integer
        Get
            Return 0
        End Get
    End Property

    Private m_test3 As Integer

    Default Public Property Item(ByVal index As Double) As Integer
        Get
            Return Me.m_test3
        End Get
        Set(ByVal value As Integer)
            Me.m_test3 = value
        End Set
    End Property
End Class", @"
internal partial class TestClass
{
    private int[] _Items;

    public int this[int index]
    {
        get
        {
            return _Items[index];
        }

        set
        {
            _Items[index] = value;
        }
    }

    public int this[string index]
    {
        get
        {
            return 0;
        }
    }

    private int m_test3;

    public int this[double index]
    {
        get
        {
            return m_test3;
        }

        set
        {
            m_test3 = value;
        }
    }
}");
        }

        [Fact]
        public async Task TestWriteOnlyPropertiesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Interface TestInterface
    WriteOnly Property Items As Integer()
End Interface", @"
internal partial interface TestInterface
{
    int[] Items { set; }
}");
        }

        [Fact]
        public async Task TestImplicitPrivateSetterAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class SomeClass
    Public ReadOnly Property SomeValue As Integer

    Public Sub SetValue(value1 As Integer, value2 As Integer)
        _SomeValue = value1 + value2
    End Sub
End Class", @"
public partial class SomeClass
{
    public int SomeValue { get; private set; }

    public void SetValue(int value1, int value2)
    {
        SomeValue = value1 + value2;
    }
}");
        }

        [Fact]
        public async Task TestSetWithNamedParameterPropertiesAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Class TestClass
    Private _Items As Integer()
    Property Items As Integer()
        Get
            Return _Items
        End Get
        Set(v As Integer())
            _Items = v
        End Set
    End Property
End Class", @"
internal partial class TestClass
{
    private int[] _Items;

    public int[] Items
    {
        get
        {
            return _Items;
        }

        set
        {
            _Items = value;
        }
    }
}");
        }

        [Fact]
        public async Task TestPropertyAssignmentReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class Class1
    Public ReadOnly Property Foo() As String
        Get
            Foo = """"
        End Get
    End Property
    Public ReadOnly Property X As String
        Get
            X = 4
            X = X * 2
            Dim y = ""random variable to check it isn't just using the value of the last statement""
        End Get
    End Property
    Public _y As String
    Public WriteOnly Property Y As String
        Set(value As String)
            If value <> """" Then
                Y = """"
            Else
                _y = """"
            End If
        End Set
    End Property
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class Class1
{
    public string Foo
    {
        get
        {
            string FooRet = default;
            FooRet = """";
            return FooRet;
        }
    }

    public string X
    {
        get
        {
            string XRet = default;
            XRet = 4.ToString();
            XRet = (Conversions.ToDouble(XRet) * 2).ToString();
            string y = ""random variable to check it isn't just using the value of the last statement"";
            return XRet;
        }
    }

    public string _y;

    public string Y
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Y = """";
            }
            else
            {
                _y = """";
            }
        }
    }
}");
        }

        [Fact]
        public async Task TestGetIteratorDoesNotGainReturnAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class VisualBasicClass
  Public Shared ReadOnly Iterator Property SomeObjects As IEnumerable(Of Object())
    Get
      Yield New Object(2) {}
      Yield New Object(2) {}
    End Get
  End Property
End Class", @"using System.Collections.Generic;

public partial class VisualBasicClass
{
    public static IEnumerable<object[]> SomeObjects
    {
        get
        {
            yield return new object[3];
            yield return new object[3];
        }
    }
}");
        }
    }
}
