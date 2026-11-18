using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp.StatementTests;

public class OnErrorStatementTests : ConverterTestBase
{
    [Fact]
    public async Task BasicGotoAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
Public Function SelfDivisionPossible(x as Integer) As Boolean
    On Error GoTo ErrorHandler
        Dim i as Integer = x / x
        Return True
ErrorHandler:
    Return Err.Number = 6
End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public bool SelfDivisionPossible(int x)
    {
        ProjectData.ClearProjectError();
        try
        {
            int i = (int)Math.Round(x / (double)x);
            return true;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            try
            {

                return Information.Err().Number == 6;
            }
            finally
            {
                ProjectData.ClearProjectError();
            }
        }
    }
}");
    }

    [Fact]
    public async Task RemainingScopeIssueCharacterizationAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
Public Function SelfDivisionPossible(x as Integer) As Boolean
    On Error GoTo ErrorHandler
        Dim i as Integer = x / x
ErrorHandler:
    Return i <> 0
End Function
End Class", @"using System;

internal partial class TestClass
{
    public bool SelfDivisionPossible(int x)
    {
        int i = default;
        try
        {
            i = (int)Math.Round(x / (double)x);
        }
        catch
        {
        }

        return i != 0;
    }
}");
    }

    [Fact]
    public async Task ErrNotUsedGivesBareCatchAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }
    }
}");
    }

    [Fact]
    public async Task ErrUsedInAnotherMemberOfTheTypeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Failed:
        LogError()
    End Sub

    Sub LogError()
        Console.WriteLine(Err.Description)
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Save()
    {
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            LogError();
            ProjectData.ClearProjectError();
        }
    }

    public void LogError()
    {
        Console.WriteLine(Information.Err().Description);
    }
}");
    }

    [Fact]
    public async Task ErrUsedInAnErrHelperElsewhereInTheProjectAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Failed:
        ErrorLog.Write()
    End Sub
End Class

Module ErrorLog
    Sub Write()
        Console.WriteLine(Err.Description)
    End Sub
End Module", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Save()
    {
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            ErrorLog.Write();
            ProjectData.ClearProjectError();
        }
    }
}

internal static partial class ErrorLog
{
    public static void Write()
    {
        Console.WriteLine(Information.Err().Description);
    }
}");
    }

    [Fact]
    public async Task ErrReadOnlyInACatchBlockElsewhereIsNotAnErrHelperAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
    End Sub
End Class

Module Other
    Sub Run()
        Try
            Console.WriteLine(""run"")
        Catch
            Console.WriteLine(Err.Description)
        End Try
    End Sub
End Module", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Save()
    {
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }
    }
}

internal static partial class Other
{
    public static void Run()
    {
        try
        {
            Console.WriteLine(""run"");
        }
        catch
        {
            Console.WriteLine(Information.Err().Description);
        }
    }
}");
    }

    [Fact]
    public async Task HoistsLocalsUsedInTheHandlerAndAfterAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Parse(s As String) As Integer
        On Error GoTo Failed
        Dim stage = ""parse"", unused = 1
        Dim result As Integer = Integer.Parse(s)
        Dim items(2) As String
        Const Doubled = 2
        Return result * Doubled
Failed:
        Console.WriteLine(stage & items.Length & Doubled)
        Return result
    End Function
End Class", @"using System;

internal partial class TestClass
{
    public int Parse(string s)
    {
        string stage = default;
        int result = default;
        string[] items = default;
        const int Doubled = 2;
        try
        {
            stage = ""parse"";
            int unused = 1;
            result = int.Parse(s);
            items = new string[3];
            return result * Doubled;
        }
        catch
        {

            Console.WriteLine(stage + items.Length + Doubled);
            return result;
        }
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HoistsLocalsDeclaredAfterTheTryThatTheHandlerUsesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Cleanup:
        Dim message = ""done""
        Console.WriteLine(message)
        Exit Sub
Failed:
        message = ""failed""
        Resume Cleanup
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {

        string message = default;
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {

            message = ""failed"";
        }

        message = ""done"";
        Console.WriteLine(message);
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task ReturnAndNestedBlocksInsideCoveredCodeAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function FirstNegative(values As Integer()) As Integer
        On Error GoTo Failed
        For Each v In values
            If v < 0 Then
                Return v
            End If
        Next
        Return 0
Failed:
        Return -1
    End Function
End Class", @"
internal partial class TestClass
{
    public int FirstNegative(int[] values)
    {
        try
        {
            foreach (var v in values)
            {
                if (v < 0)
                {
                    return v;
                }
            }
            return 0;
        }
        catch
        {

            return -1;
        }
    }
}");
    }

    [Fact]
    public async Task HandlerFallsOffTheEndBeforeLaterStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save(retry As Boolean)
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Done:
        Console.WriteLine(""done"")
        Exit Sub
Failed:
        If retry Then Resume Done
        Console.WriteLine(""failed"")
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save(bool retry)
    {
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {

            if (retry)
                goto Done;
            Console.WriteLine(""failed"");
            return;
        }

    Done:
        ;

        Console.WriteLine(""done"");
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HandlerReachedWithoutErrorAfterOtherStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Finish
        Console.WriteLine(""saving"")
        On Error GoTo 0
        Console.WriteLine(""saved"")
Finish:
        Console.WriteLine(""finished"")
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {
            goto Finish;
        }
        Console.WriteLine(""saved"");
    Finish:
        ;

        Console.WriteLine(""finished"");
    }
}");
    }

    [Fact]
    public async Task CleanupIdiomWithoutReentryGuardLetsCleanupErrorsPropagateAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Cleanup:
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
        Resume Cleanup
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {

            Console.WriteLine(""failed"");
        }

        Console.WriteLine(""cleanup"");
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task CleanupIdiomWithCounterReentryGuardIsSegmentedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        Dim attempts = 0
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Cleanup:
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        attempts += 1
        If attempts > 3 Then Exit Sub
        Console.WriteLine(Err.Description)
        Resume Cleanup
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Save()
    {
        int attempts = 0;
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""saving"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Cleanup:
        ;
        try
        {

            Console.WriteLine(""cleanup"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Failed:
        ;

        attempts += 1;
        if (attempts > 3)
        {
            ProjectData.ClearProjectError();
            return;
        }
        Console.WriteLine(Information.Err().Description);
        ProjectData.ClearProjectError();
        goto Cleanup;
    }
}");
    }

    [Fact]
    public async Task CleanupIdiomWithFlagReentryGuardIsSegmentedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        Dim cleaningUp As Boolean
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Cleanup:
        cleaningUp = True
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        If cleaningUp Then Exit Sub
        Resume Cleanup
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {
        bool cleaningUp = default;
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {
            goto Failed;
        }

    Cleanup:
        ;
        try
        {

            cleaningUp = true;
            Console.WriteLine(""cleanup"");
            return;
        }
        catch
        {
            goto Failed;
        }

    Failed:
        ;

        if (cleaningUp)
            return;
        goto Cleanup;
    }
}");
    }

    [Fact]
    public async Task CleanupThatCannotThrowIsNotSegmentedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Save() As Integer
        Dim attempts = 0
        Dim result = 0
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        result = 1
Cleanup:
        attempts = 0
        Return result
