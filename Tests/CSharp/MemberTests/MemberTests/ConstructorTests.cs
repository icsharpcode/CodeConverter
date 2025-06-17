using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests.MemberTests;

public class ConstructorTests : ConverterTestBase
{
    [Fact]
    public async Task TestConstructorVisibilityAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class Class1
    Sub New(x As Boolean)
    End Sub
End Class", @"
internal partial class Class1
{
    public Class1(bool x)
    {
    }
}");
    }

    [Fact]
    public async Task TestModuleConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module Module1
    Sub New()
        Dim someValue As Integer = 0
    End Sub
End Module", @"
internal static partial class Module1
{
    static Module1()
    {
        int someValue = 0;
    }
}");
    }

    [Fact]
    public async Task TestConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass(Of T As {Class, New}, T2 As Structure, T3)
    Public Sub New(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3)
    End Sub
End Class", @"
internal partial class TestClass<T, T2, T3>
    where T : class, new()
    where T2 : struct
{
    public TestClass(out T argument, ref T2 argument2, T3 argument3)
    {
    }
}
1 target compilation errors:
CS0177: The out parameter 'argument' must be assigned to before control leaves the current method");
    }

    [Fact]
    public async Task TestConstructorWithImplicitPublicAccessibilityAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Sub New()
End Sub", @"public SurroundingClass()
{
}");
    }

    [Fact]
    public async Task TestStaticConstructorAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Shared Sub New()
End Sub", @"static SurroundingClass()
{
}");
    }

    [Fact]
    public async Task TestConstructorStaticLocalConvertedToFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class StaticLocalConvertedToField
    Sub New(x As Boolean)
        Static sPrevPosition As Integer = 7 ' Comment moves with declaration
        Console.WriteLine(sPrevPosition)
    End Sub
    Sub New(x As Integer)
        Static sPrevPosition As Integer
        Console.WriteLine(sPrevPosition)
    End Sub
End Class", @"using System;

internal partial class StaticLocalConvertedToField
{
    private int _sPrevPosition = 7; // Comment moves with declaration
    public StaticLocalConvertedToField(bool x)
    {
        Console.WriteLine(_sPrevPosition);
    }

    private int _sPrevPosition1 = default;
    public StaticLocalConvertedToField(int x)
    {
        Console.WriteLine(_sPrevPosition1);
    }
}");
    }
}
