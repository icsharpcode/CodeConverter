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
    Return False
End Function
End Class", @"using System;

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
        }

        return false;
    }
}");
    }
}