Failed:
        attempts += 1
        If attempts > 3 Then Return -1
        Resume Cleanup
    End Function
End Class", @"using System;

internal partial class TestClass
{
    public int Save()
    {
        int attempts = 0;
        int result = 0;
        try
        {
            Console.WriteLine(""saving"");
            result = 1;
        }
        catch
        {

            attempts += 1;
            if (attempts > 3)
                return -1;
        }

        attempts = 0;
        return result;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task CleanupStartingWithItsOwnOnErrorStatementIsNotSegmentedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        Dim attempts = 0
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Cleanup:
        On Error GoTo 0
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        attempts += 1
        If attempts > 3 Then Exit Sub
        Console.WriteLine(Err.Number)
        Resume Cleanup
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Save()
    {
        int attempts = 0;
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""saving"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            attempts += 1;
            if (attempts > 3)
            {
                ProjectData.ClearProjectError();
                return;
            }
            Console.WriteLine(Information.Err().Number);
            ProjectData.ClearProjectError();
        }

        ProjectData.ClearProjectError();
        Console.WriteLine(""cleanup"");
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HandlerGoToLabelAfterCoveredStatementsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Report:
        Console.WriteLine(""report"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
        GoTo Report
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }

        Console.WriteLine(""report"");
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task ErrRaiseAndClearAreNotReadsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
    End Sub
End Class

Module Errors
    Sub Fail()
        Err.Clear()
        Err.Raise(5)
    End Sub
End Module", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Save()
    {
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }
    }
}

internal static partial class Errors
{
    public static void Fail()
    {
        Information.Err().Clear();
        Information.Err().Raise(5);
    }
}");
    }

    [Fact]
    public async Task HandlerReturnsTheReturnVariableAndIgnoresLambdaReturnsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Code() As Integer
        On Error GoTo Failed
        Code = 1
        Console.WriteLine(""saving"")
        Exit Function
Failed:
        Dim describe = Function()
                           Return Err.Description
                       End Function
        Console.WriteLine(describe())
        Code = Err.Number
        Exit Function
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Code()
    {
        int CodeRet = default;
        ProjectData.ClearProjectError();
        try
        {
            CodeRet = 1;
            Console.WriteLine(""saving"");
            return CodeRet;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            string describe() => Information.Err().Description;
            Console.WriteLine(describe());
            CodeRet = Information.Err().Number;
            ProjectData.ClearProjectError();
            return CodeRet;
        }
    }
}");
    }

    [Fact]
    public async Task HoistingKeepsDeclarationOrderAndLeavesStaticLocalsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Dim first = 1, second = first + 1, third = second + 1
        Dim kept, used As Integer
        Static calls As Integer
        calls += 1
        Exit Sub
