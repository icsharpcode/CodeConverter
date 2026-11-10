using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.Tests.TestRunners;
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
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
    public async Task NullableComparisonInWhileConditionIssue1300Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System

Class TestClass
    Sub TestMethod(startDate As Date?, endDate As Date?)
        Dim currentDate = startDate
        While currentDate <= endDate
            currentDate = currentDate.Value.AddMinutes(1)
        End While
        While (currentDate <= endDate)
            currentDate = currentDate.Value.AddMinutes(1)
        End While
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void TestMethod(DateTime? startDate, DateTime? endDate)
    {
        var currentDate = startDate;
        while (currentDate <= endDate)
            currentDate = currentDate.Value.AddMinutes(1d);
        while (currentDate <= endDate)
            currentDate = currentDate.Value.AddMinutes(1d);
    }
}");
    }

    [Fact]
    public async Task NullableLoopConditionsTreatNothingAsFalseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(x As Integer?, y As Integer?, b As Boolean?)
        While x = y : End While
        While b : End While
        While Not b : End While
        Do While x <> y : Loop
        Do While b AndAlso x > y : Loop
        Do Until x < y : Loop
        Do Until x = y : Loop
        Do Until b : Loop
        Do Until b OrElse x > 2 : Loop
        Do : Loop While x >= y
        Do : Loop While b
        Do : Loop Until x > y
        Do : Loop Until Not b
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int? x, int? y, bool? b)
    {
        while (x.HasValue && y.HasValue && x.Value == y.Value)
        {
        }
        while (b == true)
        {
        }
        while (b == false)
        {
        }
        while (x.HasValue && y.HasValue && x.Value != y.Value)
        {
        }
        while ((!b.HasValue || b.Value) && x > y && b.HasValue)
        {
        }
        while (!(x < y))
        {
        }
        while (!(x.HasValue && y.HasValue && x.Value == y.Value))
        {
        }
        while (b != true)
        {
        }
        while (!(b.GetValueOrDefault() || x > 2))
        {
        }
        do
        {
        }
        while (x >= y);
        do
        {
        }
        while (b == true);
        do
        {
        }
        while (!(x > y));
        do
        {
        }
        while (!(b == false));
    }
}");
    }

    [Fact]
    public async Task OptionStrictOffNonBooleanConditionsTreatNothingAsFalseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Option Strict Off

Class TestClass
    Private Sub TestMethod(x As Integer?, o As Object)
        If x Then Return
        While x : End While
        Do Until x : Loop
        While o : End While
        Do Until o : Loop
        Do Until o > 1 : Loop
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private void TestMethod(int? x, object o)
    {
        if (Conversions.ToBoolean(x.GetValueOrDefault()))
            return;
        while (Conversions.ToBoolean(x.GetValueOrDefault()))
        {
        }
        while (!Conversions.ToBoolean(x.GetValueOrDefault()))
        {
        }
        while (Conversions.ToBoolean(o))
        {
        }
        while (!Conversions.ToBoolean(o))
        {
        }
        while (!Conversions.ToBoolean(Operators.ConditionalCompareObjectGreater(o, 1, false)))
        {
        }
    }
}", incompatibleWithAutomatedCommentTesting: true); // Option statement comments are lost
    }

    [Fact]
    public async Task NullableStructureWithWideningToBooleanConditionsTreatNothingAsFalseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Structure S
    Public Shared Widening Operator CType(s As S) As Boolean
        Return True
    End Operator
End Structure

Class TestClass
    Private Function GetS() As S?
        Return Nothing
    End Function

    Private Sub TestMethod(s As S?)
        If s Then Return
        While (s) : End While
        Do Until s : Loop
        Do : Loop While GetS()
        If If(s, GetS()) Then Return
        Do Until If(s.HasValue, s, GetS()) : Loop
    End Sub
End Class", @"
internal partial struct S
{
    public static implicit operator bool(S s)
    {
        return true;
    }
}

internal partial class TestClass
{
    private S? GetS()
    {
        return default;
    }

    private void TestMethod(S? s)
    {
        if (s.HasValue && (bool)s.Value)
            return;
        while (s.HasValue && (bool)s.Value)
        {
        }
        while (!s.HasValue || !(bool)s.Value)
        {
        }
        do
        {
        }
        while (GetS() is { } arg1 && (bool)arg1);
        if ((s ?? GetS()) is { } arg2 && (bool)arg2)
            return;
        while (!((s.HasValue ? s : GetS()) is { } arg3) || !(bool)arg3)
        {
        }
    }
}");
    }

    // Deliberately pins master's known imperfect output: VB doesn't lift a CType returning Boolean? (a Nothing operand throws), so it's left alone
    [Fact]
    public async Task NullableStructureWithWideningToNullableBooleanConditionsAreNotLiftedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Structure S
    Public Shared Widening Operator CType(s As S) As Boolean?
        Return Nothing
    End Operator
End Structure

Class TestClass
    Private Sub TestMethod(s As S?)
        If s Then Return
        Do Until s : Loop
    End Sub
End Class", @"
internal partial struct S
{
    public static implicit operator bool?(S s)
    {
        return default;
    }
}

