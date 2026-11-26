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
}
