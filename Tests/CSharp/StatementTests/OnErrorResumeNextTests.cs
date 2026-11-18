using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// README.md "Resume next" and "Dynamic" shapes: each statement that can fail is guarded by its own try/catch.
/// </summary>
public class OnErrorResumeNextTests : ConverterTestBase
{
    [Fact]
    public async Task EachStatementThatCanFailIsGuardedAndDeclarationsAreHoistedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error Resume Next
        Console.WriteLine(""a"")
        Dim x As Integer = Integer.Parse(""z"")
        Console.WriteLine(x)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run()
    {
        try
        {
            Console.WriteLine(""a"");
        }
        catch
        {
        }
        int x = default;
        try
        {
            x = int.Parse(""z"");
        }
        catch
        {
        }
        try
        {
            Console.WriteLine(x);
        }
        catch
        {
        }
    }
}");
    }

    [Fact]
    public async Task ErrIsRecordedOnlyWhileErrorsAreIgnoredAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error Resume Next
        Console.WriteLine(Integer.Parse(""z""))
        Console.WriteLine(Err.Number)
        On Error GoTo 0
        Console.WriteLine(""unguarded"")
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Run()
    {
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(int.Parse(""z""));
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
        }
        try
        {
            Console.WriteLine(Information.Err().Number);
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
        }
        ProjectData.ClearProjectError();
        Console.WriteLine(""unguarded"");
    }
}");
    }

    [Fact]
    public async Task StatementsThatCannotFailAreNotGuardedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Run(flag As Boolean) As Integer
        Dim count As Integer = 0
        On Error Resume Next
        Dim limit As Integer = 3
        count = 1
        If flag Then GoTo Done
        count = count + 1
        Console.WriteLine(count)
Done:
        Exit Function
    End Function
End Class", @"using System;

internal partial class TestClass
{
    public int Run(bool flag)
    {
        int count = 0;
        int limit = 3;
        count = 1;
        if (flag)
            goto Done;
        count = count + 1;
        try
        {
            Console.WriteLine(count);
        }
        catch
        {
        }

    Done:
        ;

        return default;
    }
}");
    }

    [Fact]
    public async Task EachDeclaratorIsItsOwnResumePointAndReturnsStillReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function F(s As String) As Integer
        On Error Resume Next
        Dim a As Integer = Integer.Parse(s), b As Integer = 2, c As New System.Text.StringBuilder()
        If b = 2 Then
            Dim inner As Integer = Integer.Parse(""q"")
            Return inner + a
        End If
        Return b
    End Function
End Class", @"using System.Text;