Failed:
        Console.WriteLine(second & used & calls)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int _Save_calls = default;
    public void Save()
    {
        int second = default;
        int used = default;
        try
        {
            int first = 1;
            second = first + 1;
            int third = second + 1;
            int kept;
            _Save_calls += 1;
            return;
        }
        catch
        {

            Console.WriteLine(second.ToString() + used + _Save_calls);
        }
    }
}");
    }

    [Fact]
    public async Task ShapesWithoutAnEmitterYetKeepTheirOutputAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub ConditionalOnError(flag As Boolean)
        If flag Then On Error GoTo Failed
        Console.WriteLine(""work"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
    End Sub

    Sub ResetsHandlerState()
        On Error GoTo Failed
        Console.WriteLine(""work"")
        Exit Sub
Failed:
        On Error GoTo -1
        Console.WriteLine(""failed"")
    End Sub

    Sub HandlerLabelInsideABlock(flag As Boolean)
        On Error GoTo Failed
        Console.WriteLine(""work"")
        If flag Then
Failed:
            Console.WriteLine(""failed"")
        End If
    End Sub

    Sub Retries()
        On Error GoTo Failed
        Console.WriteLine(""work"")
        Exit Sub
Failed:
        Resume
    End Sub

    Sub HandlerBeforeOnError()
        GoTo Start
Failed:
        Console.WriteLine(""failed"")
        Exit Sub
Start:
        On Error GoTo Failed
        Console.WriteLine(""work"")
    End Sub

    Sub RunsIntoHandlerThatResumes()
        On Error GoTo Failed
        Console.WriteLine(""work"")
Done:
        Console.WriteLine(""done"")
Failed:
        Resume Done
    End Sub

    Sub JumpsIntoHandler(skip As Boolean)
        On Error GoTo Failed
        If skip Then GoTo Failed
        Console.WriteLine(""work"")
        Exit Sub
Failed:
        Console.WriteLine(""failed"")
    End Sub

    Sub JumpsBackIntoCoveredCode(retry As Boolean)
        On Error GoTo Failed
Again:
        Console.WriteLine(""work"")
Cleanup:
        If retry Then GoTo Again
        Exit Sub
Failed:
        Resume Cleanup
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void ConditionalOnError(bool flag)
    {
        if (flag)
            ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 106


                    Input:
                    On Error GoTo Failed

                     */
        Console.WriteLine(""work"");
        return;
    Failed:
        ;

        Console.WriteLine(""failed"");
    }

    public void ResetsHandlerState()
    {
        try
        {
            Console.WriteLine(""work"");
            return;
        }
        catch
        {
            ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
            /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToMinusOneStatement not implemented, please report this issue in 'On Error GoTo -1' at character 414


                        Input:

                                On Error GoTo -1

                         */
            Console.WriteLine(""failed"");
        }
    }

    public void HandlerLabelInsideABlock(bool flag)
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 557


                Input:
                        On Error GoTo Failed

                 */
        Console.WriteLine(""work"");
        if (flag)
        {
        Failed:
            ;

            Console.WriteLine(""failed"");
        }
    }

    public void Retries()
    {
        try
        {
            Console.WriteLine(""work"");
            return;
        }
        catch
        {
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeStatement not implemented, please report this issue in 'Resume' at character 878


            Input:

                    Resume

             */
        }
    }

    public void HandlerBeforeOnError()
    {
        goto Start;
    Failed:
        ;

        Console.WriteLine(""failed"");
        return;
    Start:
        ;
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 1047


                Input:

                        On Error GoTo Failed

                 */
        Console.WriteLine(""work"");
    }

    public void RunsIntoHandlerThatResumes()
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 1179


                Input:
                        On Error GoTo Failed

                 */
        Console.WriteLine(""work"");
    Done:
        ;

        Console.WriteLine(""done"");
    Failed:
        ;
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Done' at character 1323


                Input:

                        Resume Done

                 */
    }

    public void JumpsIntoHandler(bool skip)
    {
        try
        {
            if (skip)
                goto Failed;
            Console.WriteLine(""work"");
            return;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }
    }

    public void JumpsBackIntoCoveredCode(bool retry)
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
    /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 1662


    Input:
            On Error GoTo Failed

     */
    Again:
        ;

        Console.WriteLine(""work"");
    Cleanup:
        ;

        if (retry)
            goto Again;
        return;
    Failed:
        ;
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Cleanup' at character 1820


                Input:

                        Resume Cleanup

                 */
    }
}----- Exception 1 of 10 -----

1 target compilation errors:
CS0159: No such label 'Failed' within the scope of the goto statement
----- Exception 2 of 10 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 106

----- Exception 3 of 10 -----
CONVERSION ERROR: Conversion for OnErrorGoToMinusOneStatement not implemented, please report this issue in 'On Error GoTo -1' at character 414

----- Exception 4 of 10 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 557

----- Exception 5 of 10 -----
CONVERSION ERROR: Conversion for ResumeStatement not implemented, please report this issue in 'Resume' at character 878

----- Exception 6 of 10 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 1047

----- Exception 7 of 10 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 1179

----- Exception 8 of 10 -----
CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Done' at character 1323

----- Exception 9 of 10 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 1662

----- Exception 10 of 10 -----
CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Cleanup' at character 1820", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task ReEntryGuardsAreFoundInAnyConditionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Private _attempts As Integer

    Sub IfElseIfGuard()
        On Error GoTo Failed
        Console.WriteLine(""work"")
Cleanup:
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        _attempts += 1
        If _attempts > 3 Then
            Exit Sub
        ElseIf _attempts > 2 Then
            Console.WriteLine(""last try"")
        End If
        Resume Cleanup
    End Sub

    Sub SelectGuard()
        Dim attempts = 0
        On Error GoTo Failed
        Console.WriteLine(""work"")
Cleanup:
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        attempts += 1
        Select Case attempts
            Case Is > 3
                Exit Sub
        End Select
        Resume Cleanup
    End Sub

    Sub LoopGuard()
        Dim attempts = 0
        On Error GoTo Failed
        Console.WriteLine(""work"")
