using System.Threading.Tasks;
using Xunit;
using ICSharpCode.CodeConverter.Tests.TestRunners;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LinqExtensionMethodReproTests : ConverterTestBase
{
    [Fact]
    public async Task TestLinqExtensionMethods()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Linq

Public Class TestClass
    Public Shared Sub Test()
        Dim intCol = New Integer() {1, 2, 3}
        Dim intCol2 = New Integer() {1, 2, 3}

        Dim intColQuery = intCol.Select(Function(x) x)
        Dim intColCopy = intCol.Select(Function(x) x).ToArray()
        Dim intSum = intCol.Select(Function(x) x).Sum(Function(x) x)
        Dim intMax = intCol.Select(Function(x) x).Max(Function(x) x)
        Dim intCnt = intCol.Select(Function(x) x).Count(Function(x) x > 1)

        Dim intSum2 = intCol.Select(Function(x) x).Sum()
        Dim intMax2 = intCol.OrderBy(Function(x) x).Max()
        Dim intCnt2 = intCol.Select(Function(x) x).Count()
        Dim intSum3 = intCol.Zip(intCol2, Function(x, y) x + y).Sum()
    End Sub
End Class", @"using System;
using System.Linq;

public partial class TestClass
{
    public static void Test()
    {
        int[] intCol = new int[] { 1, 2, 3 };
        int[] intCol2 = new int[] { 1, 2, 3 };

        var intColQuery = intCol.Select(x => x);
        int[] intColCopy = intCol.Select(x => x).ToArray();
        int intSum = intCol.Select(x => x).Sum(x => x);
        int intMax = intCol.Select(x => x).Max(x => x);
        int intCnt = intCol.Select(x => x).Count(x => x > 1);

        int intSum2 = intCol.Select(x => x).Sum();
        int intMax2 = intCol.OrderBy(x => x).Max();
        int intCnt2 = intCol.Select(x => x).Count();
        int intSum3 = intCol.Zip(intCol2, (x, y) => x + y).Sum();
    }
}");
    }
}
