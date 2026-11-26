using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ParameterTests : ConverterTestBase
{
    [Fact]
    public async Task OptionalParameter_DoesNotThrowInvalidCastExceptionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class MyTestAttribute
    Inherits Attribute
End Class

Public Class MyController
    Public Function GetNothing(
        <MyTest()> Optional indexer As Integer? = 0
    ) As String
        Return Nothing
    End Function
End Class
",
            @"using System;

public partial class MyTestAttribute : Attribute
{
}

public partial class MyController
{
    public string GetNothing([MyTest()] int? indexer = 0)
    {
        return null;
    }
}");
    }

    [Fact]
    public async Task OptionalLastParameter_ExpandsOptionalOmittedArgAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestClass
    Public Sub M(a As String)
    End Sub
    Public Sub M(a As String, Optional b as String = ""smth"")
    End Sub
    
    Public Sub Test()
        M(""x"",)
    End Sub
End Class
",
            @"
public partial class TestClass
{
    public void M(string a)
    {
    }
    public void M(string a, string b = ""smth"")
    {
    }

    public void Test()
    {
        M(""x"", ""smth"");
    }
}");
    }

    [Fact]
    public async Task OptionalFirstParameter_ExpandsOptionalOmittedArgAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestClass
    Public Sub M(a As String)
    End Sub
    Public Sub M(Optional a As String = ""ss"", Optional b as String = ""smth"")
    End Sub
    
    Public Sub Test()
        M(,""x"")
    End Sub
End Class
",
            @"
public partial class TestClass
{
    public void M(string a)
    {
    }
    public void M(string a = ""ss"", string b = ""smth"")
    {
    }

    public void Test()
    {
        M(""ss"", ""x"");
    }
}");
    }

    [Fact]
    public async Task OmittedArgumentAfterNamedArgument_WhenMethodHasCollidingOverload_ShouldExpandAllOptionalArgsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestClass
    Public Sub M(a As String, b as string)
    End Sub
    Public Sub M(Optional a As String = ""1"", Optional b as string = ""2"", Optional c as string = ""3"")
    End Sub

    Public Sub Test()
        M(a:=""4"", )
    End Sub
End Class
",
            @"
public partial class TestClass
{
    public void M(string a, string b)
    {
    }
    public void M(string a = ""1"", string b = ""2"", string c = ""3"")
    {
    }

    public void Test()
    {
        M(a: ""4"", ""2"", c: ""3"");
    }
}");
    }

    [Fact]
    public async Task FractionalDefaultIsRoundedButOtherDefaultsKeepTheirExpressionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Const D As Double = 1.5
    Const X As Integer = 5
    Sub M(Optional a As Integer = 1.5, Optional y As Long = D, Optional b As Byte = X + 1, Optional c As Long = X * 2, Optional ui As UInteger = X, Optional dm As Decimal = X, Optional sb As SByte = -X, Optional n As Integer? = Nothing)
    End Sub
    Sub R(Optional ByRef r As Integer = 2.5, Optional ByRef s As Integer = X)
    End Sub
End Class", @"using System.Runtime.InteropServices;

internal partial class C
{
    private const double D = 1.5d;
    private const int X = 5;
    public void M(int a = 2, long y = 2L, byte b = X + 1, long c = X * 2, uint ui = X, decimal dm = X, sbyte sb = -X, int? n = default)
    {
    }
    public void R([Optional, DefaultParameterValue(2)] ref int r, [Optional, DefaultParameterValue(X)] ref int s)
    {
    }
}");
    }

    [Fact]
    public async Task ConstantDefaultIsCastOrFoldedOnlyWhereCSharpNeedsItAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum Col
    Red
    Green
End Enum

Class C
    Const L As Long = 5
    Const D As Double = 1.5
    Sub M(Optional a1 As Integer = L, Optional c As Integer = Col.Green, Optional b As Single = D, Optional n As Col = 1,
          Optional h As Integer? = 2.5, Optional ls As Long? = D, Optional z As Short = True << 3, Optional c2 As Col = 1.5, Optional i As Integer = True)
    End Sub
    Sub O(Optional a As Object = 1.5, Optional ByRef b As Short = 3, Optional ByRef s As String = ""a"")
    End Sub
End Class", @"using System.Runtime.InteropServices;

internal enum Col
{
    Red,
    Green
}

