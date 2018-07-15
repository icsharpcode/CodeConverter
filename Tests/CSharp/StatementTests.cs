using Xunit;

namespace CodeConverter.Tests.CSharp
{
    public class StatementTests : ConverterTestBase
    {
        [Fact]
        public void EmptyStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        If True Then
        End If

        While True
        End While

        Do
        Loop While True
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        if (true)
        {
        }

        while (true)
        {
        }

        do
        {
        }
        while (true);
    }
}");
        }

        [Fact]
        public void AssignmentStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int b;
        b = 0;
    }
}");
        }

        [Fact]
        public void AssignmentStatementInDeclaration()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer = 0
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int b = 0;
    }
}");
        }

        [Fact]
        public void AssignmentStatementInVarDeclaration()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = 0
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var b = 0;
    }
}");
        }

        [Fact]
        public void ObjectInitializationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As String
        b = New String(""test"")
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        string b;
        b = new string(""test"");
    }
}");
        }

        [Fact]
        public void ObjectInitializationStatementInDeclaration()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As String = New String(""test"")
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        string b = new string(""test"");
    }
}");
        }

        [Fact]
        public void ObjectInitializationStatementInVarDeclaration()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = New String(""test"")
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var b = new string(""test"");
    }
}");
        }

        [Fact]
        public void ArrayDeclarationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[] b;
    }
}");
        }

        [Fact]
        public void ArrayEraseAndRedimStatement()
        {
            // One statement turns into two, so can't auto-test comments
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass
    Shared Function TestMethod(numArray As Integer(), numArray2 As Integer()) As Integer()
        ReDim numArray(3)
        Erase numArray
        numArray2(1) = 1
        ReDim Preserve numArray(5), numArray2(5)
        Dim y(6, 5) As Integer
        y(2,3) = 1
        ReDim Preserve y(6,8)
        Return numArray2
    End Function
End Class", @"using System;

public class TestClass
{
    public static int[] TestMethod(int[] numArray, int[] numArray2)
    {
        numArray = new int[4];
        numArray = null;
        numArray2[1] = 1;
        var oldNumArray = numArray;
        numArray = new int[6];
        if (oldNumArray != null)
            Array.Copy(oldNumArray, numArray, Math.Min(6, oldNumArray.Length));
        var oldNumArray2 = numArray2;
        numArray2 = new int[6];
        if (oldNumArray2 != null)
            Array.Copy(oldNumArray2, numArray2, Math.Min(6, oldNumArray2.Length));
        int[,] y = new int[7, 6];
        y[2, 3] = 1;
        var oldY = y;
        y = new int[7, 9];
        if (oldY != null)
            for (var i = 0; i <= oldY.Length / oldY.GetLength(1) - 1; ++i)
                Array.Copy(oldY, i * oldY.GetLength(1), y, i * y.GetLength(1), Math.Min(oldY.GetLength(1), y.GetLength(1)));
        return numArray2;
    }
}");
        }

        [Fact]
        public void EndStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        End
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        System.Environment.Exit(0);
    }
}");
        }

        [Fact]
        public void StopStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Stop
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        System.Diagnostics.Debugger.Break();
    }
}");
        }

        [Fact]
        public void WithBlock()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        With New System.Text.StringBuilder
            .Capacity = 20
            ?.Length = 0
        End With
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock = new System.Text.StringBuilder();
            withBlock.Capacity = 20;
            withBlock?.Length = 0;
        }
    }
}");
        }

        [Fact]
        public void WithBlock2()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Imports System.Data.SqlClient

Class TestClass
    Private Sub Save()
        Using cmd As SqlCommand = new SqlCommand()
            With cmd
            .ExecuteNonQuery()
            ?.ExecuteNonQuery()
            .ExecuteNonQuery
            ?.ExecuteNonQuery
            End With
        End Using
    End Sub
End Class", @"using System.Data.SqlClient;