Cleanup:
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        attempts += 1
        While attempts < 2
            attempts += 1
        End While
        Do Until attempts > 0
        Loop
        Console.WriteLine(If(attempts > 3, ""giving up"", ""retrying""))
        Resume Cleanup
    End Sub

    Sub TwoResumeLabels(early As Boolean)
        Dim attempts = 0
        On Error GoTo Failed
        Console.WriteLine(""work"")
First:
        Console.WriteLine(""first"")
Second:
        Console.WriteLine(""second"")
        Exit Sub
Failed:
        attempts += 1
        If attempts > 3 Then Exit Sub
        If early Then Resume First
        Resume Second
    End Sub

    Sub ErrNumberIsNotAGuard()
        On Error GoTo Failed
        Console.WriteLine(""work"")
Cleanup:
        Console.WriteLine(""cleanup"")
        Exit Sub
Failed:
        If Err.Number = 5 Then Exit Sub
        Resume Cleanup
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private int _attempts;

    public void IfElseIfGuard()
    {
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""work"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Cleanup:
        ;
        try
        {

            Console.WriteLine(""cleanup"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Failed:
        ;

        _attempts += 1;
        if (_attempts > 3)
        {
            ProjectData.ClearProjectError();
            return;
        }
        else if (_attempts > 2)
        {
            Console.WriteLine(""last try"");
        }
        ProjectData.ClearProjectError();
        goto Cleanup;
    }

    public void SelectGuard()
    {
        int attempts = 0;
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""work"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Cleanup:
        ;
        try
        {

            Console.WriteLine(""cleanup"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Failed:
        ;

        attempts += 1;
        switch (attempts)
        {
            case > 3:
                {
                    ProjectData.ClearProjectError();
                    return;
                }
        }
        ProjectData.ClearProjectError();
        goto Cleanup;
    }

    public void LoopGuard()
    {
        int attempts = 0;
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""work"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Cleanup:
        ;
        try
        {

            Console.WriteLine(""cleanup"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Failed:
        ;

        attempts += 1;
        while (attempts < 2)
            attempts += 1;
        while (attempts <= 0)
        {
        }
        Console.WriteLine(attempts > 3 ? ""giving up"" : ""retrying"");
        ProjectData.ClearProjectError();
        goto Cleanup;
    }

    public void TwoResumeLabels(bool early)
    {
        int attempts = 0;
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""work"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    First:
        ;
        try
        {

            Console.WriteLine(""first"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Second:
        ;
        try
        {

            Console.WriteLine(""second"");
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            goto Failed;
        }

    Failed:
        ;

        attempts += 1;
        if (attempts > 3)
        {
            ProjectData.ClearProjectError();
            return;
        }
        if (early)
        {
            ProjectData.ClearProjectError();
            goto First;
        }
        ProjectData.ClearProjectError();
        goto Second;
    }

    public void ErrNumberIsNotAGuard()
    {
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""work"");
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            if (Information.Err().Number == 5)
            {
                ProjectData.ClearProjectError();
                return;
            }
            ProjectData.ClearProjectError();
        }

        Console.WriteLine(""cleanup"");
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task CleanupOfOnlyJumpsAndConstantsIsNotSegmentedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Save(fallback As Integer) As Integer
        Dim attempts = 0
        On Error GoTo Failed
        Console.WriteLine(""saving"")
Cleanup:
        Dim finished As Boolean = True
        Dim result As Integer = fallback
        GoTo Done
Done:
        Return result
Failed:
        attempts += 1
        If attempts > 3 Then Exit Function
        Resume Cleanup
    End Function
End Class", @"using System;

internal partial class TestClass
{
    public int Save(int fallback)
    {
        int attempts = 0;
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {

            attempts += 1;
            if (attempts > 3)
                return default;
        }

        bool finished = true;
        int result = fallback;
        goto Done;
    Done:
        ;

        return result;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task HandlerThatOnlyResumesNextBecomesAVoidFunctionAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Save()
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Exit Sub
Failed:
        Resume Next
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Save()
    {
        void Failed() { }
        try
        {
            Console.WriteLine(""saving"");
        }
        catch
        {
            Failed();
        }
        return;
    }
}
", incompatibleWithAutomatedCommentTesting: true);
    }

    [Fact]
    public async Task FunctionWhoseHandlerFallsOffTheEndStillReturnsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Function Boom(s As String) As Integer
        Throw New System.InvalidOperationException(s)
    End Function

    Private Function B1() As Integer
        On Error GoTo H
        B1 = Boom(""a"")
        Exit Function
H:
        B1 = -1
    End Function
End Class", @"using System;

internal partial class TestClass
{
    private int Boom(string s)
    {
        throw new InvalidOperationException(s);
    }

    private int B1()
    {
        int B1Ret = default;
        try
        {
            B1Ret = Boom(""a"");
            return B1Ret;
        }
        catch
        {

            B1Ret = -1;
        }

        return B1Ret;
    }
}");
    }

    [Fact]
    public async Task FunctionWithReturnValueAndLoggingHandlerStillReturnsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Private Function Boom() As Integer
        Throw New InvalidOperationException()
    End Function

    Private Function Run() As Integer
        On Error GoTo H
        Dim x = Boom()
        Return x
H:
        Console.WriteLine(""failed"")
    End Function
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    private int Run()
    {
        try
        {
            int x = Boom();
            return x;
        }
        catch
        {

            Console.WriteLine(""failed"");
        }

        return default;
    }
}");
    }

    [Fact]
    public async Task PropertyGetWhoseHandlerFallsOffTheEndStillReturnsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Private Function Boom() As Integer
        Throw New InvalidOperationException()
    End Function

    Public ReadOnly Property P As Integer
        Get
            On Error GoTo H
            P = Boom()
            Exit Property
H:
            P = -1
        End Get
    End Property
End Class", @"using System;

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    public int P
    {
        get
        {
            int PRet = default;
            try
            {
                PRet = Boom();
                return PRet;
            }
            catch
            {

                PRet = -1;
            }

            return PRet;
        }
    }
}");
    }

    [Fact]
    public async Task FunctionWhoseSelectCaseHandlerCanFallOffTheEndStillReturnsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Private Function Boom() As Integer
        Throw New InvalidOperationException()
    End Function

    Private Function Run() As Integer
        On Error GoTo H
        Return Boom()
H:
        Select Case Err.Number
            Case 5
                Return 5
        End Select
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    private int Boom()
    {
        throw new InvalidOperationException();
    }

    private int Run()
    {
        ProjectData.ClearProjectError();
        try
        {
            return Boom();
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            switch (Information.Err().Number)
            {
                case 5:
                    {
                        ProjectData.ClearProjectError();
                        return 5;
                    }
            }

            ProjectData.ClearProjectError();
        }

        return default;
    }
}");
    }

    [Fact]
    public async Task HoistedLocalOfAnAnonymousTypeKeepsTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private Sub Boom()
        Throw New System.InvalidOperationException()
    End Sub

    Sub Run()
        On Error GoTo Failed
        Dim q = New With {.X = 1}
        Boom()
        Exit Sub