internal partial class TestClass
{
    public int F(string s)
    {
        int a = default;
        StringBuilder c = default;
        try
        {
            a = int.Parse(s);
        }
        catch
        {
        }

        int b = 2;
        try
        {
            c = new StringBuilder();
        }
        catch
        {
        }
        if (b == 2)
        {
            int inner = default;
            try
            {
                inner = int.Parse(""q"");
            }
            catch
            {
            }
            try
            {
                return inner + a;
            }
            catch
            {
            }
        }
        return b;
    }
}");
    }

    [Fact]
    public async Task IfAndElseIfConditionsThatCanFailAreEvaluatedInAGuardAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run(flag As Boolean, n As Integer)
        On Error Resume Next
        If flag Then
            Console.WriteLine(""safe condition"")
        End If
        If Integer.Parse(""1"") = 1 Then
            Console.WriteLine(""a"")
        ElseIf n > 3 Then
            Console.WriteLine(""b"")
        ElseIf Integer.Parse(""2"") = 2 Then
            Console.WriteLine(""c"")
        Else
            Console.WriteLine(""d"")
        End If
        If Integer.Parse(""3"") = 3 Then Console.WriteLine(""single"") Else Console.WriteLine(""other"")
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run(bool flag, int n)
    {
        if (flag)
        {
            try
            {
                Console.WriteLine(""safe condition"");
            }
            catch
            {
            }
        }
        bool condition = true;
        try
        {
            condition = int.Parse(""1"") == 1;
        }
        catch
        {
        }

        if (condition)
        {
            try
            {
                Console.WriteLine(""a"");
            }
            catch
            {
            }
        }
        else if (n > 3)
        {
            try
            {
                Console.WriteLine(""b"");
            }
            catch
            {
            }
        }
        else
        {
            bool condition1 = true;
            try
            {
                condition1 = int.Parse(""2"") == 2;
            }
            catch
            {
            }

            if (condition1)
            {
                try
                {
                    Console.WriteLine(""c"");
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    Console.WriteLine(""d"");
                }
                catch
                {
                }
            }
        }
        bool condition2 = true;
        try
        {
            condition2 = int.Parse(""3"") == 3;
        }
        catch
        {
        }

        if (condition2)
        {
            try
            {
                Console.WriteLine(""single"");
            }
            catch
            {
            }
        }
        else
        {
            try
            {
                Console.WriteLine(""other"");
            }
            catch
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task LoopConditionsThatCanFailMoveToALocalFunctionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Class TestClass
    Sub Run(items As List(Of Integer))
        On Error Resume Next
        Dim n As Integer = 0
        While items.Count > n
            n += 1
        End While
        Do While items.Count > n
            n += 1
        Loop
        Do Until items.Count = n
            n += 1
        Loop
        Do
            n += 1
        Loop While items.Count > n
        Do
            n += 1
        Loop Until items.Count = n
        While n < 5
            n += 1
        End While
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    public void Run(List<int> items)
    {
        int n = 0;
        bool LoopCondition() { try { return items.Count > n; } catch { return true; } }

        while (LoopCondition())
            n += 1;
        bool LoopCondition1() { try { return items.Count > n; } catch { return true; } }

        while (LoopCondition1())
            n += 1;
        bool LoopCondition2() { try { return items.Count != n; } catch { return true; } }

        while (LoopCondition2())
            n += 1;
        bool LoopCondition3() { try { return items.Count > n; } catch { return false; } }

        do
            n += 1;
        while (LoopCondition3());
        bool LoopCondition4() { try { return items.Count != n; } catch { return false; } }

        do
            n += 1;
        while (LoopCondition4());
        while (n < 5)
            n += 1;
    }
}");
    }

    [Fact]
    public async Task LoopConditionCapturingAByRefParameterKeepsPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System.Collections.Generic
Class TestClass
    Sub Run(ByRef n As Integer, items As List(Of Integer))
        On Error Resume Next
        While items.Count > n
            n += 1
        End While
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    public void Run(ref int n, List<int> items)
    {
        ;
#error Cannot convert OnErrorResumeNextStatementSyntax - see comment for details
        /* Cannot convert OnErrorResumeNextStatementSyntax, CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 162


                Input:
                        On Error Resume Next

                 */
        while (items.Count > n)
            n += 1;
    }
}CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 162", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task UsingAndSyncLockAreOneResumePointAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.IO
Class TestClass
    Sub Run(path As String, gate As Object)
        On Error Resume Next
        Using reader As New StreamReader(path)
            Console.WriteLine(reader.ReadToEnd())
            Console.WriteLine(""done"")
        End Using
        SyncLock gate
            Console.WriteLine(""locked"")
        End SyncLock
    End Sub
End Class", @"using System;
using System.IO;

internal partial class TestClass
{
    public void Run(string path, object gate)
    {
        try
        {
            using (var reader = new StreamReader(path))
            {
                Console.WriteLine(reader.ReadToEnd());
                Console.WriteLine(""done"");
            }
        }
        catch
        {
        }
        try
        {
            lock (gate)
                Console.WriteLine(""locked"");
        }
        catch
        {
        }
    }
}");
    }

    [Fact]
    public async Task BlocksWithAFailingHeaderAreGuardedAndTheirStatementsAreGuardedTooAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.Collections.Generic
Class TestClass
    Sub Run(items As List(Of Integer), n As Integer, text As String)
        On Error Resume Next
        Select Case n
            Case 1
                Console.WriteLine(""one"")
            Case Else
                Console.WriteLine(""other"")
        End Select
        Select Case text.Length
            Case 2
                Console.WriteLine(""two"")
        End Select
        For i As Integer = 1 To n
            Console.WriteLine(i)
        Next
        For i As Integer = 1 To items.Count
            Console.WriteLine(i)
        Next
        For Each item In items
            Console.WriteLine(item)
        Next
        With items
            .Add(1)
        End With
    End Sub
End Class", @"using System;
using System.Collections.Generic;