class TestClass
{
    private void Save()
    {
        using (SqlCommand cmd = new SqlCommand())
        {
            {
                var withBlock = cmd;
                withBlock.ExecuteNonQuery();
                withBlock?.ExecuteNonQuery();
                withBlock.ExecuteNonQuery();
                withBlock?.ExecuteNonQuery();
            }
        }
    }
}");
        }

        [Fact]
        public void NestedWithBlock()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        With New System.Text.StringBuilder
            Dim withBlock as Integer = 3
            With New System.Text.StringBuilder
                Dim withBlock1 as Integer = 4
                .Capacity = withBlock1
            End With

            .Length = withBlock
        End With
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        {
            var withBlock2 = new System.Text.StringBuilder();
            int withBlock = 3;
            {
                var withBlock3 = new System.Text.StringBuilder();
                int withBlock1 = 4;
                withBlock3.Capacity = withBlock1;
            }

            withBlock2.Length = withBlock;
        }
    }
}");
        }
        [Fact]
        public void ArrayInitializationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = {1, 2, 3}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[] b = new[] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public void ArrayInitializationStatementInVarDeclaration()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = {1, 2, 3}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var b = new[] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public void ArrayInitializationStatementWithType()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer() {1, 2, 3}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[] b = new int[] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public void ArrayInitializationStatementWithLength()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer() = New Integer(2) {1, 2, 3}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[] b = new int[3] { 1, 2, 3 };
    }
}");
        }

        [Fact]
        public void MultidimensionalArrayDeclarationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,)
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[,] b;
    }
}");
        }

        [Fact]
        public void MultidimensionalArrayInitializationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,) = {{1, 2}, {3, 4}}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[,] b = new[] { { 1, 2 }, { 3, 4 } };
    }
}");
        }

        [Fact]
        public void MultidimensionalArrayInitializationStatementWithType()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer(,) = New Integer(,) {{1, 2}, {3, 4}}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[,] b = new int[,] { { 1, 2 }, { 3, 4 } };
    }
}");
        }

        [Fact]
        public void MultidimensionalArrayInitializationStatementWithAndWithoutLengths()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim a As Integer(,) = New Integer(,) {{1, 2}, {3, 4}}
        Dim b As Integer(,) = New Integer(1, 1) {{1, 2}, {3, 4}}
        Dim c as Integer(,,) = New Integer(,,) {{{1}}}
        Dim d as Integer(,,) = New Integer(0, 0, 0) {{{1}}}
        Dim e As Integer()(,) = New Integer()(,) {}
        Dim f As Integer()(,) = New Integer(-1)(,) {}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[,] a = new int[,] { { 1, 2 }, { 3, 4 } };
        int[,] b = new int[2, 2] { { 1, 2 }, { 3, 4 } };
        int[,,] c = new int[,,] { { { 1 } } };
        int[,,] d = new int[1, 1, 1] { { { 1 } } };
        int[][,] e = new int[][,] { };
        int[][,] f = new int[0][,] { };
    }
}");
        }

        [Fact]
        public void JaggedArrayDeclarationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()()
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[][] b;
    }
}");
        }

        [Fact]
        public void JaggedArrayInitializationStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[][] b = new[] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}");
        }

        [Fact]
        public void JaggedArrayInitializationStatementWithType()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b = New Integer()() {New Integer() {1}}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        var b = new int[][] { new int[] { 1 } };
    }
}");
        }

        [Fact]
        public void JaggedArrayInitializationStatementWithLength()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer()() = New Integer(1)() {New Integer() {1, 2}, New Integer() {3, 4}}
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[][] b = new int[2][] { new int[] { 1, 2 }, new int[] { 3, 4 } };
    }
}");
        }

        [Fact]
        public void DeclarationStatements()
        {
            TestConversionVisualBasicToCSharpWithoutComments(
@"Class Test
    Private Sub TestMethod()
the_beginning:
        Dim value As Integer = 1
        Const myPIe As Double = System.Math.PI
        Dim text = ""This is my text!""
        GoTo the_beginning
    End Sub
End Class", @"class Test
{
    private void TestMethod()
    {
    the_beginning:
        ;
        int value = 1;
        const double myPIe = System.Math.PI;
        var text = ""This is my text!"";
        goto the_beginning;
    }
}");
        }

        [Fact]
        public void DeclareStatement()
        {
            // Intentionally uses a type name with a different casing as the loop variable, i.e. "process" to test name resolution
            TestConversionVisualBasicToCSharp(@"Imports System.Diagnostics

Public Class AcmeClass
    Private Declare Function SetForegroundWindow Lib ""user32"" (ByVal hwnd As Int32) As Long

    Public Shared Sub Main()
        For Each process In Process.GetProcesses().Where(Function(p) Not String.IsNullOrEmpty(p.MainWindowTitle))
            SetForegroundWindow(process.MainWindowHandle.ToInt32())
            Thread.Sleep(1000)
        Next
    End Sub
End Class"
                , @"using System;
using System.Diagnostics;
using System.Linq;

public class AcmeClass
{
    [System.Runtime.InteropServices.DllImport(""user32"")]
    private static extern long SetForegroundWindow(Int32 hwnd);

    public static void Main()
    {
        foreach (var process in Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)))
        {
            SetForegroundWindow(process.MainWindowHandle.ToInt32());
            Thread.Sleep(1000);
        }
    }
}");
        }

        [Fact]
        public void IfStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
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
End Class", @"class TestClass
{
    private void TestMethod(int a)
    {
        int b;

        if (a == 0)
            b = 0;
        else if (a == 1)
            b = 1;
        else if (a == 2 || a == 3)
            b = 2;
        else
            b = 3;
    }
}");
        }

        [Fact]
        public void NestedBlockStatementsKeepSameNesting()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Shared Function FindTextInCol(w As String, pTitleRow As Integer, startCol As Integer, needle As String) As Integer

        For c As Integer = startCol To w.Length
            If needle = """" Then
                If String.IsNullOrWhiteSpace(w(c).ToString) Then
                    Return c
                End If
            Else
                If w(c).ToString = needle Then
                    Return c
                End If
            End If
        Next
        Return -1
    End Function
End Class", @"class TestClass
{
    public static int FindTextInCol(string w, int pTitleRow, int startCol, string needle)
    {
        for (int c = startCol; c <= w.Length; c++)
        {
            if (needle == """")
            {
                if (string.IsNullOrWhiteSpace(w[c].ToString()))
                    return c;
            }
            else if (w[c].ToString() == needle)
                return c;
        }
        return -1;
    }
}");
        }

        [Fact]
        public void WhileStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        While b = 0
            If b = 2 Then Continue While
            If b = 3 Then Exit While
            b = 1
        End While
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
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
}");
        }

        [Fact]
        public void SimpleDoStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
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
        while (true);
    }
}");
        }

        [Fact]
        public void DoWhileStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
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
}");
        }

        [Fact]
        public void IncompleteStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
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
}");
        }

        [Fact]
        public void ForEachStatementWithExplicitType()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val As Integer In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (int val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public void ForEachStatementWithVar()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (var val in values)
        {
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public void SyncLockStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))

        SyncLock nullObject
            Console.WriteLine(nullObject)
        End SyncLock
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject == null)
            throw new ArgumentNullException(nameof(nullObject));

        lock (nullObject)
            Console.WriteLine(nullObject);
    }
}");
        }

        [Fact]
        public void ForWithSingleStatement()
        {
            // Comment from "Next" gets pushed up to previous line
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b, s As Integer()
        For i = 0 To [end]
            b(i) = s(i)
        Next
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[] b, s;
        for (var i = 0; i <= end; i++)
            b[i] = s[i];
    }
}");
        }

        [Fact]
        public void ForWithBlock()
        {
            // Comment from "Next" gets pushed up to previous line
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod()
        Dim b, s As Integer()
        For i = 0 To [end] - 1
            b(i) = s(i)
        Next
    End Sub
End Class", @"class TestClass
{
    private void TestMethod()
    {
        int[] b, s;
        for (var i = 0; i <= end - 1; i++)
            b[i] = s[i];
    }
}");
        }

        [Fact]
        public void LabeledAndForStatement()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class GotoTest1
    Private Shared Sub Main()
        Dim x As Integer = 200, y As Integer = 4
        Dim count As Integer = 0
        Dim array As String(,) = New String(x - 1, y - 1) {}

        For i As Integer = 0 To x - 1

            For j As Integer = 0 To y - 1
                array(i, j) = (System.Threading.Interlocked.Increment(count)).ToString()
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
End Class", @"using System;

