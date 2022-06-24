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
    public async Task OptionalLastParameter_UsesCorrectMethodOverloadAsync()
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
    public async Task OptionalFirstParameter_UsesCorrectMethodOverloadAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"
Public Class TestClass
    Public Sub M(b As String)
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
    public void M(string b)
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
}