using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class OperatorMemberTests : ConverterTestBase
{
    [Fact]
    public async Task TestNarrowingWideningConversionOperatorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OperatorOverloadsAsync()
    {
        // Note a couple map to the same thing in C# so occasionally the result won't compile. The user can manually decide what to do in such scenarios.
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]// The stack trace displayed will change from time to time. Feel free to update this characterization test appropriately.
    public async Task OperatorOverloadsWithNoCSharpEquivalentShowErrorInlineCharacterizationAsync()
    {
        // No valid conversion to C# - to implement this you'd need to create a new method, and convert all callers to use it.
        var convertedCode = await ConvertAsync<VBToCSConversion>(@"Public Class AcmeClass
    Public Shared Operator ^(i As Integer, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
    Public Shared Operator Like(s As String, ac As AcmeClass) As AcmeClass
        Return ac
    End Operator
End Class");

        Assert.Contains("Cannot convert", convertedCode);
        Assert.Contains("#error", convertedCode);
        Assert.Contains("_failedMemberConversionMarker1", convertedCode);
        Assert.Contains("Public Shared Operator ^(i As Integer,", convertedCode);
        Assert.Contains("_failedMemberConversionMarker2", convertedCode);
        Assert.Contains("Public Shared Operator Like(s As String,", convertedCode);
    }

        [Fact]

    public async Task XorOperatorOverloadConversionAsync()

    {

        await TestConversionVisualBasicToCSharpAsync();

    }
}