using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class TriviaTests : ConverterTestBase
    {

        [Fact]
        public async Task TestMethodWithComments()
        {
            await TestConversionCSharpToVisualBasic(
                @"using System.Diagnostics; //using statement
// blank line

/// <summary>
/// class xml doc
/// </summary>
class CommentTestClass //Don't rename
{ //Keep this method at the top
    /// <summary>
    /// method xml doc
    /// </summary>
    public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class where T2 : struct //Only for structs
    { //Start of method
#if true
        argument = null; //1
#region Arg2
        argument2 = default(T2); //2
#endregion
        if (argument != null) //never
        {//Just for debug

            Debug.WriteLine(1); // Check debug window
        } //argument1 != null
        argument3 = default(T3); //3
#else
        argument = new object();
#endif
        Console.Write();
    } //End of method
} //End of class", @"Imports System.Diagnostics
Imports System.Runtime.InteropServices
    
Friend Class CommentTestClass
    ''' <summary>
    ''' /// method xml doc
    ''' /// </summary>    Public Sub TestMethod(Of T As Class, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) ' Only for structs
        ' TODO ERROR: Skipped #if true
        argument = Nothing ' 1
        argument2 = Nothing ' 2
        If argument IsNot Nothing Then ' never
            Debug.WriteLine(1) ' Check debug window
        End If
        argument3 = Nothing ' 3
    End Sub
End Class");
        }
    }
}
