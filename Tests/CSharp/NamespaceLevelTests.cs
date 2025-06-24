using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

public class NamespaceLevelTests : ConverterTestBase
{
    [Fact]
    public async Task TestNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestLongNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestGlobalNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestGenericInheritanceInGlobalNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestTopLevelAttributeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task AliasedImportsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task UnaliasedImportsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMixedCaseNamespaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestInternalStaticClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestAbstractClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestSealedClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestInterfaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestClassInheritanceList1Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestClassInheritanceList2Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestStructAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    [Fact]
    public async Task TestDelegateAsync1()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Delegate Sub Test()",
            @"public delegate void Test();", false);
    }
    [Fact]
    public async Task TestDelegateAsync2()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Delegate Function Test() As Integer",
            @"public delegate int Test();", false);
    }
    [Fact]
    public async Task TestDelegateAsync3()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Delegate Sub Test(ByVal x As Integer)",
            @"public delegate void Test(int x);", false);
    }
    [Fact]
    public async Task TestDelegateAsync4()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Delegate Sub Test(ByRef x As Integer)",
            @"public delegate void Test(ref int x);", false);
    }

    [Fact]
    public async Task TestGenericDelegate771Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestDelegateWithOmittedParameterTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ClassImplementsInterfaceAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ClassImplementsInterface2Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ClassInheritsClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ClassInheritsClass2Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ClassInheritsClassWithNoParenthesesOnBaseCallAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultilineDocCommentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MultilineCommentRootOfFileAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact (Skip ="This test currently fails.  The initial line is trimmed. Not sure of importance")]
    public async Task MultilineCommentRootOfFileLeadingSpacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
    [Fact]
    public async Task EnumConversionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NewTypeConstraintLastAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MyClassVirtualCallMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MyClassVirtualCallPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OverridenMemberCallAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue1019_ImportsClassUsingStaticAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}