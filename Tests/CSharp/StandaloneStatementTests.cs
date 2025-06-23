using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class StandaloneStatementTests : ConverterTestBase
{
    [Fact]
    public async Task ReassignmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim num as Integer = 4
num = 5",
            @"int num = 4;
num = 5;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task CallAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Call mySuperFunction",
            @"mySuperFunction();",
            expectSurroundingBlock: true, missingSemanticInfo: true);
    }

    [Fact]
    public async Task ObjectMemberInitializerSyntaxAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim obj as New AttributeUsageAttribute With
{
    .AllowMultiple = True,
    .Inherited = False
}
obj = Nothing",
            @"var obj = new AttributeUsageAttribute()
{
    AllowMultiple = true,
    Inherited = false
};
obj = null;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task AnonymousObjectCreationExpressionSyntaxAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim obj = New With
{
    .Name = ""Hello"",
    .Value = ""World""
}
obj = Nothing",
            @"var obj = new
{
    Name = ""Hello"",
    Value = ""World""
};
obj = null;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task SingleAssigmentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Dim x = 3",
            @"int x = 3;",
            expectSurroundingBlock: true);
    }

    [Fact]
    public async Task SingleFieldDeclarationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"private int x = 3;", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SingleEmptyClassAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
public partial class Test
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SingleAbstractMethodAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"protected abstract void abs();", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SingleEmptyNamespaceAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"
namespace nam
{
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task SingleUnusedUsingAliasTidiedAwayAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify("", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task QuerySyntaxAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"{
    var cmccIds = new List<int>();
    foreach (var scr in _sponsorPayment.SponsorClaimRevisions)
    {
        foreach (var claim in (IEnumerable)((dynamic)scr).Claims)
        {
            if (((dynamic)claim).ClaimSummary is ClaimSummary)
            {
                {
                    var withBlock = (ClaimSummary)((dynamic)claim).ClaimSummary;
                    cmccIds.AddRange(withBlock.UnpaidClaimMealCountCalculationsIds);
                }
            }
        }
    }
}

2 source compilation errors:
BC30451: '_sponsorPayment' is not declared. It may be inaccessible due to its protection level.
BC30002: Type 'ClaimSummary' is not defined.
2 target compilation errors:
CS0103: The name '_sponsorPayment' does not exist in the current context
CS0246: The type or namespace name 'ClaimSummary' could not be found (are you missing a using directive or an assembly reference?)", extension: "cs")
            );
        }
    }
}