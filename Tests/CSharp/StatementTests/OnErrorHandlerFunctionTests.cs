using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

/// <summary>
/// README.md "Handler function" shape: the handler resumes into the covered statements, so it becomes a local function that each guard calls.
/// The handler moves to just after the On Error statement, so the automated source-order comment check does not apply.
/// </summary>
public class OnErrorHandlerFunctionTests : ConverterTestBase
{
    [Fact]
    public async Task ResumeNextHandlerBecomesAVoidLocalFunctionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error GoTo Failed
        Console.WriteLine(""a"")
        Console.WriteLine(Integer.Parse(""b""))
        Exit Sub
Failed:
        Console.WriteLine(Err.Description)
        Resume Next
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Run()
    {
        ProjectData.ClearProjectError();

        void Failed() { Console.WriteLine(Information.Err().Description); ProjectData.ClearProjectError(); }
        try
        {
            Console.WriteLine(""a"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            Failed();
        }
        try
        {
            Console.WriteLine(int.Parse(""b""));
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            Failed();
        }
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task RetryOrExitHandlerReturnsWhetherToRetryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        Dim tries As Integer = 0
        On Error GoTo Failed
        Console.WriteLine(Integer.Parse(""a""))
        Console.WriteLine(""done"")
        Exit Sub
Failed:
        tries += 1
        If tries > 3 Then Exit Sub
        Resume
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run()
    {
        int tries = 0;

        bool FailedRetries() { tries += 1; if (tries > 3) { return false; } return true; }
    retry:
        try
        {
            Console.WriteLine(int.Parse(""a""));
        }
        catch
        {
            if (FailedRetries())
                goto retry;
            return;
        }
    retry1:
        try
        {
            Console.WriteLine(""done"");
        }
        catch
        {
            if (FailedRetries())
                goto retry1;
            return;
        }
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task ResumeNextOrExitHandlerReturnsWhetherToExitAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        Dim errors As Integer = 0
        On Error GoTo Failed
        Console.WriteLine(Integer.Parse(""a""))
        Console.WriteLine(Integer.Parse(""b""))
        Exit Sub
Failed:
        errors += 1
        If errors >= 2 Then Exit Sub
        Resume Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run()
    {
        int errors = 0;

        bool FailedExits() { errors += 1; if (errors >= 2) { return true; } return false; }
        try
        {
            Console.WriteLine(int.Parse(""a""));
        }
        catch
        {
            if (FailedExits())
                return;
        }
        try
        {
            Console.WriteLine(int.Parse(""b""));
        }
        catch
        {
            if (FailedExits())
                return;
        }
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task RetryOrResumeNextHandlerAndConditionsAndLoopsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.Collections.Generic
Class TestClass
    Sub Run(items As List(Of Integer))
        Dim tries As Integer = 0
        On Error GoTo Failed
        If items.Count = 1 Then
            Console.WriteLine(""one"")
        End If
        While items.Count > tries
            tries += 1
        End While
        Exit Sub
Failed:
        tries += 1
        If tries < 3 Then Resume
        Resume Next
    End Sub
End Class", @"using System;
using System.Collections.Generic;

internal partial class TestClass
{
    public void Run(List<int> items)
    {
        int tries = 0;

        bool FailedRetries() { tries += 1; if (tries < 3) return true; return false; }
        bool condition = true;
    retry1:
        try
        {
            condition = items.Count == 1;
        }
        catch
        {
            if (FailedRetries())
                goto retry1;
        }

        if (condition)
        {
        retry:
            try
            {
                Console.WriteLine(""one"");
            }
            catch
            {
                if (FailedRetries())
                    goto retry;
            }
        }
        bool LoopCondition() { retry3: try { return items.Count > tries; } catch { if (FailedRetries()) goto retry3; return true; } }

        while (LoopCondition())
        {
        retry2:
            try
            {
                tries += 1;
            }
            catch
            {
                if (FailedRetries())
                    goto retry2;
            }
        }
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task ReturnInTheHandlerOfAFunctionAndLocalsUsedByTheHandlerAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Run(text As String) As Integer
        On Error GoTo Failed
        Dim value As Integer = Integer.Parse(text)
        Return value
Failed:
        If value > 5 Then Resume Next
        Return -1
    End Function
End Class", @"
internal partial class TestClass
{
    public int Run(string text)
    {
        int value = default;
        int result = default;

        bool FailedExits() { if (value > 5) return false; result = -1; return true; }

        try
        {
            value = int.Parse(text);
        }
        catch
        {
            if (FailedExits())
                return result;
        }
        return value;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task ExitFunctionInTheHandlerReturnsTheReturnVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Run(text As String) As Integer
        Run = 5
        On Error GoTo Failed
        Run = Integer.Parse(text)
        Exit Function
Failed:
        If Err.Number = 5 Then Exit Function
        Resume Next
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Run(string text)
    {
        int RunRet = default;
        RunRet = 5;
        ProjectData.ClearProjectError();

        bool FailedExits() { if (Information.Err().Number == 5) { ProjectData.ClearProjectError(); return true; } ProjectData.ClearProjectError(); return false; }
        try
        {
            RunRet = int.Parse(text);
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            if (FailedExits())
                return RunRet;
        }
        return RunRet;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HandlerThatCapturesARefParameterKeepsPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run(ByRef count As Integer)
        On Error GoTo Failed
        Console.WriteLine(Integer.Parse(""a""))
        Exit Sub
Failed:
        count += 1
        Resume Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run(ref int count)
    {
        try
        {
            Console.WriteLine(int.Parse(""a""));
            return;
        }
        catch
        {

            count += 1;
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 223


            Input:
                    Resume Next

             */
        }
    }
}CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 223", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HandlerRunIntoWithoutAnErrorKeepsPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error GoTo Failed
        Console.WriteLine(Integer.Parse(""a""))
Failed:
        Console.WriteLine(""handler"")
        Resume Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run()
    {
        try
        {
            Console.WriteLine(int.Parse(""a""));
        }
        catch
        {
        }

        Console.WriteLine(""handler"");
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 215


                Input:
                        Resume Next

                 */
    }
}CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 215", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task GuardsInsideAGuardedBlockKeepPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.Collections.Generic
Class TestClass
    Sub Run(items As List(Of Integer))
        On Error GoTo Failed
        For Each item In items
            Console.WriteLine(Integer.Parse(""a""))
        Next
        Exit Sub
Failed:
        Resume Next
    End Sub
End Class", @"using System;
using System.Collections.Generic;

internal partial class TestClass
{
    public void Run(List<int> items)
    {
        try
        {
            foreach (var item in items)
                Console.WriteLine(int.Parse(""a""));
            return;
        }
        catch
        {
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 333


            Input:

                    Resume Next

             */
        }
    }
}CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 333", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task FailingLoopConditionThatMustLeaveTheMethodKeepsPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Imports System.Collections.Generic
Class TestClass
    Sub Run(items As List(Of Integer))
        Dim n As Integer = 0
        On Error GoTo Failed
        While items.Count > n
            n += 1
        End While
        Exit Sub
Failed:
        If n > 3 Then Exit Sub
        Resume Next
    End Sub
End Class", @"using System.Collections.Generic;

internal partial class TestClass
{
    public void Run(List<int> items)
    {
        int n = 0;
        try
        {
            while (items.Count > n)
                n += 1;
            return;
        }
        catch
        {

            if (n > 3)
                return;
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 354


            Input:
                    Resume Next

             */
        }
    }
}CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 354", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task TwoHandlersKeepPendingOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error GoTo First
        Console.WriteLine(Integer.Parse(""a""))
        On Error GoTo Second
        Console.WriteLine(Integer.Parse(""b""))
        Exit Sub
First:
        Resume Next
Second:
        Resume Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run()
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo First' at character 63


                Input:
                        On Error GoTo First

                 */
        Console.WriteLine(int.Parse(""a""));
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Second' at character 153


                Input:
                        On Error GoTo Second

                 */
        Console.WriteLine(int.Parse(""b""));
        return;
    First:
        ;
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
    /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 270


    Input:

            Resume Next

     */
    Second:
        ;
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 300


                Input:

                        Resume Next

                 */
    }
}----- Exception 1 of 4 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo First' at character 63

----- Exception 2 of 4 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Second' at character 153

----- Exception 3 of 4 -----
CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 270

----- Exception 4 of 4 -----
CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 300", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task GuardedDeclarationOfAnAnonymousTypeKeepsTheFallbackInAHandlerFunctionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run()
        On Error GoTo Failed
        Dim q = New With {.X = Boom()}
        System.Console.WriteLine(q.X)
        Exit Sub
Failed:
        Resume Next
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
            var q = new { X = Boom() };
            Console.WriteLine(q.X);
            return;
        }
        catch
        {
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 307


            Input:

                    Resume Next

             */
        }
    }
}CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 307", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task FailingCaseExpressionCallsTheHandlerFromALocalFunctionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom() As Integer
        Throw New System.InvalidOperationException()
    End Function

    Sub Run(v As Integer)
        On Error GoTo Failed
        Select Case v
            Case Boom()
                System.Console.WriteLine(""body"")
        End Select
        Exit Sub
Failed:
        Resume Next
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
        void Failed() { }
        var selectValue = v;
        bool CaseMatches() { try { return selectValue == Boom(); } catch { Failed(); return true; } }

        switch (selectValue)
        {
            case var _ when CaseMatches():
                try
                {
                    Console.WriteLine(""body"");
                }
                catch
                {
                    Failed();
                }

                break;
        }
        return;
    }
}
1 target compilation errors:
CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HandlerWhoseEveryBranchLeavesHasNoTrailingExitAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run()
        On Error GoTo Failed
        Console.WriteLine(Integer.Parse(""a""))
        Console.WriteLine(Integer.Parse(""b""))
        Exit Sub
Failed:
        Select Case Err.Number
            Case 5
                Resume Next
            Case Else
                Exit Sub
        End Select
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Run()
    {
        ProjectData.ClearProjectError();

        bool FailedExits() { switch (Information.Err().Number) { case 5: ProjectData.ClearProjectError(); return false; default: { ProjectData.ClearProjectError(); return true; } } }
        try
        {
            Console.WriteLine(int.Parse(""a""));
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            if (FailedExits())
                return;
        }
        try
        {
            Console.WriteLine(int.Parse(""b""));
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            if (FailedExits())
                return;
        }
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }
}
