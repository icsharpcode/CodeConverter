using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class InterfaceTests : ConverterTestBase
{

    [Fact]
    public async Task Issue443_FixCaseForInterfaceMembersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue444_FixNameForRenamedInterfaceMembersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task IdenticalInterfaceMethodsWithRenamedInterfaceMembersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfaceCasingOnlyDifferenceConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfaceCasingOnlyDifferenceForVirtualMemberConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfaceCasingOnlyDifferenceWithOverloadedPropertyConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedMethodImplementsMultipleInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task IdenticalInterfacePropertiesWithRenamedInterfaceMembersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationRequiredMethodParameters_749_Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalParameters_1062_Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task OptionalParameterWithReservedName_1092_Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalParametersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task ExplicitInterfaceImplementationOptionalMethodParameters_749_Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfaceMethodFullyQualifiedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfacePropertyFullyQualifiedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfaceMethodConsumerCasingRenamedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfacePropertyConsumerCasingRenamedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InterfaceMethodCasingRenamedConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InterfacePropertyCasingRenamedConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InterfaceRenamedMethodConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task InterfaceRenamedPropertyConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PartialInterfaceRenamedMethodConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PartialInterfaceRenamedPropertyConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfaceMethodMyClassConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task RenamedInterfacePropertyMyClassConsumerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PropertyInterfaceImplementationKeepsVirtualModifierAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PrivateAutoPropertyImplementsMultipleInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task ImplementMultipleRenamedPropertiesFromInterfaceAsAbstractAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationForVirtualMemberFromAnotherClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationWhereOnlyOneInterfaceMemberIsRenamedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitInterfaceImplementationWhereMemberShadowsBaseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PrivatePropertyAccessorBlocksImplementsMultipleInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NonPublicImplementsInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ExplicitPropertyImplementationWithDirectAccessAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ReadonlyRenamedPropertyImplementsMultipleInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task WriteonlyPropertyImplementsMultipleInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PrivateMethodAndParameterizedPropertyImplementsMultipleInterfacesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Issue444_InternalMemberDelegatingMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }



    [Fact]
    public async Task TestReadOnlyOrWriteOnlyPropertyImplementedByNormalPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestReadOnlyAndWriteOnlyParametrizedPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExplicitInterfaceOfParametrizedPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}