using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class OnErrorStatementTests : ConverterTestBase
{
    [Fact]
    public async Task BasicGotoAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
Public Function Save() As Boolean
    On Error GoTo ErrorHandler

        Dim zero as Integer = 0
        Dim i as Integer = zero / zero

    Exit Function

ErrorHandler: 
    Console.Write(0)
End Function
End Class", @"using System;
          
internal partial class TestClass
{
    public bool Save()
    {
        var catchIndex = -1;
        do
            try
            {
                switch (catchIndex)
                {
                    case 0:
                        catchIndex = -1;
                        goto ErrorHandler;
                        break;
                }
                catchIndex = 0;

                int zero = 0;
                int i = (int)Math.Round(zero / (double)zero);

                return default;

            ErrorHandler:
                ;

                Console.Write(0);
                catchIndex = -1;
            }
            catch
            {
            }
        while (catchIndex != -1);
    }
}
1 target compilation errors:
CS0161: 'TestClass.Save()': not all code paths return a value");
    }
}