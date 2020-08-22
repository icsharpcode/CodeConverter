using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
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
            await TestConversionVisualBasicToCSharpAsync(
                @"Private x As Integer = 3",
                @"private int x = 3;");
        }

        [Fact]
        public async Task SingleEmptyClassAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Public Class Test
End Class",
@"
public partial class Test
{
}");
        }

        [Fact]
        public async Task SingleAbstractMethodAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
                @"Protected MustOverride Sub abs()",
                @"protected abstract void abs();");
        }

        [Fact]
        public async Task SingleEmptyNamespaceAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Namespace nam
End Namespace",
@"
namespace nam
{
}");
        }

        [Fact]
        public async Task SingleUnusedUsingAliasTidiedAwayAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports tr = System.IO.TextReader ' Removed by simplifier", "");
        }

        [Fact]
        public async Task QuerySyntaxAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Dim cmccIds As New List(Of Integer)
For Each scr In _sponsorPayment.SponsorClaimRevisions
    For Each claim In scr.Claims
        If TypeOf claim.ClaimSummary Is ClaimSummary Then
            With DirectCast(claim.ClaimSummary, ClaimSummary)
                cmccIds.AddRange(.UnpaidClaimMealCountCalculationsIds)
            End With
        End If
    Next
Next", @"{
    var cmccIds = new List<int>();
    foreach (var scr in _sponsorPayment.SponsorClaimRevisions)
    {
        foreach (var claim in (IEnumerable)scr.Claims)
        {
            if (claim.ClaimSummary is ClaimSummary)
            {
                {
                    var withBlock = (ClaimSummary)claim.ClaimSummary;
                    cmccIds.AddRange(withBlock.UnpaidClaimMealCountCalculationsIds);
                }
            }
        }
    }
}

2 source compilation errors:
BC30451: '_sponsorPayment' is not declared. It may be inaccessible due to its protection level.
BC30002: Type 'ClaimSummary' is not defined.
3 target compilation errors:
CS0103: The name '_sponsorPayment' does not exist in the current context
CS1061: 'object' does not contain a definition for 'ClaimSummary' and no accessible extension method 'ClaimSummary' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'ClaimSummary' could not be found (are you missing a using directive or an assembly reference?)");
        }
    }
}
