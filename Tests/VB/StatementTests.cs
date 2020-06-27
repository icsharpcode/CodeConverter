using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB
{
    public class StatementTests : ConverterTestBase
    {
        [Fact]
        public async Task EmptyStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task ForConvertedToWhile_BreakAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    void TestMethod() {
        for (;;)
            break;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        While True
            Exit While
        End While
    End Sub
End Class");
        }

        [Fact]
        public async Task ForConvertedToWhile_BreakContinueAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    void TestMethod(int arg) {
        for (;;) //Becomes while loop
        {
            if (arg == 3) break;
            switch (arg)
            {
                case 1:
                    break; //From switch
                case 2:
                    break; //From switch
                default:
                    continue; // Outer while loop
            }
            for (var i = 0; i < arg; i++) // Becomes For Next loop
            {
                if (arg != 1) break; // From inner for loop
                continue; // Inner for loop
            }
            continue; // Outer while loop
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod(ByVal arg As Integer)
        While True 'Becomes while loop
            If arg = 3 Then Exit While

            Select Case arg
                Case 1 'From switch
                Case 2 'From switch
                Case Else
                    Continue While ' Outer while loop
            End Select

            For i = 0 To arg - 1 ' Becomes For Next loop
                If arg <> 1 Then Exit For ' From inner for loop
                Continue For ' Inner for loop
            Next

            Continue While ' Outer while loop
        End While
    End Sub
End Class");
        }

        [Fact]
        public async Task AssignmentStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task AssignmentStatementInDeclarationAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int b = 0;
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer = 0
    End Sub
End Class");
        }

        [Fact]
        public async Task AssignmentStatementInVarDeclarationAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task ObjectInitializationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 source compilation errors:
CS1503: Argument 1: cannot convert from 'string' to 'char*'");
        }

        [Fact]
        public async Task ObjectInitializationStatementInDeclarationAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        string b = new string(""test"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As String = New String(""test"")
    End Sub
End Class

1 source compilation errors:
CS1503: Argument 1: cannot convert from 'string' to 'char*'");
        }

        [Fact]
        public async Task ObjectInitializationStatementInVarDeclarationAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        var b = new string(""test"");
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = New String(""test"")
    End Sub
End Class

1 source compilation errors:
CS1503: Argument 1: cannot convert from 'string' to 'char*'");
        }

        [Fact]
        public async Task ArrayDeclarationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task ArrayInitializationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int[] b = { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayInitializationStatementInVarDeclarationAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        var b = { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b = {1, 2, 3}
    End Sub
End Class

1 source compilation errors:
CS0820: Cannot initialize an implicitly-typed variable with an array initializer");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int[] b = new int[] { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer() {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task ArrayInitializationStatementWithLengthAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int[] b = new int[3] { 1, 2, 3 };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer(2) {1, 2, 3}
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayDeclarationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task MultidimensionalArrayInitializationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        Dim b As Integer(,) = {
        {1, 2},
        {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatementWithTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        Dim b As Integer(,) = New Integer(,) {
        {1, 2},
        {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task MultidimensionalArrayInitializationStatementWithLengthsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        Dim b As Integer(,) = New Integer(1, 1) {
        {1, 2},
        {3, 4}}
    End Sub
End Class

1 source compilation errors:
CS1002: ; expected");
        }

        [Fact]
        public async Task JaggedArrayDeclarationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task JaggedArrayInitializationStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int[][] b = { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatementWithTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int[][] b = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = New Integer()() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task JaggedArrayInitializationStatementWithLengthAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        int[][] b = new int[2][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = New Integer(1)() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class");
        }

        [Fact]
        public async Task DeclarationStatementsAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(
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
        Dim value As Integer = 1
        Const myPIe As Double = Math.PI
        Dim text = ""This is my text!""
        GoTo the_beginning
    End Sub
End Class");
        }

        [Fact]
        public async Task IfStatementWithoutBlockAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task IfStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task BlockStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 source compilation errors:
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task WhileStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task UnsafeStatementsWithNoVbEquivalentShowErrorInlineCharacterizationAsync()
        {
            string convertedCode = await ConvertAsync<CSToVBConversion>(@"class TestClass
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
        public async Task DoWhileStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        public async Task ForEachStatementWithExplicitTypeAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
        For Each val As Integer In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class");
        }

        [Fact]
        public async Task ForEachStatementWithVarAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 target compilation errors:
BC30516: Overload resolution failed because no accessible 'Val' accepts this number of arguments.");
        }

        [Fact]
        public async Task SyncLockStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

2 source compilation errors:
CS0246: The type or namespace name 'ArgumentNullException' could not be found (are you missing a using directive or an assembly reference?)
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task ForWithUnknownConditionAndSingleStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

4 source compilation errors:
CS0103: The name 'i' does not exist in the current context
CS0103: The name 'unknownCondition' does not exist in the current context
CS0103: The name 'b' does not exist in the current context
CS0103: The name 's' does not exist in the current context
4 target compilation errors:
BC30451: 'i' is not declared. It may be inaccessible due to its protection level.
BC30451: 'unknownCondition' is not declared. It may be inaccessible due to its protection level.
BC30451: 'b' is not declared. It may be inaccessible due to its protection level.
BC30451: 's' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task ForWithUnknownConditionAndBlockAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        for (int i = 0; unknownCondition; i++) { // Increment moves to bottom of loop
            b[i] = s[i];
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        Dim i As Integer = 0

        While unknownCondition
            b(i) = s(i)
            i += 1 ' Increment moves to bottom of loop
        End While
    End Sub
End Class

3 source compilation errors:
CS0103: The name 'unknownCondition' does not exist in the current context
CS0103: The name 'b' does not exist in the current context
CS0103: The name 's' does not exist in the current context
3 target compilation errors:
BC30451: 'unknownCondition' is not declared. It may be inaccessible due to its protection level.
BC30451: 'b' is not declared. It may be inaccessible due to its protection level.
BC30451: 's' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task ForWithSingleStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

4 source compilation errors:
CS0103: The name 'i' does not exist in the current context
CS0103: The name 'end' does not exist in the current context
CS0103: The name 'b' does not exist in the current context
CS0103: The name 's' does not exist in the current context
3 target compilation errors:
BC30451: 'end' is not declared. It may be inaccessible due to its protection level.
BC30451: 'b' is not declared. It may be inaccessible due to its protection level.
BC30451: 's' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task ForWithBlockAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

4 source compilation errors:
CS0103: The name 'i' does not exist in the current context
CS0103: The name 'end' does not exist in the current context
CS0103: The name 'b' does not exist in the current context
CS0103: The name 's' does not exist in the current context
3 target compilation errors:
BC30451: 'end' is not declared. It may be inaccessible due to its protection level.
BC30451: 'b' is not declared. It may be inaccessible due to its protection level.
BC30451: 's' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task ForWithBlockDecrementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    void TestMethod()
    {
        for (i = end; i > 0; i--) {
            b[i] = s[i];
        }
    }
}", @"Friend Class TestClass
    Private Sub TestMethod()
        For i = [end] To 0 + 1 Step -1
            b(i) = s(i)
        Next
    End Sub
End Class

4 source compilation errors:
CS0103: The name 'i' does not exist in the current context
CS0103: The name 'end' does not exist in the current context
CS0103: The name 'b' does not exist in the current context
CS0103: The name 's' does not exist in the current context
3 target compilation errors:
BC30451: 'end' is not declared. It may be inaccessible due to its protection level.
BC30451: 'b' is not declared. It may be inaccessible due to its protection level.
BC30451: 's' is not declared. It may be inaccessible due to its protection level.");
        }
        [Fact]
        public async Task ForWithDateTimeVariablesAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    void TestMethod() {
        int summary = 0;
        for (int month = 1; month <= 12; month++) {
            summary += month;
        }
    }
}",
                @"Friend Class TestClass
    Private Sub TestMethod()
        Dim summary As Integer = 0

        For month As Integer = 1 To 12
            summary += month
        Next
    End Sub
End Class");
        }
        [Fact(Skip = "Should we rename, or add If True End If block?")]
        public async Task Conflict_DeclarationsAfterConvertionForToWhileAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    double height;
    void TestMethod() {
        for(double y = 0d; y < height; y += 10d)
            Draw(y);
        for(double y = 0d; y < height; y += 20d)
            Draw(y);
    }
    void Draw(double height) {
    }
}",
                @"Friend Class TestClass
    Private height As Double

    Private Sub TestMethod()
        Dim y = 0R

        While y < height
            Draw(y)
            y += 10R
        End While

        Dim y = 0R

        While y < height
            Draw(y)
            y += 20R
        End While
    End Sub

    Private Sub Draw(ByVal height As Double)
    End Sub
End Class");
        }
        [Fact]
        public async Task SubWithForEach_MultilineAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    void TestMethod(IEnumerable<int> counts) {
        int summary = 0;
        Action action = () => {
            foreach(var c in counts) {
                var current = c;
                summary += current;
            }
        };
    }
}",
                @"Friend Class TestClass
    Private Sub TestMethod(ByVal counts As IEnumerable(Of Integer))
        Dim summary As Integer = 0
        Dim action As Action = Sub()
                                   For Each c In counts
                                       Dim current = c
                                       summary += current
                                   Next
                               End Sub
    End Sub
