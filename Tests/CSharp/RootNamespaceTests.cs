using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class RootNamespaceTests : ConverterTestBase
{
    public RootNamespaceTests() : base("TheRootNamespace")
    {
    }

    [Fact]
    public async Task RootNamespaceIsExplicitAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceIsExplicitWithSingleClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceIsAddedToExistingNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceIsAddedToExistingNamespaceWithDeclarationCasingAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NestedNamespacesRemainRelativeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NestedNamespaceWithRootClassRemainRelativeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceIsNotAddedToExistingGlobalNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceIsExplicitForSingleNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceNotAppliedToFullyQualifiedNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RootNamespaceOnlyAppliedToUnqualifiedMembersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}