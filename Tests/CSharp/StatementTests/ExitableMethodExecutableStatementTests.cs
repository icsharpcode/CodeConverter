using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests
{
    /// <summary>
    /// Covers:
    /// Exit { Do | For | Select | Try | While } https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/exit-statement
    /// Continue { Do | For | While } https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/statements/continue-statement
    ///
    /// Does not cover:
    /// Exit { Function | Property | Sub } since they are not MethodExecutableStatements
    /// </summary>
    public class ExitableMethodExecutableStatementTests : ConverterTestBase
    {
        [Fact]
        public async Task WhileStatementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
        public async Task SimpleDoStatementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
        public async Task DoWhileStatementAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
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
        public async Task ForEachStatementWithExplicitTypeAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each v As Integer In values
            If v = 2 Then Continue For
            If v = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (int v in values)
        {
            if (v == 2)
                continue;
            if (v == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithVarAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        For Each v In values
            If v = 2 Then Continue For
            If v = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        foreach (var v in values)
        {
            if (v == 2)
                continue;
            if (v == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithUsedOuterDeclarationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim val As Integer
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next

        Console.WriteLine(val)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        var val = default(int);
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }

        Console.WriteLine(val);
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithFieldVarUsedOuterDeclarationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Dim val As Integer

    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next

        Console.WriteLine(val)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int val;

    private void TestMethod(int[] values)
    {
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }

        Console.WriteLine(val);
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithUnusedOuterDeclarationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim val As Integer
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
}");
        }

        [Fact]
        public async Task ForEachStatementWithFieldVarUnusedOuterDeclarationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Dim val As Integer
    Private Sub TestMethod(ByVal values As Integer())
        For Each val In values
            If val = 2 Then Continue For
            If val = 3 Then Exit For
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    private int val;

    private void TestMethod(int[] values)
    {
        foreach (var currentVal in values)
        {
            val = currentVal;
            if (val == 2)
                continue;
            if (val == 3)
                break;
        }
    }
}");
        }

        [Fact]
        public async Task ForEachStatementWithUnusedNestedDeclarationAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub TestMethod(ByVal values As Integer())
        Dim inline1, inline2, keep1, keep2 As Integer
        For Each inline1 In values
            For Each keep1 In values
                For Each inline2 In values
                    If inline2 = 2 Then Continue For
                    If inline2 = 3 Then Exit For
                Next
            Next
            Console.WriteLine(keep1)
        Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void TestMethod(int[] values)
    {
        int keep1 = default, keep2;
        foreach (var inline1 in values)
        {
            foreach (var currentKeep1 in values)
            {
                keep1 = currentKeep1;
                foreach (var inline2 in values)
                {
                    if (inline2 == 2)
                        continue;
                    if (inline2 == 3)
                        break;
                }
            }

            Console.WriteLine(keep1);
        }
    }
}");
        }

        [Fact]
        public async Task SelectCaseWithExplicitExitAsync()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Class A
    Public Function Add(ByVal x As Integer) As Integer
        Select Case x
            Case 1
                Exit Select
        End Select
        Return 3
    End Function
End Class", @"
internal partial class A
{
    public int Add(int x)
    {
        switch (x)
        {
            case 1:
                {
                    break;
                }
        }

        return 3;
    }
}");
        }

        [Fact()]
        public async Task MultipleBreakable_CreatesCompileErrorCharacterization_Issue690Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic

Public Class VisualBasicClass
    Public Sub Test
        Dim LstTmp As New List(Of Integer)
        LstTmp.Add(5)
        LstTmp.Add(6)
        LstTmp.Add(7)
        Dim i_Total As Integer
        For Each CurVal As Integer In LstTmp
            i_Total += CurVal
            Select Case CurVal
                Case 6
                    Exit For
            End Select
        Next
    system.Console.WriteLine(i_Total.ToString())
    End Sub
End Class", @"using System;
using System.Collections.Generic;

public partial class VisualBasicClass
{
    public void Test()
    {
        var LstTmp = new List<int>();
        LstTmp.Add(5);
        LstTmp.Add(6);
        LstTmp.Add(7);
        var i_Total = default(int);
        foreach (int CurVal in LstTmp)
        {
            i_Total += CurVal;
            bool exitSelect = false;
            switch (CurVal)
            {
                case 6:
                    {
                        exitSelect = true;
                        break;
                    }
            }

            if (exitSelect)
            {
                break;
            }
        }

        Console.WriteLine(i_Total.ToString());
    }
}");
        }
    }
}
