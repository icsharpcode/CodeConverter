using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class ParameterTests : ConverterTestBase
{
    [Fact]
    public async Task OptionalParameter_DoesNotThrowInvalidCastExceptionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
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
", extension: "vb"),
                Verifier.Verify(@"using System;

public partial class MyTestAttribute : Attribute
{
}

public partial class MyController
{
    public string GetNothing([MyTest()] int? indexer = 0)
    {
        return null;
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OptionalLastParameter_ExpandsOptionalOmittedArgAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
Public Class TestClass
    Public Sub M(a As String)
    End Sub
    Public Sub M(a As String, Optional b as String = ""smth"")
    End Sub
    
    Public Sub Test()
        M(""x"",)
    End Sub
End Class
", extension: "vb"),
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OptionalFirstParameter_ExpandsOptionalOmittedArgAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
Public Class TestClass
    Public Sub M(a As String)
    End Sub
    Public Sub M(Optional a As String = ""ss"", Optional b as String = ""smth"")
    End Sub
    
    Public Sub Test()
        M(,""x"")
    End Sub
End Class
", extension: "vb"),
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task OmittedArgumentAfterNamedArgument_WhenMethodHasCollidingOverload_ShouldExpandAllOptionalArgsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
Public Class TestClass
    Public Sub M(a As String, b as string)
    End Sub
    Public Sub M(Optional a As String = ""1"", Optional b as string = ""2"", Optional c as string = ""3"")
    End Sub

    Public Sub Test()
        M(a:=""4"", )
    End Sub
End Class
", extension: "vb"),
                Verifier.Verify(@"
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
}", extension: "cs")
            );
        }
    }
}