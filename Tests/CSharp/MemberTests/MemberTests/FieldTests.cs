using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MemberTests.MemberTests;

public class FieldTests : ConverterTestBase
{
    [Fact]
    public async Task TestFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Const answer As Integer = 42
    Private value As Integer = 10
    ReadOnly v As Integer = 15
End Class", @"
internal partial class TestClass
{
    private const int answer = 42;
    private int value = 10;
    private readonly int v = 15;
}");
    }

    [Fact]
    public async Task TestMultiArrayFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Dim Parts(), Taxes(), Deposits()(), Prepaid()(), FromDate, ToDate As String
End Class", @"
internal partial class TestClass
{
    private string[] Parts, Taxes;
    private string[][] Deposits, Prepaid;
    private string FromDate, ToDate;
}");
    }

    [Fact]
    public async Task TestConstantFieldInModuleAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Module TestModule
    Const answer As Integer = 42
End Module", @"
internal static partial class TestModule
{
    private const int answer = 42;
}");
    }

    [Fact]
    public async Task TestTypeInferredConstAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Const someConstField = 42
    Sub TestMethod()
        Const someConst = System.DateTimeKind.Local
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private const int someConstField = 42;
    public void TestMethod()
    {
        const DateTimeKind someConst = DateTimeKind.Local;
    }
}");
    }

    [Fact]
    public async Task TestTypeInferredVarAsync()
    {
        // VB doesn't infer the type of EnumVariable like you'd think, it just uses object
        // VB compiler uses Conversions rather than any plainer casting
        await TestConversionVisualBasicToCSharpAsync(
            @"Class TestClass
    Public Enum TestEnum As Integer
        Test1
    End Enum

    Dim EnumVariable = TestEnum.Test1
    Public Sub AMethod()
        Dim t1 As Integer = EnumVariable
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public enum TestEnum : int
    {
        Test1
    }

    private object EnumVariable = TestEnum.Test1;
    public void AMethod()
    {
        int t1 = Conversions.ToInteger(EnumVariable);
    }
}");
    }

    [Fact]
    public async Task FieldWithAttributeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    <ThreadStatic>
    Private Shared First As Integer
End Class", @"using System;

internal partial class TestClass
{
    [ThreadStatic]
    private static int First;
}");
    }

    [Fact]
    public async Task FieldWithNonStaticInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class A
    Private x As Integer = 2
    Private y(x) As Integer
End Class", @"
public partial class A
{
    private int x = 2;
    private int[] y;

    public A()
    {
        y = new int[x + 1];
    }
}");
    }

    [Fact]
    public async Task FieldWithInstanceOperationOfDifferingTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class DoesNotNeedConstructor
    Private ReadOnly ClassVariable1 As New ParallelOptions With {.MaxDegreeOfParallelism = 5}
End Class", @"using System.Threading.Tasks;

public partial class DoesNotNeedConstructor
{
    private readonly ParallelOptions ClassVariable1 = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
}");
    }

    [Fact]
    public async Task Issue281FieldWithNonStaticLambdaInitializerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.IO

Public Class Issue281
    Private lambda As System.Delegate = New ErrorEventHandler(Sub(a, b) Len(0))
    Private nonShared As System.Delegate = New ErrorEventHandler(AddressOf OnError)

    Sub OnError(s As Object, e As ErrorEventArgs)
    End Sub
End Class", @"using System;
using System.IO;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

public partial class Issue281
{
    private Delegate lambda = new ErrorEventHandler((a, b) => Strings.Len(0));
    private Delegate nonShared;

    public Issue281()
    {
        nonShared = new ErrorEventHandler(OnError);
    }

    public void OnError(object s, ErrorEventArgs e)
    {
    }
}");
    }
}
