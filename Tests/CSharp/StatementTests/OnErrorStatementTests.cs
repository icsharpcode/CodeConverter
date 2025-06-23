using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class OnErrorStatementTests : ConverterTestBase
{
    [Fact]
    public async Task BasicGotoAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;
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
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task RemainingScopeIssueCharacterizationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"using System;

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
CS0103: The name 'i' does not exist in the current context", extension: "cs")
            );
        }
    }
}