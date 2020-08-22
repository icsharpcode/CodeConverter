using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB
{
    public class TriviaTests : ConverterTestBase
    {
        [Fact]
        public async Task MethodWithCommentsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"using System;
using System.Diagnostics; //Using statement

//blank line

namespace ANamespace //namespace
{ // Block start - namespace
    /// <summary>
    /// class xml doc
    /// </summary>
    class CommentTestClass //Don't rename
    { //Keep this method at the top
        /// <summary>
        /// method xml doc
        /// </summary>
        public void TestMethod<T, T2, T3>(out T argument, ref T2 argument2, T3 argument3) where T : class where T2 : struct //Only for structs
        { // Block start - method
    #if true //IfDirective keeps comments
            argument = null; //1
    #region Arg2
            argument2 = default(T2); //2
    #endregion // EndRegion loses comments
            if (argument != null) //never
            { // Block start - if
             // leading trivia for the next line
                Debug.WriteLine(1); // Check debug window
                Debug.WriteLine(2);
            } //argument1 != null
            argument3 = default(T3); //3
    #else //ElseDirective keeps comments
            argument = new object();
    #endif //EndIfDirective keeps comments
            Console.Write(3);
        } //End of method
    } //End of class
}
// Last line comment", @"Imports System
Imports System.Diagnostics 'Using statement
Imports System.Runtime.InteropServices


'blank line

Namespace ANamespace 'namespace
    ' Block start - namespace
    ''' <summary>
    ''' class xml doc
    ''' </summary>
    Friend Class CommentTestClass 'Don't rename
        'Keep this method at the top
        ''' <summary>
        ''' method xml doc
        ''' </summary>
        Public Sub TestMethod(Of T As Class, T2 As Structure, T3)(<Out> ByRef argument As T, ByRef argument2 As T2, ByVal argument3 As T3) 'Only for structs
            ' Block start - method
#If True 'IfDirective keeps comments
            argument = Nothing '1
#Region ""Arg2""
            argument2 = Nothing '2
#End Region
            If argument IsNot Nothing Then 'never
                ' Block start - if
                ' leading trivia for the next line
                Debug.WriteLine(1) ' Check debug window
                Debug.WriteLine(2)
            End If 'argument1 != null
            argument3 = Nothing '3
#Else 'ElseDirective keeps comments
            argument = new object();
#End If 'EndIfDirective keeps comments
            Console.Write(3)
        End Sub 'End of method
    End Class 'End of class
End Namespace' Last line comment");
        }

        [Fact]
        public async Task TrailingAndEndOfFileLineCommentsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
                @"//leading
namespace ANamespace //namespace
{ // start of block - namespace
} //end namespace
// Last line comment", @"'leading
Namespace ANamespace 'namespace
    ' start of block - namespace
End Namespace 'end namespace
' Last line comment");
        }
    }
}