internal partial class C
{
    private const long L = 5L;
    private const double D = 1.5d;
    public void M(int a1 = (int)L, int c = (int)Col.Green, float b = (float)D, Col n = (Col)1, int? h = 2, long? ls = 2L, short z = -8, Col c2 = (Col)2, int i = -1)
    {
    }
    public void O([Optional, DefaultParameterValue(1.5d)] object a, [Optional, DefaultParameterValue((short)3)] ref short b, [Optional, DefaultParameterValue(""a"")] ref string s)
    {
    }
}");
    }

    [Fact]
    public async Task DefaultsAfterADateDefaultAreAttributesTooAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Sub M(Optional d As Date = #1/2/2000#, Optional i As Integer = 3, Optional s As String = Nothing)
    End Sub
End Class", @"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class C
{
    public void M([Optional, DateTimeConstant(630823680000000000L/* #1/2/2000# */)] DateTime d, [Optional, DefaultParameterValue(3)] int i, [Optional] string s)
    {
    }
}");
    }

    [Fact]
    public async Task DecimalDefaultInAttributeFormIsADecimalConstantAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Sub M(Optional o As Object = 1, Optional d As Decimal = 1.5D, Optional n As Decimal? = -2.25D)
    End Sub
    Sub R(Optional ByRef d As Decimal = 2.5D)
    End Sub
End Class", @"using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class C
{
    public void M([Optional, DefaultParameterValue(1)] object o, [Optional, DecimalConstant(1, 0, 0U, 0U, 15U)] decimal d, [Optional, DecimalConstant(2, 1, 0U, 0U, 225U)] decimal? n)
    {
    }
    public void R([Optional, DecimalConstant(1, 0, 0U, 0U, 25U)] ref decimal d)
    {
    }
}");
    }

    [Fact]
    public async Task ConstantWithoutACSharpConversionIsVbsValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum Col
    Red
    Green
End Enum

Class TAttribute
    Inherits System.Attribute
    Public Property B As Boolean
End Class

<T(B:=Col.Green)>
Class C
    Sub M(Optional b As Boolean = Col.Green, Optional e As Col = True, Optional s As String = ChrW(66) & ""b"", Optional d As Double = Col.Green)
    End Sub
End Class", @"using System;

internal enum Col
{
    Red,
    Green
}

internal partial class TAttribute : Attribute
{
    public bool B { get; set; }
}

[T(B = true)]
internal partial class C
{
    public void M(bool b = true, Col e = (Col)(-1), string s = ""Bb"", double d = (double)Col.Green)
    {
    }
}");
    }

    [Fact]
    public async Task OmittedOptionalByRefEnumArgumentIsCastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class C
    Sub M(Optional ByRef e As DayOfWeek = 1, Optional ByRef n As DayOfWeek? = 2)
    End Sub
    Sub Caller()
        M()
    End Sub
End Class", @"using System;
using System.Runtime.InteropServices;

internal partial class C
{
    public void M([Optional, DefaultParameterValue((DayOfWeek)1)] ref DayOfWeek e, [Optional, DefaultParameterValue((DayOfWeek)2)] ref DayOfWeek? n)
    {
    }
    public void Caller()
    {
        DayOfWeek arge = (DayOfWeek)1;
        DayOfWeek? argn = (DayOfWeek)2;
        M(e: ref arge, n: ref argn);
    }
}");
    }

    [Fact]
    public async Task ObjectConstantThatIsntACSharpConstantIsVbsValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class AAttribute
    Inherits System.Attribute
    Public O As Object
End Class

<A(O:=""a""c & ""b"")>
Class C
    Sub M(Optional o As Object = ""a""c & ""b"")
    End Sub
End Class", @"using System;
using System.Runtime.InteropServices;

internal partial class AAttribute : Attribute
{
    public object O;
}

[A(O = ""ab"")]
internal partial class C
{
    public void M([Optional, DefaultParameterValue(""ab"")] object o)
    {
    }
}");
    }

    [Fact]
    public async Task ObjectConstantKeepsVbsTypeAndDecimalDefaultVbsValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Enum E
    A = 1
    B = 2
End Enum

Class AAttribute
    Inherits System.Attribute
    Sub New(o As Object)
    End Sub
End Class

<A(CByte(True))>
Class B
End Class

<A(CType(2.5, E))>
Class C
    Sub M(Optional x As Object = CByte(True), Optional y As Object = CType(2.5, E))
    End Sub
    Sub D(Optional m As Decimal = 0.1F)
    End Sub
End Class", @"using System;
using System.Runtime.InteropServices;

internal enum E
{
    A = 1,
    B = 2
}

internal partial class AAttribute : Attribute
{
    public AAttribute(object o)
    {
    }
}

[A((byte)255)]
internal partial class B
{
}

[A((E)2)]
internal partial class C
{
    public void M([Optional, DefaultParameterValue((byte)255)] object x, [Optional, DefaultParameterValue(2)] object y)
    {
    }
    public void D(decimal m = 0.100000001490116m)
    {
    }
}");
    }
}
