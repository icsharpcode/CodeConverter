using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.ExpressionTests;

public class LambdaTests : ConverterTestBase
{
    [Fact]
    public async Task Issue1012_MultiLineLambdaWithSingleStatement()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Public Class ConversionTest3
    Private Class MyEntity
        Property EntityId As Integer
        Property Name As String
    End Class
    Private Sub BugRepro()

        Dim entities As New List(Of MyEntity)

        Parallel.For(1, 3, Sub(i As Integer)
                               Dim result As String = (From e In entities
                                                       Where e.EntityId = 123
                                                       Select e.Name).Single
                           End Sub)
    End Sub
End Class", @"using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class ConversionTest3
{
    private partial class MyEntity
    {
        public int EntityId { get; set; }
        public string Name { get; set; }
    }
    private void BugRepro()
    {

        var entities = new List<MyEntity>();

        Parallel.For(1, 3, (i) =>
        {
            string result = (from e in entities
                             where e.EntityId == 123
                             select e.Name).Single();
        });
    }
}");
    }
}
