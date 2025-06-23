using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests;

public class MemberTests : ConverterTestBase
{

    [Fact]
    public async Task TestFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMultiArrayFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestConstantFieldInModuleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestDeclareMethodVisibilityInModuleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestDeclareMethodVisibilityInClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestTypeInferredConstAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestTypeInferredVarAsync()
    {
        // VB doesn't infer the type of EnumVariable like you'd think, it just uses object
        // VB compiler uses Conversions rather than any plainer casting
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodAssignmentReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodAssignmentReturn293Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodAssignmentAdditionReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodMissingReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodWithOutParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMethodWithReturnTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestFunctionWithNoReturnTypeSpecifiedAsync()
    {
        // Note: "Inferred" type is always object except with local variables
        // https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/variables/local-type-inference
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestFunctionReturningTypeRequiringConversionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestStaticMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestAbstractMethodAndPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestAbstractReadOnlyAndWriteOnlyPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task SetterProperty1053Async()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task StaticLocalsInPropertyGetterAndSetterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestSealedMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestShadowedMethodAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }


    [Fact]
    public async Task TestDestructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue681_OverloadsOverridesPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PartialFriendClassWithOverloadsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ClassWithGloballyQualifiedAttributeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task FieldWithAttributeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task FieldWithNonStaticInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task FieldWithInstanceOperationOfDifferingTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue281FieldWithNonStaticLambdaInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ParamArrayAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task ParamNamedBoolAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task MethodWithNameArrayParameterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task UntypedParametersAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task PartialClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task NestedClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task LessQualifiedNestedClassAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestAsyncMethodsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestAsyncMethodsWithNoReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestExternDllImportAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue420_RenameClashingClassMemberAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue420_DoNotRenameClashingEnumMemberAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task Issue420_DoNotRenameClashingEnumMemberForPublicEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestPropertyStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestStaticLocalWithoutDefaultInitializerConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestModuleStaticLocalConvertedToStaticFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestStaticLocalConvertedToStaticFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestOmittedArgumentsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestRefConstArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestRefFunctionCallNoParenthesesArgumentAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }

    [Fact]
    public async Task TestMissingByRefArgumentWithNoExplicitDefaultValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync();
    }
}