using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.MissingSemanticModelInfo
{
    public class StatementTests : ConverterTestBase
    {
        [Fact]
        public async Task MissingLoopTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class MissingLoopType
    Public Sub Test()
        Dim x As Asadf = Nothing

        For i = 1 To x.SomeInteger

        Next
    End Sub
End Class", @"
internal partial class MissingLoopType
{
    public void Test()
    {
        Asadf x = default;
        var loopTo = x.SomeInteger;
        for (int i = 1; i <= loopTo; i++)
        {
        }
    }
}
1 source compilation errors:
BC30002: Type 'Asadf' is not defined.
1 target compilation errors:
CS0246: The type or namespace name 'Asadf' could not be found (are you missing a using directive or an assembly reference?)", missingSemanticInfo: true);
        }

        [Fact]
        public async Task RedimOfUnknownVariableAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"ReDim Preserve UnknownArray(unknownIntIdentifer)", @"{
    Array.Resize(ref UnknownArray, unknownIntIdentifer + 1);
}

2 source compilation errors:
BC30451: 'UnknownArray' is not declared. It may be inaccessible due to its protection level.
BC30451: 'unknownIntIdentifer' is not declared. It may be inaccessible due to its protection level.
2 target compilation errors:
CS0103: The name 'UnknownArray' does not exist in the current context
CS0103: The name 'unknownIntIdentifer' does not exist in the current context", missingSemanticInfo: true);
        }
    }
}
