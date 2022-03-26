using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;

using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB { 

    public class LinqExpressionTests : ConverterTestBase
    {
        [Fact]
        public async Task LinqCommasToFromAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Imports System.Linq

Public Class VisualBasicClass
    Sub Main
	    Dim list1 As New List(Of Integer)() From {1,2,3}
	    Dim list2 As New List(Of Integer) From {2, 4,5}
	
	    Dim qs = From n In list1 from x In list2
			     Where x = n 
			     Select New With {x, n}
    End Sub
End Class
",
@"using System.Collections.Generic;
using System.Linq;

public partial class VisualBasicClass
{
    public void Main()
    {
        var list1 = new List<int>() { 1, 2, 3 };
        var list2 = new List<int>() { 2, 4, 5 };
        var qs = from n in list1
                 from x in list2
                 where x == n
                 select new { x, n };
    }
}");
        }
        

       
    }
}