Failed:
        System.Console.WriteLine(q.X)
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private void Boom()
    {
        throw new InvalidOperationException();
    }

    public void Run()
    {
        try
        {
            var q = new { X = 1 };
            Boom();
            return;
        }
        catch
        {

            Console.WriteLine(q.X);
        }
    }
}
1 target compilation errors:
CS0103: The name 'q' does not exist in the current context");
    }

    [Fact]
    public async Task HandlerLeavesReturnsInsideReassignedLambdasAloneWhenReturnVariableIsUsedAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Code() As Integer
        On Error GoTo Failed
        Code = 1
        Console.WriteLine(""saving"")
        Exit Function
Failed:
        Dim pick As Func(Of Integer, Integer) = Nothing
        pick = Function(x)
                   If x > 1 Then Return x
                   Return 0
               End Function
        Dim pickAgain As Func(Of Integer, Integer) = Function(x As Integer)
                                                         Return x + 1
                                                     End Function
        pickAgain = Function(x As Integer) As Integer
                        Return x + 2
                    End Function
        Code = pick(Err.Number) + pickAgain(1)
        Exit Function
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Code()
    {
        int CodeRet = default;
        ProjectData.ClearProjectError();
        try
        {
            CodeRet = 1;
            Console.WriteLine(""saving"");
            return CodeRet;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            Func<int, int> pick = null;
            pick = new Func<int, int>(x =>
                {
                    if (x > 1)
                        return x;
                    return 0;
                });
            Func<int, int> pickAgain = (x) => x + 1;
            pickAgain = new Func<int, int>((x) => x + 2);
            CodeRet = pick(Information.Err().Number) + pickAgain(1);
            ProjectData.ClearProjectError();
            return CodeRet;
        }
    }
}");
    }

    [Fact]
    public async Task HandlerLeavesReturnsInsideLambdasAloneWithoutAReturnVariableAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Code() As Integer
        On Error GoTo Failed
        Console.WriteLine(""saving"")
        Return 1
Failed:
        Dim pick As Func(Of Integer, Integer) = Nothing
        pick = Function(x)
                   If x > 1 Then Return x
                   Return 0
               End Function
        Return pick(Err.Number)
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Code()
    {
        ProjectData.ClearProjectError();
        try
        {
            Console.WriteLine(""saving"");
            return 1;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            Func<int, int> pick = null;
            pick = new Func<int, int>(x =>
                {
                    if (x > 1)
                        return x;
                    return 0;
                });
            try
            {
                return pick(Information.Err().Number);
            }
            finally
            {
                ProjectData.ClearProjectError();
            }
        }
    }
}");
    }

    [Fact]
    public async Task HandlerResumingToALabelBeforeOnErrorKeepsTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Sub Run(attempts As Integer)
Start:
        Console.WriteLine(""start"")
        On Error GoTo Failed
        Console.WriteLine(""work"")
        Exit Sub
Failed:
        Resume Start
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Run(int attempts)
    {
    Start:
        ;

        Console.WriteLine(""start"");
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 140


                Input:
                        On Error GoTo Failed

                 */
        Console.WriteLine(""work"");
        return;
    Failed:
        ;
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Start' at character 246


                Input:

                        Resume Start

                 */
    }
}----- Exception 1 of 2 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo Failed' at character 140

----- Exception 2 of 2 -----
CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Start' at character 246", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// A handler that leaves to a label after itself: the statements from that label come after the try, and the handler's jump to them is a fall through.
    /// </summary>
    [Fact]
    public async Task HandlerResumingToALabelAfterItIsStructuredAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function Foo() As Integer
        Return 1
    End Function

    Function Run() As Integer
        Dim r = 0
        On Error GoTo H
        r = Foo() : GoTo Done
H:
        r = 5 : Resume Done
