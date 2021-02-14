using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    public class TriviaTests : ConverterTestBase
    {
        [Fact]
        public async Task Issue506_IfStatementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"Imports System

Public Class TestClass506
    Public Sub Deposit(Item As Integer, ColaOnly As Boolean, MonteCarloLogActive As Boolean, InDevEnv As Func(Of Boolean))

        If ColaOnly Then 'just log the Cola value
            Console.WriteLine(1)
        ElseIf (Item = 8 Or Item = 9) Then 'this is an indexing rate for inflation adjustment
            Console.WriteLine(2)
        Else 'this for a Roi rate from an assets parameters
            Console.WriteLine(3)
        End If
        If MonteCarloLogActive AndAlso InDevEnv() Then 'Special logging for dev debugging
            Console.WriteLine(4)
            'WriteErrorLog() 'write a blank line
        End If
    End Sub
End Class", @"using System;

public partial class TestClass506
{
    public void Deposit(int Item, bool ColaOnly, bool MonteCarloLogActive, Func<bool> InDevEnv)
    {
        if (ColaOnly) // just log the Cola value
        {
            Console.WriteLine(1);
        }
        else if (Item == 8 | Item == 9) // this is an indexing rate for inflation adjustment
        {
            Console.WriteLine(2);
        }
        else // this for a Roi rate from an assets parameters
        {
            Console.WriteLine(3);
        }

        if (MonteCarloLogActive && InDevEnv()) // Special logging for dev debugging
        {
            Console.WriteLine(4);
            // WriteErrorLog() 'write a blank line
        }
    }
}");
        }

        [Fact]
        public async Task Issue15_NestedRegionsAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(
@"#Region ""Whole File""
#Region ""Nested""
Imports System

#Region ""Class""
Module Program
#Region ""Inside Class""
    Sub Main(args As String())
#Region ""Inside Method""
        Console.WriteLine(""Hello World!"")
#End Region
    End Sub
#End Region
End Module
#End Region
#End Region
#End Region
",
@"#region Whole File
#region Nested
using System;

#region Class
internal static partial class Program
{
    #region Inside Class
    public static void Main(string[] args)
    {
        #region Inside Method
        Console.WriteLine(""Hello World!"");
        #endregion
    }
    #endregion
}
#endregion
#endregion
#endregion",
hasLineCommentConversionIssue: true);//Auto-test code doesn't know to avoid adding comment on same line as region
        }
    }
}
