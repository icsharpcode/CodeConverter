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
        try
        {
            count = count + 1;
        }
        catch
        {
        }
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
        {
            try
            {
                n += 1;
            }
            catch
            {
            }
        }
        bool LoopCondition1() { try { return items.Count > n; } catch { return true; } }

        while (LoopCondition1())
        {
            try
            {
                n += 1;
            }
            catch
            {
            }
        }
        bool LoopCondition2() { try { return items.Count != n; } catch { return true; } }

        while (LoopCondition2())
        {
            try
            {
                n += 1;
            }
            catch
            {
            }
        }
        bool LoopCondition3() { try { return items.Count > n; } catch { return false; } }

        do
        {
            try
            {
                n += 1;
            }
            catch
            {
            }
        }
        while (LoopCondition3());
        bool LoopCondition4() { try { return items.Count != n; } catch { return false; } }

        do
        {
            try
            {
                n += 1;
            }
            catch
            {
            }
        }
        while (LoopCondition4());
        while (n < 5)
        {
            try
            {
                n += 1;
            }
            catch
            {
            }
        }
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
        try
        {
            ComputeRet += 1;
        }
        catch
        {
        }

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
        {
            try
            {
                n -= 1;
            }
            catch (Exception ex) when (resumeNext)
            {
                ProjectData.SetProjectError(ex);
            }
        }
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
}
