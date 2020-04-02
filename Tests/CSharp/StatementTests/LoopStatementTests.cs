using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests
{
    public class LoopStatementTests : ConverterTestBase
    {
        [Fact]
        public async Task WhileStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        While b = 0
            If b = 2 Then Continue While
            If b = 3 Then Exit While
            b = 1
        End While
    End Sub
End Class", @"
internal partial class TestClass
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
        public async Task UntilStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(rand As Random)
        Dim charIndex As Integer
        ' allow only digits and letters
        Do
            charIndex = rand.Next(48, 123)
        Loop Until (charIndex >= 48 AndAlso charIndex <= 57) OrElse (charIndex >= 65 AndAlso charIndex <= 90) OrElse (charIndex >= 97 AndAlso charIndex <= 122)
    End Sub
End Class", @"using System;

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
}");
        }

        [Fact]
        public async Task SimpleDoStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop
    End Sub
End Class", @"
internal partial class TestClass
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
        public async Task DoWhileStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim b As Integer
        b = 0

        Do
            If b = 2 Then Continue Do
            If b = 3 Then Exit Do
            b = 1
        Loop While b = 0
    End Sub
End Class", @"
internal partial class TestClass
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
        public async Task ForEachStatementWithExplicitType()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val As Integer In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
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
        public async Task ForEachStatementWithVar()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
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
}
1 source compilation errors:
BC30516: Overload resolution failed because no accessible 'Val' accepts this number of arguments.");
        }

        [Fact]
        public async Task TwoForEachStatementsWithImplicitVariableCreation()
        {
            await TestConversionVisualBasicToCSharp(@"Friend Class Program
    Public Shared Sub Main(ByVal args As String())
        For idx = 0 To 10
        Next

        For idx = 0 To 10
        Next
    End Sub
End Class", @"
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
}");
        }

        [Fact]
        public async Task ForNonNegativeStep()
        {
            await TestConversionVisualBasicToCSharp(@"Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer)
      For i As Integer = startIndex To endIndex Step -0
        Debug.WriteLine(i)
  Next
End Sub
End Class", @"using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex)
    {
        for (int i = startIndex, loopTo = endIndex; i <= loopTo; i += -0)
            Debug.WriteLine(i);
    }
}");
        }

        [Fact]
        public async Task ForNegativeStep()
        {
            await TestConversionVisualBasicToCSharp(@"Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer)
      For i As Integer = startIndex To endIndex Step -5
        Debug.WriteLine(i)
  Next
End Sub
End Class", @"using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex)
    {
        for (int i = startIndex, loopTo = endIndex; i >= loopTo; i -= 5)
            Debug.WriteLine(i);
    }
}");
        }

        [Fact]
        public async Task ForVariableStep()
        {
            await TestConversionVisualBasicToCSharp(@"Friend Class Issue453
    Sub PrintLoop(startIndex As Integer, endIndex As Integer, [step] As Integer)
      For i As Integer = startIndex To endIndex Step [step]
        Debug.WriteLine(i)
  Next
End Sub
End Class", @"using System.Diagnostics;

internal partial class Issue453
{
    public void PrintLoop(int startIndex, int endIndex, int step)
    {
        for (int i = startIndex, loopTo = endIndex; step >= 0 ? i <= loopTo : i >= loopTo; i += step)
            Debug.WriteLine(i);
    }
}");
        }

        [Fact]
        public async Task ForeachWithObjectCollection()
        {
            await TestConversionVisualBasicToCSharp(@"Friend Class Program
    Public Shared Sub Main(ByVal args As String())
        Dim zs As Object = { 1, 2, 3 }
        For Each z in zs
            Console.WriteLine(z)
        Next
    End Sub
End Class", @"using System;
using System.Collections;

internal partial class Program
{
    public static void Main(string[] args)
    {
        object zs = new[] { 1, 2, 3 };
        foreach (var z in (IEnumerable)zs)
            Console.WriteLine(z);
    }
}");
        }


        [Fact]
        public async Task ForWithSingleStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod(end As Integer)
        Dim b, s As Integer()
        For i = 0 To [end]
            b(i) = s(i)
        Next
    End Sub
End Class", @"
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
BC30183: Keyword is not valid as an identifier.");
        }

        [Fact]
        public async Task ForNextMutatingField()
        {
            await TestConversionVisualBasicToCSharp(@"Public Class Class1
    Private Index As Integer

    Sub Foo()
        For Me.Index = 0 To 10

        Next
    End Sub
End Class", @"
public partial class Class1
{
    private int Index;

    public void Foo()
    {
        for (Index = 0; Index <= 10; Index++)
        {
        }
    }
}");
        }

        [Fact]
        public async Task ForRequiringExtraVariable()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod()
        Dim stringValue AS string = ""42""
        For i As Integer = 1 To 10 - stringValue.Length
           stringValue = stringValue & "" "" + Cstr(i)
           Console.WriteLine(stringValue)
        Next
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic.CompilerServices;

internal partial class TestClass
{
    private void TestMethod()
    {
        string stringValue = ""42"";
        for (int i = 1, loopTo = 10 - stringValue.Length; i <= loopTo; i++)
        {
            stringValue = stringValue + "" "" + Conversions.ToString(i);
            Console.WriteLine(stringValue);
        }
    }
}");
        }

        [Fact]
        public async Task ForWithBlock()
        {
            await TestConversionVisualBasicToCSharp(@"Class TestClass
    Private Sub TestMethod([end] As Integer)
        Dim b, s As Integer()
        For i = 0 To [end] - 1
            b(i) = s(i)
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int end)
    {
        int[] b = default, s = default;
        for (int i = 0, loopTo = end - 1; i <= loopTo; i++)
            b[i] = s[i];
    }
}");
        }

        [Fact]
        public async Task LabeledAndForStatement()
        {
            await TestConversionVisualBasicToCSharp(@"Class GotoTest1
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

internal partial class GotoTest1
{
    private static void Main()
    {
        int x = 200;
        int y = 4;
        int count = 0;
        var array = new string[x, y];
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
}");
        }
    }
}
