using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class OnErrorStatementTests : ConverterTestBase
{
    [Fact]
    public async Task BasicGotoAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
Public Function SelfDivisionPossible(x as Integer) As Boolean
    On Error GoTo ErrorHandler
        Dim i as Integer = x / x
        Return True
ErrorHandler:
    Return Err.Number = 6
End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public bool SelfDivisionPossible(int x)
    {
        try
        {
            int i = (int)Math.Round(x / (double)x);
            return true;
        }
        catch
        {

            return Information.Err().Number == 6;
        }
    }
}");
    }

    [Fact]
    public async Task RemainingScopeIssueCharacterizationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
Public Function SelfDivisionPossible(x as Integer) As Boolean
    On Error GoTo ErrorHandler
        Dim i as Integer = x / x
ErrorHandler:
    Return i <> 0
End Function
End Class", @"using System;

internal partial class TestClass
{
    public bool SelfDivisionPossible(int x)
    {
        try
        {
            int i = (int)Math.Round(x / (double)x);
        }
        catch
        {
        }

        return i != 0;
    }
}
1 target compilation errors:
CS0103: The name 'i' does not exist in the current context");
    }
}