End Class

2 source compilation errors:
CS0246: The type or namespace name 'IEnumerable<>' could not be found (are you missing a using directive or an assembly reference?)
CS0246: The type or namespace name 'Action' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task ForTupleDeconstructionAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"public class SolutionConverter
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
End Class

5 source compilation errors:
CS0246: The type or namespace name 'IEnumerable<>' could not be found (are you missing a using directive or an assembly reference?)
CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'oldValue'.
CS8130: Cannot infer the type of implicitly-typed deconstruction variable 'newValue'.
CS0103: The name 'Regex' does not exist in the current context
CS0103: The name 'RegexOptions' does not exist in the current context
2 target compilation errors:
BC30451: 'Regex' is not declared. It may be inaccessible due to its protection level.
BC30451: 'RegexOptions' is not declared. It may be inaccessible due to its protection level.");
        }

        [Fact]
        public async Task LabeledAndForStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class GotoTest1
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
        Dim x As Integer = 200, y As Integer = 4
        Dim count As Integer = 0
        Dim array As String(,) = New String(x - 1, y - 1) {}

        For i As Integer = 0 To x - 1

            For j As Integer = 0 To y - 1
                array(i, j) = Threading.Interlocked.Increment(count).ToString()
            Next
        Next

        Console.Write(""Enter the number to search for: "")
        Dim myNumber As String = Console.ReadLine()

        For i As Integer = 0 To x - 1

            For j As Integer = 0 To y - 1

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
        Console.ReadKey()
    End Sub