class GotoTest1
{
    private static void Main()
    {
        int x = 200;
        int y = 4;
        int count = 0;
        string[,] array = new string[x - 1 + 1, y - 1 + 1];

        for (int i = 0; i <= x - 1; i++)
        {
            for (int j = 0; j <= y - 1; j++)
                array[i, j] = (System.Threading.Interlocked.Increment(ref count)).ToString();
        }

        Console.Write(""Enter the number to search for: "");
        string myNumber = Console.ReadLine();

        for (int i = 0; i <= x - 1; i++)
        {
            for (int j = 0; j <= y - 1; j++)
            {
                if (array[i, j].Equals(myNumber))
                    goto Found;
            }
        }

        Console.WriteLine(""The number {0} was not found."", myNumber);
        goto Finish;
    Found:
        ;
        Console.WriteLine(""The number {0} is found."", myNumber);
    Finish:
        ;
        Console.WriteLine(""End of search."");
        Console.WriteLine(""Press any key to exit."");
        Console.ReadKey();
    }
}");
        }

        [Fact]
        public void ThrowStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal nullObject As Object)
        If nullObject Is Nothing Then Throw New ArgumentNullException(NameOf(nullObject))
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod(object nullObject)
    {
        if (nullObject == null)
            throw new ArgumentNullException(nameof(nullObject));
    }
}");
        }

        [Fact]
        public void CallStatement()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Call (Sub() Console.Write(""Hello""))
        Call (Sub() Console.Write(""Hello""))()
        Call TestMethod
        Call TestMethod()
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod()
    {
        (() => Console.Write(""Hello""))();
        (() => Console.Write(""Hello""))();
        TestMethod();
        TestMethod();
    }
}");
        }

        [Fact]
        public void AddRemoveHandler()
        {
            TestConversionVisualBasicToCSharp(@"Class TestClass
    Public Event MyEvent As EventHandler

    Private Sub TestMethod(ByVal e As EventHandler)
        AddHandler Me.MyEvent, e
        AddHandler Me.MyEvent, AddressOf MyHandler
    End Sub

    Private Sub TestMethod2(ByVal e As EventHandler)
        RemoveHandler Me.MyEvent, e
        RemoveHandler Me.MyEvent, AddressOf MyHandler
    End Sub

    Private Sub MyHandler(ByVal sender As Object, ByVal e As EventArgs)
    End Sub
End Class", @"using System;

class TestClass
{
    public event EventHandler MyEvent;

    private void TestMethod(EventHandler e)
    {
        this.MyEvent += e;
        this.MyEvent += MyHandler;
    }

    private void TestMethod2(EventHandler e)
    {
        this.MyEvent -= e;
        this.MyEvent -= MyHandler;
    }

    private void MyHandler(object sender, EventArgs e)
    {
    }
}");
        }

        [Fact]
        public void SelectCase1()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Sub TestMethod(ByVal number As Integer)
        Select Case number
            Case 0, 1, 2
                Console.Write(""number is 0, 1, 2"")
            Case 5
                Console.Write(""section 5"")
            Case Else
                Console.Write(""default section"")
        End Select
    End Sub
End Class", @"using System;

class TestClass
{
    private void TestMethod(int number)
    {
        switch (number)
        {
            case 0:
            case 1:
            case 2:
                {
                    Console.Write(""number is 0, 1, 2"");
                    break;
                }

            case 5:
                {
                    Console.Write(""section 5"");
                    break;
                }

            default:
                {
                    Console.Write(""default section"");
                    break;
                }
        }
    }
}");
        }

        [Fact]
        public void SelectCaseWithExpression()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass
    Shared Function TimeAgo(daysAgo As Integer) As String
        Select Case daysAgo
            Case 0 To 3, 4, Is >= 5, Is < 6, Is <= 7
                Return ""this week""
            Case Is > 0
                Return daysAgo \ 7 & "" weeks ago""
            Case Else
                Return ""in the future""
        End Select
    End Function
