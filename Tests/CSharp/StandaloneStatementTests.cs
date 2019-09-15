using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class StandaloneStatementTests : ConverterTestBase
    {
        [Fact]
        public async Task Reassignment()
        {
            await TestConversionVisualBasicToCSharp(
@"Dim num as Integer = 4
num = 5",
@"int num = 4;
num = 5;",
expectSurroundingBlock: true);
        }

        [Fact]
        public async Task ObjectMemberInitializerSyntax()
        {
            await TestConversionVisualBasicToCSharp(
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
        public async Task AnonymousObjectCreationExpressionSyntax()
        {
            await TestConversionVisualBasicToCSharp(
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
        public async Task SingleAssigment()
        {
            await TestConversionVisualBasicToCSharp(
                @"Dim x = 3",
                @"int x = 3;",
                expectSurroundingBlock: true);
        }

        [Fact]
        public async Task SingleFieldDeclaration()
        {
            await TestConversionVisualBasicToCSharp(
                @"Private x As Integer = 3",
                @"private int x = 3;");
        }

        [Fact]
        public async Task SingleEmptyClass()
        {
            await TestConversionVisualBasicToCSharp(
@"Public Class Test
End Class",
@"public partial class Test
{
}");
        }

        [Fact]
        public async Task SingleAbstractMethod()
        {
            await TestConversionVisualBasicToCSharp(
                @"Protected MustOverride Sub abs()",
                @"protected abstract void abs();");
        }

        [Fact]
        public async Task SingleEmptyNamespace()
        {
            await TestConversionVisualBasicToCSharp(
@"Namespace nam
End Namespace",
@"namespace nam
{
}");
        }

        [Fact]
        public async Task SingleUnusedUsingAliasTidiedAway()
        {
            await TestConversionVisualBasicToCSharp(@"Imports tr = System.IO.TextReader", "");
        }

        [Fact]
        public async Task QuerySyntax()
        {
            await TestConversionVisualBasicToCSharp(@"Dim cmccIds As New List(Of Integer)
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
        foreach (var claim in scr.Claims)
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
}");
        }
    }
}
