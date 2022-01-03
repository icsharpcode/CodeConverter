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

        [Fact]
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
            switch (CurVal)
            {
                case 6:
                    {
                        ;
#error Cannot convert ExitStatementSyntax - see comment for details
                        /* Cannot convert ExitStatementSyntax, System.InvalidOperationException: Cannot convert exit For to break since it would break only from the containing Select
                           at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitExitStatement>d__52.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\MethodBodyExecutableStatementVisitor.cs:line 446
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
                           at ICSharpCode.CodeConverter.CSharp.PerScopeStateVisitorDecorator.<AddLocalVariablesAsync>d__6.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\PerScopeStateVisitorDecorator.cs:line 47
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisitInnerAsync>d__3.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\CommentConvertingMethodBodyVisitor.cs:line 29

                        Input:
                                            Exit For

                         */
                        break;
                    }
            }
        }

        Console.WriteLine(i_Total.ToString());
    }
}
2 target compilation errors:
CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
CS1029: #error: 'Cannot convert ExitStatementSyntax - see comment for details'",
                hasLineCommentConversionIssue: true);
        }

        [Fact]
        public async Task ExitTry_CreatesCompileErrorCharacterization_Issue779Async()
        {
            await TestConversionVisualBasicToCSharpAsync(@"Imports System

Public Class VisualBasicClass779
    Public Property SomeCase As Integer = 1
    Public Property ComboBox_CostCenter As Object
    Public Property The_Cost_Center As Object

    Public Sub Test
        Try
            If Not To_Show_Cost() Then
                SomeCase *= 2
            End If

            SomeCase *= 3
                
            If The_Cost_Center = 0 Then
                    SomeCase *=5
                Exit Try
            End If

            ComboBox_CostCenter.SelectedIndex = -1
            For i = 0 To ComboBox_CostCenter.Items.Count - 1
                If ComboBox_CostCenter.Items(i).item(0) = The_Cost_Center Then
                    SomeCase *=7
                    Exit Try
                End If
            Next
        Finally
        End Try
    End Sub

    Private Function To_Show_Cost() As Boolean
        Throw New NotImplementedException()
    End Function
End Class", @"using System;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

public partial class VisualBasicClass779
{
    public int SomeCase { get; set; } = 1;
    public object ComboBox_CostCenter { get; set; }
    public object The_Cost_Center { get; set; }

    public void Test()
    {
        try
        {
            if (!To_Show_Cost())
            {
                SomeCase *= 2;
            }

            SomeCase *= 3;
            if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(The_Cost_Center, 0, false)))
            {
                SomeCase *= 5;
                ;
#error Cannot convert ExitStatementSyntax - see comment for details
                /* Cannot convert ExitStatementSyntax, System.InvalidOperationException: Cannot convert exit Try since no C# equivalent exists
                   at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitExitStatement>d__52.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\MethodBodyExecutableStatementVisitor.cs:line 441
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
                   at ICSharpCode.CodeConverter.CSharp.PerScopeStateVisitorDecorator.<AddLocalVariablesAsync>d__6.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\PerScopeStateVisitorDecorator.cs:line 47
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisitInnerAsync>d__3.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\CommentConvertingMethodBodyVisitor.cs:line 29

                Input:
                                Exit Try

                 */
            }

            ComboBox_CostCenter.SelectedIndex = (object)-1;
            for (int i = 0, loopTo = Conversions.ToInteger(Operators.SubtractObject(ComboBox_CostCenter.Items.Count, 1)); i <= loopTo; i++)
            {
                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(ComboBox_CostCenter.Items(i).item((object)0), The_Cost_Center, false)))
                {
                    SomeCase *= 7;
                    ;
#error Cannot convert ExitStatementSyntax - see comment for details
                    /* Cannot convert ExitStatementSyntax, System.InvalidOperationException: Cannot convert exit Try since no C# equivalent exists
                       at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitExitStatement>d__52.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\MethodBodyExecutableStatementVisitor.cs:line 441
                    --- End of stack trace from previous location where exception was thrown ---
                       at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
                       at ICSharpCode.CodeConverter.CSharp.PerScopeStateVisitorDecorator.<AddLocalVariablesAsync>d__6.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\PerScopeStateVisitorDecorator.cs:line 47
                    --- End of stack trace from previous location where exception was thrown ---
                       at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()
                       at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisitInnerAsync>d__3.MoveNext() in C:\Users\gph77\source\repos\CodeConverter\CodeConverter\CSharp\CommentConvertingMethodBodyVisitor.cs:line 29

                    Input:
                                        Exit Try

                     */
                }
            }
        }
        finally
        {
        }
    }

    private bool To_Show_Cost()
    {
        throw new NotImplementedException();
    }
}
4 target compilation errors:
CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
CS1029: #error: 'Cannot convert ExitStatementSyntax - see comment for details'
CS1061: 'object' does not contain a definition for 'SelectedIndex' and no accessible extension method 'SelectedIndex' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
CS1061: 'object' does not contain a definition for 'Items' and no accessible extension method 'Items' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)", hasLineCommentConversionIssue: true);
        }
    }
}