End Class", @"public class TestClass
{
    public static string TimeAgo(int daysAgo)
    {
        switch (daysAgo)
        {
            case object _ when 0 <= daysAgo && daysAgo <= 3:
            case 4:
            case object _ when daysAgo >= 5:
            case object _ when daysAgo < 6:
            case object _ when daysAgo <= 7:
                {
                    return ""this week"";
                }

            case object _ when daysAgo > 0:
                {
                    return daysAgo / 7 + "" weeks ago"";
                }

            default:
                {
                    return ""in the future"";
                }
        }
    }
}");
        }

        [Fact]
        public void SelectCaseWithExpression2()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Public Class TestClass2
    Function CanDoWork(Something As Object) As Boolean
        Select Case True
            Case Today.DayOfWeek = DayOfWeek.Saturday Or Today.DayOfWeek = DayOfWeek.Sunday
                ' we do not work on weekends
                Return False
            Case Not IsSqlAlive()
                ' Database unavailable
                Return False
            Case TypeOf Something Is Integer
                ' Do something with the Integer
                Return True
            Case Else
                ' Do something else 
                Return False
        End Select
    End Function

    Private Function IsSqlAlive() As Boolean
        ' Do something to test SQL Server
        Return True
    End Function
End Class", @"using System;

public class TestClass2
{
    public bool CanDoWork(object Something)
    {
        switch (true)
        {
            case object _ when DateTime.Today.DayOfWeek == DayOfWeek.Saturday | DateTime.Today.DayOfWeek == DayOfWeek.Sunday:
                {
                    // we do not work on weekends
                    return false;
                }

            case object _ when !IsSqlAlive():
                {
                    // Database unavailable
                    return false;
                }

            case object _ when Something is int:
                {
                    // Do something with the Integer
                    return true;
                }

            default:
                {
                    // Do something else 
                    return false;
                }
        }
    }