Done:
        Return r
    End Function

    Function RunWithErr() As Integer
        Dim r = 0
        On Error GoTo H
        r = Foo() : GoTo Done
H:
        r = Err.Number
        GoTo Done
Done:
        Return r
    End Function
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Foo()
    {
        return 1;
    }

    public int Run()
    {
        int r = 0;
        ProjectData.ClearProjectError();
        try
        {
            r = Foo();
            goto Done;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            r = 5;
            ProjectData.ClearProjectError();
        }

    Done:
        ;

        return r;
    }

    public int RunWithErr()
    {
        int r = 0;
        ProjectData.ClearProjectError();
        try
        {
            r = Foo();
            goto Done;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            r = Information.Err().Number;
        }

    Done:
        ;

        return r;
    }
}");
    }

    /// <summary>
    /// With no handler ever active, <c>On Error GoTo 0</c> does nothing but clear <c>Err</c> (checked by running VB), so it needs no shape.
    /// </summary>
    [Fact]
    public async Task OnErrorGoToZeroWithoutAHandlerOnlyClearsErrAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub WithoutErr(flag As Boolean)
        On Error GoTo 0
        System.Console.WriteLine(""a"")
        If flag Then On Error GoTo 0
    End Sub
End Class
Class UsesErr
    Sub WithErr()
        System.Console.WriteLine(""a"")
        On Error GoTo 0
        System.Console.WriteLine(Err.Number)
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void WithoutErr(bool flag)
    {
        Console.WriteLine(""a"");
        if (flag)
        {
        }
    }
}

internal partial class UsesErr
{
    public void WithErr()
    {
        Console.WriteLine(""a"");
        ProjectData.ClearProjectError();
        Console.WriteLine(Information.Err().Number);
    }
}");
    }

    /// <summary>
    /// Err isn't cleared after a statement whose end can't be reached, e.g. a using whose body always returns.
    /// </summary>
    [Fact]
    public async Task ErrIsNotClearedAfterCodeThatAlwaysLeavesAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Imports System
Class TestClass
    Function Foo() As Integer
        Return 1
    End Function

    Sub HandlerFallsOffTheEnd()
        On Error GoTo H
        Foo()
        Exit Sub
H:
        Console.WriteLine(Err.Number)
        Using New System.IO.StringReader(""x"")
            Return
        End Using
    End Sub

    Sub CodeBeforeOnErrorAlwaysLeaves()
        Using New System.IO.StringReader(""x"")
            Return
        End Using
        On Error GoTo H
        Foo()
        Exit Sub
H:
        Console.WriteLine(Err.Number)
    End Sub

    Sub HandlerFunctionAfterALeavingUsing()
        On Error GoTo H
        Foo()
        Exit Sub
H:
        Console.WriteLine(Err.Number)
        Using New System.IO.StringReader(""x"")
            Return
        End Using
        Resume Next
    End Sub
End Class", @"using System;
using System.IO;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public int Foo()
    {
        return 1;
    }

    public void HandlerFallsOffTheEnd()
    {
        ProjectData.ClearProjectError();
        try
        {
            Foo();
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            Console.WriteLine(Information.Err().Number);
            using (new StringReader(""x""))
            {
                ProjectData.ClearProjectError();
                return;
            }
        }
    }

    public void CodeBeforeOnErrorAlwaysLeaves()
    {
        using (new StringReader(""x""))
            return;
        try
        {
            Foo();
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            Console.WriteLine(Information.Err().Number);
            ProjectData.ClearProjectError();
        }
    }

    public void HandlerFunctionAfterALeavingUsing()
    {
        ProjectData.ClearProjectError();

        bool HExits() { Console.WriteLine(Information.Err().Number); using (new StringReader(""x"")) { ProjectData.ClearProjectError(); return true; } }
        try
        {
            Foo();
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);
            if (HExits())
                return;
        }
        return;
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// The tail after the label can end reachably although VB's flow analysis says the label is unreachable (only the handler jumps to it), so a return is added.
    /// </summary>
    [Fact]
    public async Task TailAfterTheHandlerEndingReachablyGetsAReturnAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Function F() As Integer
        On Error GoTo H
        F = 1
        Exit Function
H:
        F = 2
        Resume Done
Done:
        F += 1
    End Function
End Class", @"
internal partial class TestClass
{
    public int F()
    {
        int FRet = default;
        try
        {
            FRet = 1;
            return FRet;
        }
        catch
        {

            FRet = 2;
        }

        FRet += 1;
        return FRet;
    }
}");
    }

    /// <summary>
    /// The handler resumes to a label after the only statement that can't fail, so the code after the label has to be protected too: a guard in that code (`i` is assigned and tested after the label) says it can re-enter the handler.
    /// </summary>
    [Fact]
    public async Task ResumingToALabelInTheCoveredCodeIsSegmentedWhenItCountsItsAttemptsAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub Boom()
        Throw New System.Exception()
    End Sub

    Function Retries() As Integer
        On Error GoTo EH
        Dim i As Integer = 0
Retry:
        i += 1
        If i < 3 Then Boom()
        Retries = i
        Exit Function
EH:
        Resume Retry
    End Function
End Class", @"using System;

internal partial class TestClass
{
    public void Boom()
    {
        throw new Exception();
    }

