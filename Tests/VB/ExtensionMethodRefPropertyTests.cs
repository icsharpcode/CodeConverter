using System.Threading.Tasks;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB;

public class ExtensionMethodRefPropertyTests : TestRunners.ConverterTestBase
{
    [Fact]
    public async Task TestExtensionMethodRefPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Imports System.Runtime.CompilerServices
Public Class ExtensionMethodsRefPropertyParameter
    Public Property Number As Integer = 3
    Public Sub WithExtensionMethod()
        Number.NegEx()
    End Sub
    Public Sub WithMethod()
        Neg(Number)
    End Sub
End Class

Public Module MathEx
    <Extension()>
    Public Sub NegEx(ByRef num As Integer)
        num = -num
    End Sub
    Public Sub Neg(ByRef num As Integer)
        num = -num
    End Sub
End Module", @"
public partial class ExtensionMethodsRefPropertyParameter
{
    public int Number { get; set; } = 3;
    public void WithExtensionMethod()
    {
        int argnum = Number;
        argnum.NegEx();
        Number = argnum;
    }
    public void WithMethod()
    {
        int argnum = Number;
        MathEx.Neg(ref argnum);
        Number = argnum;
    }
}

public static partial class MathEx
{
    public static void NegEx(this ref int num)

    {
        num = -num;
    }
    public static void Neg(ref int num)
    {
        num = -num;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }
}