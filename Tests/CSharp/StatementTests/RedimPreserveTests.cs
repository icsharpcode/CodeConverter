using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class RedimPreserveTests : ConverterTestBase
{
    [Fact]
    public async Task RedimPreserveOnPropertyAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(
            @"Public Class TestClass
    Public Property NumArray1 As Integer()

    Public Sub New()
        ReDim Preserve NumArray1(4)
    End Sub
End Class", @"using System;

public partial class TestClass
{
    public int[] NumArray1 { get; set; }

    public TestClass()
    {
        var argNumArray1 = NumArray1;
        Array.Resize(ref argNumArray1, 5);
        NumArray1 = argNumArray1;
    }
}");
    }
}