    public int Retries()
    {
        int RetriesRet = default;
        int i = default;
        try
        {
            i = 0;
        }
        catch
        {
            goto EH;
        }

    Retry:
        ;
        try
        {

            i += 1;
            if (i < 3)
                Boom();
            RetriesRet = i;
            return RetriesRet;
        }
        catch
        {
            goto EH;
        }

    EH:
        ;

        goto Retry;
    }
}");
    }

    /// <summary>
    /// The first error can only come from the code after the label, which the plain form would leave unprotected, and with no guard the segmented form isn't used.
    /// </summary>
    [Fact]
    public async Task ResumingToALabelInTheCoveredCodeKeepsTheFallbackWithoutAGuardAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub Boom()
        Throw New System.Exception()
    End Sub

    Sub Fix()
    End Sub

    Sub RetriesWithoutAGuard()
        On Error GoTo EH
        Dim i As Integer = 0
Retry:
        Boom()
        Exit Sub
EH:
        Fix()
        Resume Retry
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Boom()
    {
        throw new Exception();
    }

    public void Fix()
    {
    }

    public void RetriesWithoutAGuard()
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo EH' at character 163


                Input:
                        On Error GoTo EH

                 */
        int i = 0;
    Retry:
        ;

        Boom();
        return;
    EH:
        ;

        Fix();
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Retry' at character 287


                Input:
                        Resume Retry

                 */
    }
}----- Exception 1 of 2 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo EH' at character 163

----- Exception 2 of 2 -----
CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Retry' at character 287", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// Nothing is between the `On Error` and the first label the handler resumes to, so there is nothing to protect there.
    /// </summary>
    [Fact]
    public async Task ASegmentWithNothingToProtectGetsNoTryAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private attempts As Integer

    Sub Boom()
        Throw New System.Exception()
    End Sub

    Function Retries() As Integer
        On Error GoTo EH
Retry:
        attempts += 1
        If attempts < 3 Then Boom()
        Retries = attempts
        Exit Function
EH:
        Resume Retry
    End Function
End Class", @"using System;

internal partial class TestClass
{
    private int attempts;

    public void Boom()
    {
        throw new Exception();
    }

    public int Retries()
    {
        int RetriesRet = default;
    Retry:
        ;
        try
        {

            attempts += 1;
            if (attempts < 3)
                Boom();
            RetriesRet = attempts;
            return RetriesRet;
        }
        catch
        {
            goto EH;
        }

    EH:
        ;

        goto Retry;
    }
}");
    }

    /// <summary>
    /// In the try/catch shape the catch already lets later errors propagate, so On Error GoTo 0 in the handler only has to clear Err (VB sets Err.Number to 0 there, checked by running it), or nothing when Err isn't used.
    /// </summary>
    [Fact]
    public async Task OnErrorGoToZeroInAStructuredHandlerOnlyClearsErrAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub Boom()
        Throw New System.Exception()
    End Sub

    Sub Log(s As String)
    End Sub

    Sub M()
        On Error GoTo H
        Boom()
        Exit Sub
H:
        Log(Err.Description)
        On Error GoTo 0
        Boom()
    End Sub
End Class

Class WithoutErr
    Sub Boom()
        Throw New System.Exception()
    End Sub

    Sub M()
        On Error GoTo H
        Boom()
        Exit Sub
H:
        On Error GoTo 0
        Boom()
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Boom()
    {
        throw new Exception();
    }

    public void Log(string s)
    {
    }

    public void M()
    {
        ProjectData.ClearProjectError();
        try
        {
            Boom();
            return;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            Log(Information.Err().Description);
            ProjectData.ClearProjectError();
            Boom();
            ProjectData.ClearProjectError();
        }
    }
}

internal partial class WithoutErr
{
    public void Boom()
    {
        throw new Exception();
    }

    public void M()
    {
        try
        {
            Boom();
            return;
        }
        catch
        {
            Boom();
        }
    }
}", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// The handler is written for re-entry, which needs the segmented form, but it turns error handling off for the code it resumes to: that code is protected when reached without an error and not when resumed to, so no form is exact.
    /// </summary>
    [Fact]
    public async Task OnErrorGoToZeroInAHandlerWrittenForReEntryKeepsTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Private attempts As Integer

    Sub Boom()
        Throw New System.Exception()
    End Sub

    Sub M()
        On Error GoTo H
        Boom()
Cleanup:
        Boom()
        Exit Sub
H:
        attempts += 1
        If attempts > 3 Then Exit Sub
        On Error GoTo 0
        Resume Cleanup
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    private int attempts;

    public void Boom()
    {
        throw new Exception();
    }

    public void M()
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo H' at character 149


                Input:
                        On Error GoTo H

                 */
        Boom();
    Cleanup:
        ;

        Boom();
        return;
    H:
        ;

        attempts += 1;
        if (attempts > 3)
            return;
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToZeroStatement not implemented, please report this issue in 'On Error GoTo 0' at character 312


        Input:
                On Error GoTo 0

         */
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Cleanup' at character 337


                Input:
                        Resume Cleanup

                 */
    }
}----- Exception 1 of 3 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo H' at character 149

----- Exception 2 of 3 -----
CONVERSION ERROR: Conversion for OnErrorGoToZeroStatement not implemented, please report this issue in 'On Error GoTo 0' at character 312

