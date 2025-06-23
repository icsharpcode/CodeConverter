using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using VerifyXunit;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// For generic loop related tests. Also see ExitableMethodExecutableStatementTests for tests of Exit Do, Exit For, etc.
/// </summary>
public class LoopStatementTests : ConverterTestBase
{

    [Fact]
    public async Task UntilStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod(rand As Random)
        Dim charIndex As Integer
        ' allow only digits and letters
        Do
            charIndex = rand.Next(48, 123)
        Loop Until (charIndex >= 48 AndAlso charIndex <= 57) OrElse (charIndex >= 65 AndAlso charIndex <= 90) OrElse (charIndex >= 97 AndAlso charIndex <= 122)
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(Random rand)
    {
        int charIndex;
        // allow only digits and letters
        do
            charIndex = rand.Next(48, 123);
        while ((charIndex < 48 || charIndex > 57) && (charIndex < 65 || charIndex > 90) && (charIndex < 97 || charIndex > 122));
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task TwoForEachStatementsWithImplicitVariableCreationAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Friend Class Program
    Public Shared Sub Main(ByVal args As String())
        For idx = 0 To 10
        Next

        For idx = 0 To 10
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
internal partial class Program
{
    public static void Main(string[] args)
    {
        for (int idx = 0; idx <= 10; idx++)
        {
        }

        for (int idx = 0; idx <= 10; idx++)
        {
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task Int16ForLoopAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"    Sub DummyMethod()
        Dim someArray = New Integer() { 1, 2, 3}
        For index As Int16 = 0 To someArray.Length - 1
            Console.WriteLine(index)
        Next
    End Sub", extension: "vb"),
                Verifier.Verify(@"public void DummyMethod()
{
    int[] someArray = new int[] { 1, 2, 3 };
    for (short index = 0, loopTo = (short)(someArray.Length - 1); index <= loopTo; index++)
        Console.WriteLine(index);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ExternallyDeclaredLoopVariableAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Sub Main()
    Dim foo As Single = 3.5
    Dim index As Integer
    For index = Int(foo) To Int(foo * 3)
        Console.WriteLine(index)
    Next
End Sub", extension: "vb"),
                Verifier.Verify(@"public void Main()
{
    float foo = 3.5f;
    int index;
    var loopTo = (int)Math.Round(Conversion.Int(foo * 3f));
    for (index = (int)Math.Round(Conversion.Int(foo)); index <= loopTo; index++)
        Console.WriteLine(index);
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForNonNegativeStepAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer)
      For i As Integer = startIndex To endIndex Step -0
        Debug.WriteLine(i)
  Next
End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex)
    {
        for (int i = startIndex, loopTo = endIndex; i <= loopTo; i += -0)
            Debug.WriteLine(i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForNegativeStepAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer)
      For i As Integer = startIndex To endIndex Step -5
        Debug.WriteLine(i)
  Next
End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex)
    {
        for (int i = startIndex, loopTo = endIndex; i >= loopTo; i -= 5)
            Debug.WriteLine(i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForVariableStepAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer, [step] As Integer)
      For i As Integer = startIndex To endIndex Step [step]
        Debug.WriteLine(i)
  Next
End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex, int step)
    {
        for (int i = startIndex, loopTo = endIndex; step >= 0 ? i <= loopTo : i >= loopTo; i += step)
            Debug.WriteLine(i);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForEnumAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Friend Enum MyEnum
    Zero
    One
End Enum

Friend Class ForEnumAsync
    Sub PrintLoop(startIndex As MyEnum, endIndex As MyEnum, [step] As MyEnum)
      For i = startIndex To endIndex Step [step]
        Debug.WriteLine(i)
      Next
      For i2 As MyEnum = startIndex To endIndex Step [step]
        Debug.WriteLine(i2)
      Next
      For i3 As MyEnum = startIndex To endIndex Step 3
        Debug.WriteLine(i3)
      Next
      For i4 As MyEnum = startIndex To 4
        Debug.WriteLine(i4)
      Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System.Diagnostics;

internal enum MyEnum
{
    Zero,
    One
}

internal partial class ForEnumAsync
{
    public void PrintLoop(MyEnum startIndex, MyEnum endIndex, MyEnum step)
    {
        for (MyEnum i = startIndex, loopTo = endIndex; (int)step >= 0 ? i <= loopTo : i >= loopTo; i += (int)step)
            Debug.WriteLine(i);
        for (MyEnum i2 = startIndex, loopTo1 = endIndex; (int)step >= 0 ? i2 <= loopTo1 : i2 >= loopTo1; i2 += (int)step)
            Debug.WriteLine(i2);
        for (MyEnum i3 = startIndex, loopTo2 = endIndex; i3 <= loopTo2; i3 += 3)
            Debug.WriteLine(i3);
        for (MyEnum i4 = startIndex; i4 <= (MyEnum)4; i4++)
            Debug.WriteLine(i4);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForeachWithObjectCollectionAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Friend Class Program
    Public Shared Sub Main(ByVal args As String())
        Dim zs As Object = { 1, 2, 3 }
        For Each z in zs
            Console.WriteLine(z)
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;
using System.Collections;

internal partial class Program
{
    public static void Main(string[] args)
    {
        object zs = new[] { 1, 2, 3 };
        foreach (var z in (IEnumerable)zs)
            Console.WriteLine(z);
    }
}", extension: "cs")
            );
        }
    }


    [Fact]
    public async Task ForWithSingleStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod(end As Integer)
        Dim b, s As Integer()
        For i = 0 To [end]
            b(i) = s(i)
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(int end)
    {
        int[] b = default, s = default;
        for (int i = 0, loopTo = end; i <= loopTo; i++)
            b[i] = s[i];
    }
}
1 source compilation errors:
BC30183: Keyword is not valid as an identifier.", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForNextMutatingFieldAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class Class1
    Private Index As Integer

    Sub Foo()
        For Me.Index = 0 To 10

        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
public partial class Class1
{
    private int Index;

    public void Foo()
    {
        for (Index = 0; Index <= 10; Index++)
        {

        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForRequiringExtraVariableAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        Dim stringValue AS string = ""42""
        For i As Integer = 1 To 10 - stringValue.Length
           stringValue = stringValue & "" "" + Cstr(i)
           Console.WriteLine(stringValue)
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        string stringValue = ""42"";
        for (int i = 1, loopTo = 10 - stringValue.Length; i <= loopTo; i++)
        {
            stringValue = stringValue + "" "" + i.ToString();
            Console.WriteLine(stringValue);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForWithBlockAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod([end] As Integer)
        Dim b, s As Integer()
        For i = 0 To [end] - 1
            b(i) = s(i)
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"
internal partial class TestClass
{
    private void TestMethod(int end)
    {
        int[] b = default, s = default;
        for (int i = 0, loopTo = end - 1; i <= loopTo; i++)
            b[i] = s[i];
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NullInitValueForHoistedVariableIssue913Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Public Class VisualBasicClass
Private Shared Sub ProblemsWithPullingVariablesOut()
      ' example 1
      For Each a In New List(Of String)
          Dim b As Long
          If a = """" Then
              b = 1
          End If
          DoSomeImportantStuff(b)
      Next

      ' example 2
      Dim c As String
      Do While True
          Dim d As Long
          If c = """" Then
              d = 1
          End If

         DoSomeImportantStuff(d)
         Exit Do
     Loop
 End Sub

Private Shared Sub ProblemsWithPullingVariablesOut_AlwaysWriteBeforeRead()
      ' example 1
      For Each a In New List(Of String)
          Dim b As Long
          If a = """" Then
              b = 1
          End If
          DoSomeImportantStuff()
      Next

      ' example 2
      Dim c As String
      Do While True
          Dim d As Long
          If c = """" Then
              d = 1
          End If

         DoSomeImportantStuff()
         Exit Do
     Loop
 End Sub
 Private Shared Sub DoSomeImportantStuff()
     Debug.Print(""very important"")
 End Sub
 Private Shared Sub DoSomeImportantStuff(b as Long)
     Debug.Print(""very important"")
 End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System.Collections.Generic;
using System.Diagnostics;

public partial class VisualBasicClass
{
    private static void ProblemsWithPullingVariablesOut()
    {
        // example 1
        var b = default(long);
        foreach (var a in new List<string>())
        {
            if (string.IsNullOrEmpty(a))
            {
                b = 1L;
            }
            DoSomeImportantStuff(b);
        }

        // example 2
        var c = default(string);
        var d = default(long);
        while (true)
        {
            if (string.IsNullOrEmpty(c))
            {
                d = 1L;
            }

            DoSomeImportantStuff(d);
            break;
        }
    }

    private static void ProblemsWithPullingVariablesOut_AlwaysWriteBeforeRead()
    {
        // example 1
        foreach (var a in new List<string>())
        {
            long b;
            if (string.IsNullOrEmpty(a))
            {
                b = 1L;
            }
            DoSomeImportantStuff();
        }

        // example 2
        var c = default(string);
        while (true)
        {
            long d;
            if (string.IsNullOrEmpty(c))
            {
                d = 1L;
            }

            DoSomeImportantStuff();
            break;
        }
    }
    private static void DoSomeImportantStuff()
    {
        Debug.Print(""very important"");
    }
    private static void DoSomeImportantStuff(long b)
    {
        Debug.Print(""very important"");
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LabeledAndForStatementAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class GotoTest1
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
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class GotoTest1
{
    private static void Main()
    {
        int x = 200;
        int y = 4;
        int count = 0;
        string[,] array = new string[x, y];

        for (int i = 0, loopTo = x - 1; i <= loopTo; i++)
        {

            for (int j = 0, loopTo1 = y - 1; j <= loopTo1; j++)
                array[i, j] = System.Threading.Interlocked.Increment(ref count).ToString();
        }

        Console.Write(""Enter the number to search for: "");
        string myNumber = Console.ReadLine();

        for (int i = 0, loopTo2 = x - 1; i <= loopTo2; i++)
        {

            for (int j = 0, loopTo3 = y - 1; j <= loopTo3; j++)
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
        ;

        Console.WriteLine(""The number {0} is found."", myNumber);
    Finish:
        ;

        Console.WriteLine(""End of search."");
        Console.WriteLine(""Press any key to exit."");
        Console.ReadKey();
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LoopWithVariableDeclarationInitializedWithDefault_ShouldNotBePulledOutOfTheLoopAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim a As Boolean? = Nothing
            Console.WriteLine(a)
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        for (int i = 1; i <= 2; i++)
        {
            bool? a = default;
            Console.WriteLine(a);
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LoopWithMultipleVariableDeclarationsAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim a, b as Integer, c, d as Integer, e, f as Long, g = Sub() System.Console.WriteLine(1)
            a = 1
            b += 1
            
            c += 1
            d = 1

            e += 1
            f = 1

            g()
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int b = default;
        int c = default;
        long e = default;
        for (int i = 1; i <= 2; i++)
        {
            int a;
            int d;
            long f;
            void g() => Console.WriteLine(1);
            a = 1;
            b += 1;

            c += 1;
            d = 1;

            e += 1L;
            f = 1L;

            g();
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task LoopWithVariableDeclarationInitializedWithAsNewClause_ShouldNotBePulledOutOfTheLoopAsync()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim a As New Integer()
            Console.WriteLine(a)
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        for (int i = 1; i <= 2; i++)
        {
            int a = new int();
            Console.WriteLine(a);
        }
    }
}", extension: "cs")
            );
        }
    }
    
    [Fact]
    public async Task ForWithVariableDeclarationIssue897Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim b As Boolean
            Console.WriteLine(b)
            b = True
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        var b = default(bool);
        for (int i = 1; i <= 2; i++)
        {
            Console.WriteLine(b);
            b = true;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task NestedLoopsWithVariableDeclarationIssue897Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        Dim i=1
        Do
            Dim b As Integer
            b  +=1
            Console.WriteLine(""b={0}"", b)
            For j = 1 To 3
                Dim c As Integer
                c  +=1
                Console.WriteLine(""c={0}"", c)
            Next
            For j = 1 To 3
                Dim c As Integer
                c +=1
                Console.WriteLine(""c1={0}"", c)
            Next
            Dim k=1
            Do while k <= 3
                Dim c As Integer
                c +=1
                Console.WriteLine(""c2={0}"", c)
                k+=1
            Loop
        i += 1
        Loop Until i > 3
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        int i = 1;
        var b = default(int);
        var c = default(int);
        var c1 = default(int);
        var c2 = default(int);
        do
        {
            b += 1;
            Console.WriteLine(""b={0}"", b);
            for (int j = 1; j <= 3; j++)
            {
                c += 1;
                Console.WriteLine(""c={0}"", c);
            }
            for (int j = 1; j <= 3; j++)
            {
                c1 += 1;
                Console.WriteLine(""c1={0}"", c1);
            }
            int k = 1;
            while (k <= 3)
            {
                c2 += 1;
                Console.WriteLine(""c2={0}"", c2);
                k += 1;
            }
            i += 1;
        }
        while (i <= 3);
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForWithVariableDeclarationIssue1000Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim b As Boolean
            Console.WriteLine(b)
            b = True
        Next
        For i = 1 To 2
            Dim b As Boolean
            Console.WriteLine(b)
            b = True
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod()
    {
        var b = default(bool);
        for (int i = 1; i <= 2; i++)
        {
            Console.WriteLine(b);
            b = true;
        }
        var b1 = default(bool);
        for (int i = 1; i <= 2; i++)
        {
            Console.WriteLine(b1);
            b1 = true;
        }
    }
}", extension: "cs")
            );
        }
    }

    [Fact]
    public async Task ForWithVariableDeclarationIssue998Async()
    {
        {
            await Task.WhenAll(
                Verifier.Verify(@"Class TestClass
    Private Sub TestMethod(someCondition As Boolean)
        For j = 1 To 2
            If someCondition Then
                Dim b As Boolean
                Console.WriteLine(b)
                b = True
            End If
        Next
    End Sub
End Class", extension: "vb"),
                Verifier.Verify(@"using System;

internal partial class TestClass
{
    private void TestMethod(bool someCondition)
    {
        var b = default(bool);
        for (int j = 1; j <= 2; j++)
        {
            if (someCondition)
            {
                Console.WriteLine(b);
                b = true;
            }
        }
    }
}", extension: "cs")
            );
        }
    }
}