internal partial class TestClass
{
    public void Run(List<int> items, int n, string text)
    {
        switch (n)
        {
            case 1:
                try
                {
                    Console.WriteLine(""one"");
                }
                catch
                {
                }

                break;
            default:
                try
                {
                    Console.WriteLine(""other"");
                }
                catch
                {
                }

                break;
        }
        try
        {
            switch (text.Length)
            {
                case 2:
                    try
                    {
                        Console.WriteLine(""two"");
                    }
                    catch
                    {
                    }

                    break;
            }
        }
        catch
        {
        }
        for (int i = 1, loopTo = n; i <= loopTo; i++)
        {
            try
            {
                Console.WriteLine(i);
            }
            catch
            {
            }
        }
        try
        {
            for (int i = 1, loopTo1 = items.Count; i <= loopTo1; i++)
            {
                try
                {
                    Console.WriteLine(i);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        try
        {
            foreach (var item in items)
            {
                try
                {
                    Console.WriteLine(item);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        try
        {
            items.Add(1);
        }
        catch
        {
        }
    }
}");
    }

    [Fact]
    public async Task HandlerLabelAlongsideResumeNextKeepsPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error Resume Next
        Console.WriteLine(""a"")
        On Error GoTo Failed
        Console.WriteLine(""b"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run()
    {
        ;
#error Cannot convert OnErrorResumeNextStatementSyntax - see comment for details
        /* Cannot convert OnErrorResumeNextStatementSyntax, CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 63


                Input:
                        On Error Resume Next

                 */
        Console.WriteLine(""a"");
        try
        {
            Console.WriteLine(""b"");
            return;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }
    }
}CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 63", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task AssigningTheFunctionNameIsGuardedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function Compute(s As String) As Integer
        On Error Resume Next
        Compute = Integer.Parse(s)
        Compute += 1
    End Function
End Class", @"
internal partial class TestClass
{
    public int Compute(string s)
    {
        int ComputeRet = default;
        try
        {
            ComputeRet = int.Parse(s);
        }
        catch
        {
        }
        ComputeRet += 1;
        return ComputeRet;
    }
}");
    }

    [Fact]
    public async Task DynamicHandlerUsesAFlagFilterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run(flag As Boolean)
        If flag Then On Error Resume Next
        Console.WriteLine(""a"")
        Dim n As Integer = Integer.Parse(""x"")
        If flag Then On Error GoTo 0
        Console.WriteLine(n)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run(bool flag)
    {
        bool resumeNext = false;
        if (flag)
            resumeNext = true;
        try
        {
            Console.WriteLine(""a"");
        }
        catch when (resumeNext)
        {
        }
        int n = default;
        try
        {
            n = int.Parse(""x"");
        }
        catch when (resumeNext)
        {
        }
        if (flag)
            resumeNext = false;
        try
        {
            Console.WriteLine(n);
        }
        catch when (resumeNext)
        {
        }
    }
}");
    }

    [Fact]
    public async Task DynamicHandlerRecordsErrAndGuardsConditionsAndLoopsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function Run(flag As Boolean) As Integer
        If flag Then On Error Resume Next
        Dim n As Integer = Integer.Parse(""x"")
        If Err.Number <> 0 Then Return -1
        While n > Integer.Parse(""2"")
            n -= 1
        End While
        Return n
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Run(bool flag)
    {
        bool resumeNext = false;
        if (flag)
        {
            resumeNext = true;
            ProjectData.ClearProjectError();
        }
        int n = default;
        try
        {
            n = int.Parse(""x"");
        }
        catch (Exception ex) when (resumeNext)
        {
            ProjectData.SetProjectError(ex);
        }
        bool condition = true;
        try
        {
            condition = Information.Err().Number != 0;
        }
        catch (Exception ex) when (resumeNext)
        {
            ProjectData.SetProjectError(ex);
        }

        if (condition)
            return -1;
        bool LoopCondition() { try { return n > int.Parse(""2""); } catch (Exception ex) when (resumeNext) { ProjectData.SetProjectError(ex); return true; } }

        while (LoopCondition())
            n -= 1;
        return n;
    }
}");
    }

    [Fact]
    public async Task DeclarationWithAFailingInitializerInALoopIsDeclaredBeforeTheLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run()
        On Error Resume Next
        For i = 1 To 3
            Dim q As Integer = Boom()
            System.Console.WriteLine(q)
        Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run()
    {
        int q = default;
        for (int i = 1; i <= 3; i++)
        {
            try
            {
                q = Boom();
            }
            catch
            {
            }
            try
            {
                Console.WriteLine(q);
            }
            catch
            {
            }
        }
    }
}");
    }

    [Fact]
    public async Task DeclarationWithAFailingInitializerInNestedLoopsIsDeclaredBeforeTheOuterLoopAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run()
        On Error Resume Next
        For i = 1 To 3
            For j = 1 To 3
                Dim q As Integer = Boom()
                System.Console.WriteLine(q)
            Next
        Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run()
    {
        int q = default;
        for (int i = 1; i <= 3; i++)
        {
            for (int j = 1; j <= 3; j++)
            {
                try
                {
                    q = Boom();
                }
                catch
                {
                }
                try
                {
                    Console.WriteLine(q);
                }
                catch
                {
                }
            }
        }
    }
}");
    }

    [Fact]
    public async Task GuardedDeclarationOfAnAnonymousTypeKeepsTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run()
        On Error Resume Next
        Dim q = New With {.X = Boom()}
        System.Console.WriteLine(q.X)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run()
    {
        ;
#error Cannot convert OnErrorResumeNextStatementSyntax - see comment for details
        /* Cannot convert OnErrorResumeNextStatementSyntax, CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 161


                Input:
                        On Error Resume Next

                 */
        var q = new { X = Boom() };
        Console.WriteLine(q.X);
    }
}CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 161", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task FailingCaseExpressionEntersThatCaseAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run(v As Integer)
        On Error Resume Next
        Select Case v
            Case 1, Boom()
                System.Console.WriteLine(""list"")
            Case Is > Boom()
                System.Console.WriteLine(""relational"")
            Case 5 To Boom()
                System.Console.WriteLine(""range"")
        End Select
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run(int v)
    {
        var selectValue = v;
        bool CaseMatches() { try { return selectValue == Boom(); } catch { return true; } }
        bool CaseMatches1() { try { return selectValue > Boom(); } catch { return true; } }
        bool CaseMatches2() { try { return 5 <= selectValue && selectValue <= Boom(); } catch { return true; } }

        switch (selectValue)
        {
            case 1:
            case var _ when CaseMatches():
                try
                {
                    Console.WriteLine(""list"");
                }
                catch
                {
                }

                break;
            case var _ when CaseMatches1():
                try
                {
                    Console.WriteLine(""relational"");
                }
                catch
                {
                }

                break;
            case var _ when CaseMatches2():
                try
                {
                    Console.WriteLine(""range"");
                }
                catch
                {
                }

                break;
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task FailingSelectAndCaseExpressionsAreBothGuardedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run()
        On Error Resume Next
        Select Case Boom()
            Case Boom()
                System.Console.WriteLine(""body"")
        End Select
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run()
    {
        try
        {
            var selectValue = Boom();
            bool CaseMatches() { try { return selectValue == Boom(); } catch { return true; } }

            switch (selectValue)
            {
                case var _ when CaseMatches():
                    try
                    {
                        Console.WriteLine(""body"");
                    }
                    catch
                    {
                    }

                    break;
            }
        }
        catch
        {
        }
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code");
    }

    [Fact]
    public async Task FailingCaseExpressionOnABooleanSelectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run(v As Integer)
        On Error Resume Next
        Select Case True
            Case Boom() = v
                System.Console.WriteLine(""body"")
        End Select
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run(int v)
    {
        bool CaseMatches() { try { return Boom() == v; } catch { return true; } }

        switch (true)
        {
            case object _ when CaseMatches():
                try
                {
                    Console.WriteLine(""body"");
                }
                catch
                {
                }

                break;
        }
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task FailingCaseExpressionReadingARefParameterKeepsTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom(i As Integer) As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run(v As Integer, ByRef limit As Integer)
        On Error Resume Next
        Select Case v
            Case Boom(limit)
                System.Console.WriteLine(""body"")
        End Select
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int Boom(int i)
    {
        throw new InvalidOperationException();
    }

    public void Run(int v, ref int limit)
    {
        ;
#error Cannot convert OnErrorResumeNextStatementSyntax - see comment for details
        /* Cannot convert OnErrorResumeNextStatementSyntax, CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 209


                Input:
                        On Error Resume Next

                 */
        switch (v)
        {
            case var @case when @case == Boom(limit):
                Console.WriteLine(""body"");
                break;
        }
    }
}----- Exception 1 of 2 -----

1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
----- Exception 2 of 2 -----
CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 209", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// A local function in a struct can't use <c>this</c> (CS1673), so a struct instance member that needs one keeps the fallback. A static member, or one with only plain guards, doesn't.
    /// </summary>
    [Fact]
    public async Task StructInstanceMembersThatNeedALocalFunctionKeepTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Structure S
    Public N As Integer
    Function Foo() As Integer
        Throw New Exception()
    End Function
    Sub Handler()
        On Error GoTo H
        N = Foo() : N += 1 : Exit Sub
H:
        N = 5 : Resume Next
    End Sub
    Sub Loop1()
        On Error Resume Next
        While N < Foo() : N += 1 : End While
    End Sub
    Sub Plain()
        On Error Resume Next
        N = Foo()
    End Sub
    Shared Sub St()
        On Error Resume Next
        Dim x = 0
        While x < Integer.Parse(""a"") : x += 1 : End While
    End Sub
End Structure", @"using System;

internal partial struct S
{
    public int N;
    public int Foo()
    {
        throw new Exception();
    }
    public void Handler()
    {
        try
        {
            N = Foo();
            N += 1;
            return;
        }
        catch
        {

            N = 5;
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 270


            Input:
             Resume Next

             */
        }
    }
    public void Loop1()
    {
        ;
#error Cannot convert OnErrorResumeNextStatementSyntax - see comment for details
        /* Cannot convert OnErrorResumeNextStatementSyntax, CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 321


                Input:
                        On Error Resume Next

                 */
        while (N < Foo())
            N += 1;
    }
    public void Plain()
    {
        try
        {
            N = Foo();
        }
        catch
        {
        }
    }
    public static void St()
    {
        int x = 0;
        bool LoopCondition() { try { return x < int.Parse(""a""); } catch { return true; } }

        while (LoopCondition())
            x += 1;
    }
}----- Exception 1 of 2 -----
CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 270

----- Exception 2 of 2 -----
CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 321", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// A guarded declaration in a loop is declared before the loop, where its name would clash with a local of the same name in an earlier block (CS0136), so that method keeps the fallback.
    /// </summary>
    [Fact]
    public async Task HoistingAGuardedDeclarationOutOfALoopKeepsTheFallbackWhenAnotherLocalHasTheSameNameAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function Foo() As Integer
        Return 1
    End Function

    Sub Run(a As Boolean)
        On Error Resume Next
        If a Then
            Dim b = Foo()
        End If
        For i = 1 To 2
            Dim b = Foo()
            Foo()
        Next
    End Sub
End Class", @"
internal partial class TestClass
{
    public int Foo()
    {
        return 1;
    }

    public void Run(bool a)
    {
        ;
#error Cannot convert OnErrorResumeNextStatementSyntax - see comment for details
        /* Cannot convert OnErrorResumeNextStatementSyntax, CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 121


                Input:
                        On Error Resume Next

                 */
        if (a)
        {
            int b = Foo();
        }
        for (int i = 1; i <= 2; i++)
        {
            int b = Foo();
            Foo();
        }
    }
}CONVERSION ERROR: Conversion for OnErrorResumeNextStatement not implemented, please report this issue in 'On Error Resume Next' at character 121", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// Creating a lambda can't fail, whatever its body does, so declaring one needs no guard (which would need to name its anonymous delegate type).
    /// </summary>
    [Fact]
    public async Task CreatingALambdaIsNotGuardedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function Foo() As Integer
        Return 1
    End Function

    Function Run() As Integer
        On Error Resume Next
        Dim a = Function(x As Integer) x + Foo()
        Return a(1)
    End Function
End Class", @"
internal partial class TestClass
{
    public int Foo()
    {
        return 1;
    }

    public int Run()
    {
        int a(int x) => x + Foo();
        try
        {
            return a(1);
        }
        catch
        {
        }

        return default;
    }
}");
    }
}