    private bool IsSqlAlive()
    {
        // Do something to test SQL Server
        return true;
    }
}");
        }

        [Fact]
        public void TryCatch()
        {
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
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
        Catch e2 As NotImplementedException
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
End Class", @"using System;

class TestClass
{
    private static bool Log(string message)
    {
        Console.WriteLine(message);
        return false;
    }

    private void TestMethod(int number)
    {
        try
        {
            Console.WriteLine(""try"");
        }
        catch (Exception e)
        {
            Console.WriteLine(""catch1"");
        }
        catch
        {
            Console.WriteLine(""catch all"");
        }
        finally
        {
            Console.WriteLine(""finally"");
        }

        try
        {
            Console.WriteLine(""try"");
        }
        catch (NotImplementedException e2)
        {
            Console.WriteLine(""catch1"");
        }
        catch (Exception e) when (Log(e.Message))
        {
            Console.WriteLine(""catch2"");
        }

        try
        {
            Console.WriteLine(""try"");
        }
        finally
        {
            Console.WriteLine(""finally"");
        }
    }
}");
        }

        [Fact]
        public void Yield()
        {
            // Comment from "Next" gets pushed up to previous line
            TestConversionVisualBasicToCSharpWithoutComments(@"Class TestClass
    Private Iterator Function TestMethod(ByVal number As Integer) As IEnumerable(Of Integer)
        If number < 0 Then Return

        For i As Integer = 0 To number - 1
            Yield i
        Next
    End Function
End Class", @"using System.Collections.Generic;

class TestClass
{
    private IEnumerable<int> TestMethod(int number)
    {
        if (number < 0)
            yield break;

        for (int i = 0; i <= number - 1; i++)
            yield return i;
    }
}");
        }
    }
}