----- Exception 3 of 3 -----
CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Cleanup' at character 337", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// The handler runs into Done without a GoTo, and the covered code also jumps there: the label would be reached from the try as well as from the catch.
    /// </summary>
    [Fact]
    public async Task HandlerFallingIntoALabelTheCoveredCodeJumpsToKeepsTheFallbackAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub Boom()
        Throw New System.Exception()
    End Sub

    Sub M(flag As Boolean)
        On Error GoTo H
        Boom()
        If flag Then GoTo Done
        Exit Sub
H:
        Boom()
Done:
        Boom()
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void Boom()
    {
        throw new Exception();
    }

    public void M(bool flag)
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo H' at character 129


                Input:
                        On Error GoTo H

                 */
        Boom();
        if (flag)
            goto Done;
        return;
    H:
        ;

        Boom();
    Done:
        ;

        Boom();
    }
}CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo H' at character 129", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// The handler turns error handling off and then retries from a label in the covered code, so VB protects the first pass through the code after the label but not the retry.
    /// The plain form would leave that code outside the try, so even the first error would escape; the segmented form would protect the retry.
    /// </summary>
    [Fact]
    public async Task OneShotRetryFromALabelInTheCoveredCodeKeepsTheFallbackWhenTheCodeAfterTheLabelCanFailAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub OpenIt()
    End Sub

    Sub Work()
        Throw New System.Exception()
    End Sub

    Sub OneShotRetry()
        On Error GoTo EH
        OpenIt()
Again:
        Work()
        Exit Sub
EH:
        On Error GoTo 0
        Resume Again
    End Sub
End Class", @"using System;

internal partial class TestClass
{
    public void OpenIt()
    {
    }

    public void Work()
    {
        throw new Exception();
    }

    public void OneShotRetry()
    {
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo EH' at character 158


                Input:
                        On Error GoTo EH

                 */
        OpenIt();
    Again:
        ;

        Work();
        return;
    EH:
        ;
        ;
#error Cannot convert OnErrorGoToStatementSyntax - see comment for details
        /* Cannot convert OnErrorGoToStatementSyntax, CONVERSION ERROR: Conversion for OnErrorGoToZeroStatement not implemented, please report this issue in 'On Error GoTo 0' at character 255


        Input:

                On Error GoTo 0

         */
        ;
#error Cannot convert ResumeStatementSyntax - see comment for details
        /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Again' at character 280


                Input:
                        Resume Again

                 */
    }
}----- Exception 1 of 3 -----
CONVERSION ERROR: Conversion for OnErrorGoToLabelStatement not implemented, please report this issue in 'On Error GoTo EH' at character 158

----- Exception 2 of 3 -----
CONVERSION ERROR: Conversion for OnErrorGoToZeroStatement not implemented, please report this issue in 'On Error GoTo 0' at character 255

----- Exception 3 of 3 -----
CONVERSION ERROR: Conversion for ResumeLabelStatement not implemented, please report this issue in 'Resume Again' at character 280", incompatibleWithAutomatedCommentTesting: true);
    }

    /// <summary>
    /// A cleanup after the handler that tests what it frees: the condition in the tail (not the handler) reads `conn`, which the tail assigns. That isn't a re-entry guard, since a second pass would do the same again, so the tail stays plain.
    /// </summary>
    [Fact]
    public async Task CleanupTailThatTestsAndClearsWhatItFreesIsPlainStructuredAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub Work()
    End Sub

    Sub Close(c As Object)
    End Sub

    Sub CleanupTail()
        Dim conn As Object = Nothing
        On Error GoTo EH
        conn = New Object() : Work()
        GoTo Cleanup
EH:
        Console.WriteLine(Err.Description)
        Resume Cleanup
Cleanup:
        If conn IsNot Nothing Then Close(conn)
        conn = Nothing
    End Sub
End Class", @"using System;
using Microsoft.VisualBasic; // Install-Package Microsoft.VisualBasic
using Microsoft.VisualBasic.CompilerServices; // Install-Package Microsoft.VisualBasic

internal partial class TestClass
{
    public void Work()
    {
    }

    public void Close(object c)
    {
    }

    public void CleanupTail()
    {
        object conn = null;
        ProjectData.ClearProjectError();
        try
        {
            conn = new object();
            Work();
            goto Cleanup;
        }
        catch (Exception ex)
        {
            ProjectData.SetProjectError(ex);

            Console.WriteLine(Information.Err().Description);
            ProjectData.ClearProjectError();
        }

    Cleanup:
        ;

        if (conn is not null)
            Close(conn);
        conn = null;
    }
}");
    }

    /// <summary>
    /// The handler sets the result and the tail tests it: the condition is only in the tail, and nothing in the tail re-assigns what it reads.
    /// </summary>
    [Fact]
    public async Task TailThatTestsTheResultTheHandlerSetIsPlainStructuredAsync()
    {
        await TestConversionVisualBasicToCSharpAsync(@"Class TestClass
    Sub Work()
    End Sub

    Function F() As Integer
        On Error GoTo EH
        Work() : F = 1 : Exit Function
EH:
        F = 2
        Resume Fin
Fin:
        If F > 1 Then Exit Function
        Console.WriteLine(""fin"")
    End Function
End Class", @"using System;

internal partial class TestClass
{
    public void Work()
    {
    }

    public int F()
    {
        int FRet = default;
        try
        {
            Work();
            FRet = 1;
            return FRet;
        }
        catch
        {

            FRet = 2;
        }

        if (FRet > 1)
            return FRet;
        Console.WriteLine(""fin"");
        return FRet;
    }
}");
    }
}
