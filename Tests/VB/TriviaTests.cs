using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class TriviaTests : ConverterTestBase
    {
        [Fact]
        public async Task MethodWithComments()
        {
            await TestConversionCSharpToVisualBasic(
                @"using System;
using System.Diagnostics; //Using statement

//blank line

namespace ANamespace //namespace
{ //BUG: Block start loses comments
    /// <summary>
    /// class xml doc
    /// </summary>
    class CommentTestClass //Don't rename
    { //Keep this method at the top
        /// <summary>
        /// method xml doc
        /// </summary>
        public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class where T2 : struct //Only for structs
        { //BUG: Block start loses comments
    #if true //BUG: IfDirective loses comments
            argument = null; //1
    #region Arg2
            argument2 = default(T2); //2
    #endregion //BUG: EndRegion loses comments
            if (argument != null) //never
            { //BUG: Block start loses comments
             //This works because it's leading trivia for the next line
                Debug.WriteLine(1); // Check debug window
                Debug.WriteLine(2);
            } //argument1 != null
            argument3 = default(T3); //3
    #else //BUG: ElseDirective loses comments
            argument = new object();
    #endif //BUG: EndIfDirective loses comments
            Console.Write(3);
        } //End of method
    } //End of class
}
// Last line comment", @"Imports System
Imports System.Diagnostics ' Using statement
Imports System.Runtime.InteropServices


' blank line

Namespace ANamespace ' namespace
    ''' <summary>
    ''' class xml doc
    ''' </summary>
    Friend Class CommentTestClass ' Don't rename
        ''' <summary>
        ''' method xml doc
        ''' </summary>
        Public Sub TestMethod(Of T As Class, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) ' Only for structs
#If true
            argument = Nothing ' 1
#Region ""Arg2""
            argument2 = Nothing ' 2
#End Region
            If argument IsNot Nothing Then ' never
                ' This works because it's leading trivia for the next line
                Debug.WriteLine(1) ' Check debug window
                Debug.WriteLine(2)
            End If ' argument1 != null
            argument3 = Nothing ' 3
#Else
' Skipped during conversion:             argument = new object();
#End If
            Console.Write(3)
        End Sub ' End of method
    End Class ' End of class
End Namespace' Last line comment");
        }

        [Fact]
        public async Task TrailingAndEndOfFileLineComments()
        {
            await TestConversionCSharpToVisualBasic(
                @"//leading
namespace ANamespace //namespace
{ //BUG: Block start loses comments
} //end namespace
// Last line comment", @"' leading
Namespace ANamespace ' namespace
End Namespace ' end namespace
' Last line comment");
        }
    }
}
