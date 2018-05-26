using System;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class SpecialConversionTests : ConverterTestBase
    {
        [Fact]
        public void TestSimpleInlineAssign()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        int a, b;
        b = a = 5;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim a, b As Integer
        b = CSharpImpl.__Assign(a, 5)
    End Sub

    Private Class CSharpImpl
        <Obsolete(""Please refactor calling code to use normal Visual Basic assignment"")>
        Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function
    End Class
End Class");
        }

        [Fact]
        public void TestSimplePostIncrementAssign()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        int a = 5, b;
        b = a++;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer, a As Integer = 5
        b = Math.Min(System.Threading.Interlocked.Increment(a), a - 1)
    End Sub
End Class");
        }

        [Fact]
        public void RaiseEvent()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    event EventHandler MyEvent;

    void TestMethod()
    {
        if (MyEvent != null) MyEvent(this, EventArgs.Empty);
    }
}", @"Friend Class TestClass
    Private Event MyEvent As EventHandler

    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if ((MyEvent != null)) MyEvent(this, EventArgs.Empty);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (null != MyEvent) { MyEvent(this, EventArgs.Empty); }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (this.MyEvent != null) MyEvent(this, EventArgs.Empty);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (MyEvent != null) this.MyEvent(this, EventArgs.Empty);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if ((this.MyEvent != null)) { this.MyEvent(this, EventArgs.Empty); }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        RaiseEvent MyEvent(Me, EventArgs.Empty)
    End Sub
End Class");
        }

        [Fact]
        public void IfStatementSimilarToRaiseEvent()
        {
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) DrawImage();
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then DrawImage()
    End Sub
End Class");
            // regression test:
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) e.DrawImage();
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then e.DrawImage()
    End Sub
End Class");
            // with braces:
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) { DrawImage(); }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then
            DrawImage()
        End If
    End Sub
End Class");
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (FullImage != null) { e.DrawImage(); }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If FullImage IsNot Nothing Then
            e.DrawImage()
        End If
    End Sub
End Class");
            // another bug related to the IfStatement code:
            TestConversionCSharpToVisualBasic(
                @"class TestClass
{
    void TestMethod()
    {
        if (Tiles != null) foreach (Tile t in Tiles) this.TileTray.Controls.Remove(t);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If Tiles IsNot Nothing Then

            For Each t As Tile In Tiles
                Me.TileTray.Controls.Remove(t)
            Next
        End If
    End Sub
End Class");
        }

        /// <summary>
        /// VB's overload resolution is much poorer than C#'s in relation to Func/Action (in C# it was improved to support Linq method chaining, but VB has a more extensive query syntax instead)
        /// If there are any overloads (including the extension method version of a method vs its non extension method version), VB needs an exact match (with no narrowing conversions).
        /// This means Funcs/Actions need to be wrapped in a typed constructor such as New Action(Of String)
        /// </summary>
        [Fact]
        public void AddressOfWhereVbTypeInferenceIsWeaker()
        {
            TestConversionCSharpToVisualBasic(@"static class TestClass
{
    private static object TypeSwitch(this object obj, Func<string, object> matchFunc1, Func<int, object> matchFunc2, Func<object, object> defaultFunc)
    {
        return null;
    }

    private static object ConvertInt(int node)
    {
        return node;
    }

    private static object ConvertString(string node)
    {
        return node;
    }

    public static object Convert(object node)
    {
        return node.TypeSwitch(ConvertString, ConvertInt, _ => throw new NotImplementedException($""Conversion for '{node.GetType()}' not implemented""));
    }
}", @"Imports System.Runtime.CompilerServices

Friend Module TestClass
    <Extension()>
    Private Function TypeSwitch(ByVal obj As Object, ByVal matchFunc1 As Func(Of String, Object), ByVal matchFunc2 As Func(Of Integer, Object), ByVal defaultFunc As Func(Of Object, Object)) As Object
        Return Nothing
    End Function

    Private Function ConvertInt(ByVal node As Integer) As Object
        Return node
    End Function

    Private Function ConvertString(ByVal node As String) As Object
        Return node
    End Function

    Function Convert(ByVal node As Object) As Object
        Return node.TypeSwitch(New Func(Of String, Object)(AddressOf ConvertString), New Func(Of Integer, Object)(AddressOf ConvertInt), Function(__)
                                                                                                                                             Throw New NotImplementedException($""Conversion for '{node.[GetType]()}' not implemented"")
                                                                                                                                         End Function)
    End Function
End Module");
        }

        [Fact]
        public void HexAndBinaryLiterals()
        {
            TestConversionCSharpToVisualBasic(
                @"class Test
{
    public int CR = 0x0D * 0b1;
}", @"Friend Class Test
    Public CR As Integer = &H0D * &B1
End Class");
        }
    }
}
