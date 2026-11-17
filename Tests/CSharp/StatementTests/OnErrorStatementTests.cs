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
    public async Task HandlerThatResumesNextKeepsPendingOutputAsync()
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
        try
        {
            Console.WriteLine(""saving"");
            return;
        }
        catch
        {
            ;
#error Cannot convert ResumeStatementSyntax - see comment for details
            /* Cannot convert ResumeStatementSyntax, CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 172


            Input:

                    Resume Next

             */
        }
    }
}CONVERSION ERROR: Conversion for ResumeNextStatement not implemented, please report this issue in 'Resume Next' at character 172
", incompatibleWithAutomatedCommentTesting: true);
    }
}