End Class

1 source compilation errors:
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task ThrowStatementAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 source compilation errors:
CS0246: The type or namespace name 'ArgumentNullException' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task AddRemoveHandlerAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System;

class TestClass
{
    public event EventHandler SomethingHappening;

    void TestMethod(EventHandler e)
    {
        this.SomethingHappening += e;
        SomethingHappening += MyHandler;
    }

    void TestMethod2(EventHandler e)
    {
        this.SomethingHappening -= e;
        SomethingHappening -= MyHandler;
    }

    void MyHandler(object sender, EventArgs e)
    {

    }
}", @"Imports System

Friend Class TestClass
    Public Event SomethingHappening As EventHandler

    Private Sub TestMethod(ByVal e As EventHandler)
        AddHandler SomethingHappening, e
        AddHandler SomethingHappening, AddressOf MyHandler
    End Sub

    Private Sub TestMethod2(ByVal e As EventHandler)
        RemoveHandler SomethingHappening, e
        RemoveHandler SomethingHappening, AddressOf MyHandler
    End Sub

    Private Sub MyHandler(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
End Class");
        }
        [Fact]
        public async Task AddHandlerInSimpleLambdaAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"using System;
using System.ComponentModel;
using System.Collections.Generic;

class TestClass {
    List<INotifyPropertyChanged> items;

    void TestMethod(EventHandler e) {
        items.ForEach(x => x.PropertyChanged += OnItemPropertyChanged);
    }

    void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e) { }
}",
@"Imports System
Imports System.ComponentModel
Imports System.Collections.Generic

Friend Class TestClass
    Private items As List(Of INotifyPropertyChanged)

    Private Sub TestMethod(ByVal e As EventHandler)
        items.ForEach(Sub(x) AddHandler x.PropertyChanged, AddressOf OnItemPropertyChanged)
    End Sub

    Private Sub OnItemPropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs)
    End Sub
End Class");
        }
        

        [Fact]
        public async Task SelectCase1Async()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
                Console.Write(""default section""); // Block moves to end - 1
                break; // Block moves to end - 2
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
                Console.Write(""default section"") ' Block moves to end - 1
                ' Block moves to end - 2
        End Select
    End Sub
End Class

1 source compilation errors:
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task SelectCase_WithDotInCaseLabelAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

1 source compilation errors:
CS0103: The name 'Console' does not exist in the current context");
        }

        [Fact]
        public async Task SelectCase_WithVarWhenClauseAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
{
    public bool IsFive(int aNum)
    {
        switch (aNum)
        {
            case var case2 when case2 == 5:
            {
                return true;
            }
        }
        return false;
    }
}", @"Friend Class TestClass
    Public Function IsFive(ByVal aNum As Integer) As Boolean
        Select Case aNum
            Case = 5
                Return True
        End Select

        Return False
    End Function
End Class");
        }

        [Fact]
        public async Task TryCatchAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"class TestClass
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
End Class

2 source compilation errors:
CS0103: The name 'Console' does not exist in the current context
CS0246: The type or namespace name 'Exception' could not be found (are you missing a using directive or an assembly reference?)");
        }

        [Fact]
        public async Task YieldAsync()
        {
            await TestConversionCSharpToVisualBasicAsync(@"using System.Collections.Generic;

class TestClass
{
    IEnumerable<int> TestMethod(int number)
    {
        if (number < 0)
            yield break;
        for (int i = 0; i < number; i++)
            yield return i;
    }
}", @"Imports System.Collections.Generic

Friend Class TestClass
    Private Iterator Function TestMethod(ByVal number As Integer) As IEnumerable(Of Integer)
        If number < 0 Then Return

        For i As Integer = 0 To number - 1
            Yield i
        Next
    End Function
End Class");
        }

        [Fact]
        public async Task ObjectCreationExpressionInInvocationExpressionAsync() {
            await TestConversionCSharpToVisualBasicAsync(
@"class TestClass {
    int field;
    TestClass(int param) {
        this.field = param;
    }
    
    static void TestMethod() {
        new TestClass(10).Initialize();
    }
    void Initialize() { }
}",
@"Friend Class TestClass
    Private field As Integer

    Private Sub New(ByVal param As Integer)
        field = param
    End Sub

    Private Shared Sub TestMethod()
        Call New TestClass(10).Initialize()
    End Sub

    Private Sub Initialize()
    End Sub
End Class");
        }
    }
}