internal partial class TestClass
{
    private void TestMethod(S? s)
    {
        if ((bool)s)
            return;
        while (!(bool)s)
        {
        }
    }
}");
    }

    [Fact]
    public async Task OptionStrictOffNullableToBooleanOutsideConditionsKeepsValueAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Option Strict Off

Structure S
    Public Shared Widening Operator CType(s As S) As Boolean
        Return True
    End Operator
End Structure

Class TestClass
    Private Function IsTrue(b As Boolean) As Boolean
        Return b
    End Function

    Private Sub TestMethod(x As Integer?, s As S?)
        Dim b As Boolean = x
        If IsTrue(x) Then Return
        If CBool(x) Then Return
        Do Until s : Loop
    End Sub
End Class", @"using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial struct S
{
    public static implicit operator bool(S s)
    {
        return true;
    }
}

internal partial class TestClass
{
    private bool IsTrue(bool b)
    {
        return b;
    }

    private void TestMethod(int? x, S? s)
    {
        bool b = Conversions.ToBoolean(x.Value);
        if (IsTrue(Conversions.ToBoolean(x.Value)))
            return;
        if (Conversions.ToBoolean(x.Value))
            return;
        while (!s.HasValue || !(bool)s.Value)
        {
        }
    }
}", incompatibleWithAutomatedCommentTesting: true); // Option statement comments are lost
    }

    [Fact]
    public async Task NonNullableUntilConditionsAreInvertedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(i As Integer, b As Boolean)
        Do Until i < 1 : Loop
        Do Until Not b : Loop
        Do : Loop Until i = 1 OrElse b
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int i, bool b)
    {
        while (i >= 1)
        {
        }
        while (b)
        {
        }
        do
        {
        }
        while (i != 1 && !b);
    }
}");
    }

    [Fact]
    public async Task TwoForEachStatementsWithImplicitVariableCreationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class Program
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
    public async Task Int16ForLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"    Sub DummyMethod()
        Dim someArray = New Integer() { 1, 2, 3}
        For index As Int16 = 0 To someArray.Length - 1
            Console.WriteLine(index)
        Next
    End Sub", @"public void DummyMethod()
{
    int[] someArray = new int[] { 1, 2, 3 };
    for (short index = 0, loopTo = (short)(someArray.Length - 1); index <= loopTo; index++)
        Console.WriteLine(index);
}");
    }

    [Fact]
    public async Task ExternallyDeclaredLoopVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Sub Main()
    Dim foo As Single = 3.5
    Dim index As Integer
    For index = Int(foo) To Int(foo * 3)
        Console.WriteLine(index)
    Next
End Sub", @"public void Main()
{
    float foo = 3.5f;
    int index;
    var loopTo = (int)Math.Round(Conversion.Int(foo * 3f));
    for (index = (int)Math.Round(Conversion.Int(foo)); index <= loopTo; index++)
        Console.WriteLine(index);
}");
    }

    [Fact]
    public async Task ForNonNegativeStepAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class Issue453
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
    public async Task ForNegativeStepAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class Issue453
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
    public async Task ForVariableStepAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class Issue453
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
    public async Task ForEnumAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Enum MyEnum
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
End Class", @"using System.Diagnostics;

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
}");
    }

    [Fact]
    public async Task ForeachWithObjectCollectionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Friend Class Program
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
    public async Task ForWithSingleStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
    public async Task ForNextMutatingFieldAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class Class1
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
    public async Task ForRequiringExtraVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        Dim stringValue AS string = ""42""
        For i As Integer = 1 To 10 - stringValue.Length
           stringValue = stringValue & "" "" + Cstr(i)
           Console.WriteLine(stringValue)
        Next
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task ForWithBlockAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
    public async Task NullInitValueForHoistedVariableIssue913Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Public Class VisualBasicClass
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
End Class", @"using System.Collections.Generic;
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
}");
    }

    [Fact]
    public async Task LabeledAndForStatementAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class GotoTest1
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
}");
    }

    [Fact]
    public async Task LoopWithVariableDeclarationInitializedWithDefault_ShouldNotBePulledOutOfTheLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim a As Boolean? = Nothing
            Console.WriteLine(a)
        Next
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task LoopWithMultipleVariableDeclarationsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task LoopWithVariableDeclarationInitializedWithAsNewClause_ShouldNotBePulledOutOfTheLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim a As New Integer()
            Console.WriteLine(a)
        Next
    End Sub
End Class", @"using System;

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
}");
    }
    
    [Fact]
    public async Task ForWithVariableDeclarationIssue897Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod()
        For i = 1 To 2
            Dim b As Boolean
            Console.WriteLine(b)
            b = True
        Next
    End Sub
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task NestedLoopsWithVariableDeclarationIssue897Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task ForWithVariableDeclarationIssue1000Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
End Class", @"using System;

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
}");
    }

    [Fact]
    public async Task ForWithVariableDeclarationIssue998Async()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(someCondition As Boolean)
        For j = 1 To 2
            If someCondition Then
                Dim b As Boolean
                Console.WriteLine(b)
                b = True
            End If
        Next
    End Sub
End Class", @"using System;

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
}");
    }
}