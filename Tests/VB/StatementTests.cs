using System.Threading.Tasks;
using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.VB;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeConverter.Tests.VB
{
    public class StatementTests : ConverterTestBase
    {
        [Fact]
        public async Task EmptyStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        if (true) ;
        while (true) ;
        for (;;) ;
        do ; while (true);
        ;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        If True Then
        End If

        While True
        End While

        While True
        End While

        Do
        Loop While True
    End Sub
End Class");
        }

        [Fact]
        public async Task AssignmentStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int b;
        b = 0;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task AssignmentStatementInDeclaration()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int b = 0;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task AssignmentStatementInVarDeclaration()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        var b = 0;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task ObjectInitializationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        string b;
        b = new string(""test"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As String
        b = New String(""test"")
    End Sub
End Class");
        }

        [Fact]
        public async Task ObjectInitializationStatementInDeclaration()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        string b = new string(""test"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New String(""test"")
    End Sub
End Class");
        }

        [Fact]
        public async Task ObjectInitializationStatementInVarDeclaration()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        var b = new string(""test"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New String(""test"")
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayDeclarationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[] b;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayInitializationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[] b = { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayInitializationStatementInVarDeclaration()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        var b = { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithType()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[] b = new int[] { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer() {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithLength()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[] b = new int[3] { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer(2) {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayDeclarationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[,] b;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,)
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[,] b = {
            {1, 2},
            {3, 4}
        };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = {
        {1, 2},
        {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatementWithType()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[,] b = new int[,] {
            {1, 2},
            {3, 4}
        };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer(,) {
        {1, 2},
        {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatementWithLengths()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[,] b = new int[2, 2] {
            {1, 2},
            {3, 4}
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer(1, 1) {
        {1, 2},
        {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task JaggedArrayDeclarationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[][] b;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()()
    End Sub
End Class");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[][] b = { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatementWithType()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[][] b = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer()() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatementWithLength()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int[][] b = new int[2][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer(1)() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task DeclarationStatements()
        {
            await TestConversionCSharpToVisualBasic(
                @"class Test {
    void TestMethod()
    {
the_beginning:
        int value = 1;
        const double myPIe = System.Math.PI;
        var text = ""This is my text!"";
        goto the_beginning;
    }
}", @"Friend Class Test
    Private Sub TestMethod()
the_beginning:
        Dim value = 1
        Const myPIe = Math.PI
        Dim text = ""This is my text!""
        GoTo the_beginning
    End Sub
End Class");
        }

        [Fact]
        public async Task IfStatementWithoutBlock()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod (int a)
    {
        int b;
        if (a == 0)
            b = 0;
        else
            b = 3;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal a As Integer)
        Dim b As Integer

        If a = 0 Then
            b = 0
        Else
            b = 3
        End If
    End Sub
End Class");
        }

        [Fact]
        public async Task IfStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod (int a)
    {
        int b;
        if (a == 0) {
            b = 0;
        } else if (a == 1) {
            b = 1;
        } else if (a == 2 || a == 3) {
            b = 2;
        } else {
            b = 3;
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal a As Integer)
        Dim b As Integer

        If a = 0 Then
            b = 0
        ElseIf a = 1 Then
            b = 1
        ElseIf a = 2 OrElse a = 3 Then
            b = 2
        Else
            b = 3
        End If
    End Sub
End Class");
        }

        [Fact]
        public async Task BlockStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    public static void TestMethod()
    {
        {
            var x = 1;
            Console.WriteLine(x);
        }

        {
            var x = 2;
            Console.WriteLine(x);
        }
    }
}", @"Friend Class TestClass
    Public Shared Sub TestMethod()
        If True Then
            Dim x = 1
            Console.WriteLine(x)
        End If

        If True Then
            Dim x = 2
            Console.WriteLine(x)
        End If
    End Sub
End Class");
        }

        [Fact]
        public async Task WhileStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int b;
        b = 0;
        while (b == 0)
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        While b = 0
            If b = 2 Then Continue While
            If b = 3 Then Exit While
            b = 1
        End While
    End Sub
End Class");
        }

        [Fact]
        public async Task UnsafeStatementsWithNoVbEquivalentShowErrorInlineCharacterization()
        {
            string convertedCode = await GetConvertedCodeOrErrorString<CSToVBConversion>(@"class TestClass
{
    void TestMethod()
    {
        int b;
        b = 0;
        while (b == 0)
        {
            if (b == 2)
            {
                unsafe
                {
                    int ab = 32;
                    int* p = &ab;
                    Console.WriteLine(""value of ab is {0}"", *p);
                }
            }
            if (b == 3)
                break;
            b = 1;
        }
    }
}");

            Assert.Contains("CONVERSION ERROR", convertedCode);
            Assert.Contains("unsafe", convertedCode);
            Assert.Contains("UnsafeStatementSyntax", convertedCode);
            Assert.Contains("If b = 2 Then", convertedCode);
            Assert.Contains("End If", convertedCode);
        }

        [Fact]
        public async Task DoWhileStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        int b;
        b = 0;
        do
        {
            if (b == 2)
                continue;
            if (b == 3)
                break;
            b = 1;
        }
        while (b == 0);
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task ForEachStatementWithExplicitType()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(int[] values)
    {
        foreach (int val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class");
        }

        [Fact]
        public async Task ForEachStatementWithVar()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(int[] values)
    {
        foreach (var val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class");
        }

        [Fact]
        public async Task SyncLockStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(object nullObject)
    {
        if (nullObject == null)
            throw new ArgumentNullException(nameof(nullObject));
        lock (nullObject) {
            Console.WriteLine(nullObject);
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))

        SyncLock nullObject
            Console.WriteLine(nullObject)
        End SyncLock
    End Sub
End Class");
        }

        [Fact]
        public async Task ForWithUnknownConditionAndSingleStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        for (i = 0; unknownCondition; i++)
            b[i] = s[i];
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        i = 0

        While unknownCondition
            b(i) = s(i)
            i += 1
        End While
    End Sub
End Class");
        }

        [Fact]
        public async Task ForWithUnknownConditionAndBlock()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        for (int i = 0; unknownCondition; i++) {
            b[i] = s[i];
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim i = 0

        While unknownCondition
            b(i) = s(i)
            i += 1
        End While
    End Sub
End Class");
        }

        [Fact]
        public async Task ForWithSingleStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        for (i = 0; i < end; i++) b[i] = s[i];
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        For i = 0 To [end] - 1
            b(i) = s(i)
        Next
    End Sub
End Class");
        }

        [Fact]
        public async Task ForWithBlock()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod()
    {
        for (i = 0; i < end; i++) {
            b[i] = s[i];
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        For i = 0 To [end] - 1
            b(i) = s(i)
        Next
    End Sub
End Class");
        }

        [Fact]
        public async Task ForTupleDeconstruction()
        {
            await TestConversionCSharpToVisualBasic(@"public class SolutionConverter
{
    private static string ApplyReplacements(string originalText, IEnumerable<(string, string)> replacements)
    {
        foreach (var (oldValue, newValue) in replacements)
        {
            originalText = Regex.Replace(originalText, oldValue, newValue, RegexOptions.IgnoreCase);
        }

        return originalText;
    }
}", @"Public Class SolutionConverter
    Private Shared Function ApplyReplacements(ByVal originalText As String, ByVal replacements As IEnumerable(Of (String, String))) As String
        For Each oldValueNewValue In replacements
            Dim oldValue = oldValueNewValue.Item1
            Dim newValue = oldValueNewValue.Item2
            originalText = Regex.Replace(originalText, oldValue, newValue, RegexOptions.IgnoreCase)
        Next

        Return originalText
    End Function
End Class");
        }

        [Fact]
        public async Task LabeledAndForStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class GotoTest1
{
    static void Main()
    {
        int x = 200, y = 4;
        int count = 0;
        string[,] array = new string[x, y];

        for (int i = 0; i < x; i++)

            for (int j = 0; j < y; j++)
                array[i, j] = (++count).ToString();

        Console.Write(""Enter the number to search for: "");

        string myNumber = Console.ReadLine();

                for (int i = 0; i < x; i++)
                {
                    for (int j = 0; j < y; j++)
                    {
                        if (array[i, j].Equals(myNumber))
                        {
                            goto Found;
                        }
                    }
                }

            Console.WriteLine(""The number {0} was not found."", myNumber);
            goto Finish;

        Found:
            Console.WriteLine(""The number {0} is found."", myNumber);

        Finish:
            Console.WriteLine(""End of search."");

            Console.WriteLine(""Press any key to exit."");
            Console.ReadKey();
        }
    }", @"Friend Class GotoTest1
    Private Shared Sub Main()
        Dim x = 200, y = 4
        Dim count = 0
        Dim array = New String(x - 1, y - 1) {}

        For i = 0 To x - 1

            For j = 0 To y - 1
                array(i, j) = Threading.Interlocked.Increment(count).ToString
            Next
        Next

        Console.Write(""Enter the number to search for: "")
        Dim myNumber = Console.ReadLine

        For i = 0 To x - 1

            For j = 0 To y - 1

                If array(i, j).Equals(myNumber) Then
                    GoTo Found
                End If
            Next
        Next

        Console.WriteLine(""The number {0} was not found."", myNumber)
        GoTo Finish
Found:
        Console.WriteLine(""The number {0} is found."", myNumber)
Finish:
        Console.WriteLine(""End of search."")
        Console.WriteLine(""Press any key to exit."")
        Console.ReadKey
    End Sub
End Class");
        }

        [Fact]
        public async Task ThrowStatement()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(object nullObject)
    {
        if (nullObject == null)
            throw new ArgumentNullException(nameof(nullObject));
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))
    End Sub
End Class");
        }

        [Fact]
        public async Task AddRemoveHandler()
        {
            await TestConversionCSharpToVisualBasic(@"using System;

class TestClass
{
    public event EventHandler MyEvent;

    void TestMethod(EventHandler e)
    {
        this.MyEvent += e;
        this.MyEvent += MyHandler;
    }

    void TestMethod2(EventHandler e)
    {
        this.MyEvent -= e;
        this.MyEvent -= MyHandler;
    }

    void MyHandler(object sender, EventArgs e)
    {

    }
}", @"Imports System

Friend Class TestClass
    Public Event MyEvent As EventHandler

    Private Sub TestMethod(ByVal e As EventHandler)
        AddHandler MyEvent, e
        AddHandler MyEvent, AddressOf MyHandler
    End Sub

    Private Sub TestMethod2(ByVal e As EventHandler)
        RemoveHandler MyEvent, e
        RemoveHandler MyEvent, AddressOf MyHandler
    End Sub

    Private Sub MyHandler(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
End Class");
        }

        [Fact]
        public async Task SelectCase1()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(int number)
    {
        switch (number) {
            case 0:
            case 1:
            case 2:
                Console.Write(""number is 0, 1, 2"");
                break;
            case 3:
                Console.Write(""section 3"");
                goto case 5;
            case 4:
                Console.Write(""section 4"");
                goto default;
            default:
                Console.Write(""default section"");
                break;
            case 5:
                Console.Write(""section 5"");
                break;
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal number As Integer)
        Select Case number
            Case 0, 1, 2
                Console.Write(""number is 0, 1, 2"")
            Case 3
                Console.Write(""section 3"")
                GoTo _Select0_Case5
            Case 4
                Console.Write(""section 4"")
                GoTo _Select0_CaseDefault
            Case 5
_Select0_Case5:
                Console.Write(""section 5"")
            Case Else
_Select0_CaseDefault:
                Console.Write(""default section"")
        End Select
    End Sub
End Class");
        }

        [Fact]
        public async Task SelectCase_WithDotInCaseLabel()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    void TestMethod(double number)
    {
        switch (number) {
            case 3:
                Console.Write(""section 3"");
                goto case 5.5;
            case 5.5:
                Console.Write(""section 5"");
                break;
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal number As Double)
        Select Case number
            Case 3
                Console.Write(""section 3"")
                GoTo _Select0_Case5_5
            Case 5.5
_Select0_Case5_5:
                Console.Write(""section 5"")
        End Select
    End Sub
End Class");
        }

        [Fact]
        public async Task TryCatch()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    static bool Log(string message)
    {
        Console.WriteLine(message);
        return false;
    }

    void TestMethod(int number)
    {
        try {
            Console.WriteLine(""try"");
        } catch (Exception e) {
            Console.WriteLine(""catch1"");
        } catch {
            Console.WriteLine(""catch all"");
        } finally {
            Console.WriteLine(""finally"");
        }
        try {
            Console.WriteLine(""try"");
        } catch (System.IO.IOException) {
            Console.WriteLine(""catch1"");
        } catch (Exception e) when (Log(e.Message)) {
            Console.WriteLine(""catch2"");
        }
        try {
            Console.WriteLine(""try"");
        } finally {
            Console.WriteLine(""finally"");
        }
    }
}", @"Friend Class TestClass
    Private Shared Function Log(ByVal message As String) As Boolean
        Console.WriteLine(message)
        Return False
    End Function

    Private Sub TestMethod(ByVal number As Integer)
        Try
            Console.WriteLine(""try"")
        Catch e As Exception
            Console.WriteLine(""catch1"")
        Catch
            Console.WriteLine(""catch all"")
        Finally
            Console.WriteLine(""finally"")
        End Try

        Try
            Console.WriteLine(""try"")
        Catch __unusedIOException1__ As IOException
            Console.WriteLine(""catch1"")
        Catch e As Exception When Log(e.Message)
            Console.WriteLine(""catch2"")
        End Try

        Try
            Console.WriteLine(""try"")
        Finally
            Console.WriteLine(""finally"")
        End Try
    End Sub
End Class");
        }

        [Fact]
        public async Task Yield()
        {
            await TestConversionCSharpToVisualBasic(@"class TestClass
{
    IEnumerable<int> TestMethod(int number)
    {
        if (number < 0)
            yield break;
        for (int i = 0; i < number; i++)
            yield return i;
    }
}", @"Friend Class TestClass
    Private Iterator Function TestMethod(ByVal number As Integer) As IEnumerable(Of Integer)
        If number < 0 Then Return

        For i = 0 To number - 1
            Yield i
        Next
    End Function
End Class");
        }
    }
}
