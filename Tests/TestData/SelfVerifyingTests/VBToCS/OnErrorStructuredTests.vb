Imports System
Imports System.Collections.Generic
Imports Xunit

Public Class OnErrorStructuredTests
    Private ReadOnly _log As New List(Of String)
    Private _closeFailuresLeft As Integer

    Private Sub Work(succeed As Boolean)
        _log.Add("work")
        If Not succeed Then Throw New InvalidOperationException("work failed")
    End Sub

    Private Sub Close()
        _log.Add("close")
        If _closeFailuresLeft > 0 Then
            _closeFailuresLeft -= 1
            Throw New InvalidOperationException("close failed")
        End If
    End Sub

    Private Function Logged() As String
        Return String.Join(", ", _log)
    End Function

    Private Sub HandlerExits(succeed As Boolean)
        On Error GoTo Failed
        Work(succeed)
        _log.Add("after work")
        Exit Sub
Failed:
        _log.Add("handler " & Err.Number & " " & Err.Description)
    End Sub

    <Fact>
    Sub HandlerSeesErrAndErrIsClearedOnLeaving()
        HandlerExits(False)
        Assert.Equal("work, handler 5 work failed", Logged())
        Assert.Equal(0, Err.Number)
        _log.Clear()
        HandlerExits(True)
        Assert.Equal("work, after work", Logged())
    End Sub

    Private Function SelfDivisionPossible(x As Integer) As Boolean
        On Error GoTo ErrorHandler
        Dim i As Integer = x / x
        Return True
ErrorHandler:
        Return Err.Number = 6
    End Function

    <Fact>
    Sub HandlerReturnsValueReadFromErr()
        Assert.True(SelfDivisionPossible(0))
        Assert.Equal(0, Err.Number)
        Assert.True(SelfDivisionPossible(3))
    End Sub

    Private Function LastStage(succeed As Boolean) As String
        On Error GoTo Failed
        Dim stage = "started"
        Work(succeed)
        stage = "worked"
        Dim count As Integer = 1
Failed:
        Return stage & " " & count
    End Function

    <Fact>
    Sub LocalsDeclaredInCoveredCodeAreVisibleToTheHandler()
        Assert.Equal("started 0", LastStage(False))
        Assert.Equal("worked 1", LastStage(True))
    End Sub

    Private Sub CleanupWithCounterGuard()
        Dim attempts = 0
        On Error GoTo Failed
        Work(False)
Cleanup:
        _log.Add("cleanup")
        Close()
        _log.Add("closed")
        Exit Sub
Failed:
        attempts += 1
        _log.Add("handler " & attempts & " " & Err.Description)
        If attempts > 3 Then Exit Sub
        Resume Cleanup
    End Sub

    <Fact>
    Sub CleanupErrorReentersHandlerWithCounterGuard()
        _closeFailuresLeft = 1
        CleanupWithCounterGuard()
        Assert.Equal("work, handler 1 work failed, cleanup, close, handler 2 close failed, cleanup, close, closed", Logged())
        Assert.Equal(0, Err.Number)
    End Sub

    <Fact>
    Sub CounterGuardStopsReentry()
        _closeFailuresLeft = 10
        CleanupWithCounterGuard()
        Assert.Equal("work, handler 1 work failed, cleanup, close, handler 2 close failed, cleanup, close, handler 3 close failed, cleanup, close, handler 4 close failed", Logged())
        Assert.Equal(0, Err.Number)
    End Sub

    Private Sub CleanupWithFlagGuard()
        Dim cleaningUp As Boolean
        On Error GoTo Failed
        Work(False)
Cleanup:
        cleaningUp = True
        Close()
        _log.Add("closed")
        Exit Sub
Failed:
        _log.Add("handler " & cleaningUp)
        If cleaningUp Then Exit Sub
        Resume Cleanup
    End Sub

    <Fact>
    Sub FlagGuardStopsReentry()
        _closeFailuresLeft = 10
        CleanupWithFlagGuard()
        Assert.Equal("work, handler False, close, handler True", Logged())
    End Sub

    Private Function Retried(retry As Boolean) As Integer
        On Error GoTo Failed
        Work(False)
Done:
        _log.Add("done")
        Retried = 1
        Exit Function
Failed:
        If retry Then Resume Done
        _log.Add("gave up " & Err.Number)
        Retried = -1
    End Function

    <Fact>
    Sub HandlerThatFallsOffTheEndLeavesTheMethod()
        Assert.Equal(-1, Retried(False))
        Assert.Equal("work, gave up 5", Logged())
        Assert.Equal(0, Err.Number)
        _log.Clear()
        Assert.Equal(1, Retried(True))
        Assert.Equal("work, done", Logged())
    End Sub

    Private Sub ReachesHandlerLabel(succeed As Boolean)
        On Error GoTo Finish
        Work(succeed)
        On Error GoTo 0
        _log.Add("worked")
Finish:
        _log.Add("finish " & Err.Number)
    End Sub

    <Fact>
    Sub HandlerLabelReachedWithAndWithoutError()
        ReachesHandlerLabel(True)
        Assert.Equal("work, worked, finish 0", Logged())
        _log.Clear()
        ReachesHandlerLabel(False)
        Assert.Equal("work, finish 5", Logged())
    End Sub

    Private Function FirstNegative(values As Integer()) As Integer
        On Error GoTo Failed
        For Each v In values
            If v < 0 Then
                Return v
            End If
        Next
        Return 0
Failed:
        Return -100
    End Function

    <Fact>
    Sub ReturnFromNestedBlocksInCoveredCode()
        Assert.Equal(-2, FirstNegative({1, -2, 3}))
        Assert.Equal(0, FirstNegative({1}))
        Assert.Equal(-100, FirstNegative(Nothing))
    End Sub

    Private Sub GoesToReport()
        On Error GoTo Failed
        Work(False)
        Exit Sub
Report:
        _log.Add("report " & Err.Number)
        Exit Sub
Failed:
        _log.Add("failed")
        GoTo Report
    End Sub

    <Fact>
    Sub HandlerGoToKeepsErr()
        GoesToReport()
        Assert.Equal("work, failed, report 5", Logged())
    End Sub

    Private Sub CleanupWithoutHandler()
        Dim attempts = 0
        On Error GoTo Failed
        Work(False)
Cleanup:
        On Error GoTo 0
        _log.Add("cleanup " & Err.Number)
        Exit Sub
Failed:
        attempts += 1
        If attempts > 3 Then Exit Sub
        _log.Add("handler " & Err.Number)
        Resume Cleanup
    End Sub

    <Fact>
    Sub CleanupStartingWithOnErrorGoToZero()
        CleanupWithoutHandler()
        Assert.Equal("work, handler 5, cleanup 0", Logged())
    End Sub

    Private Function Boom(s As String) As Integer
        _log.Add("boom " & s)
        Throw New InvalidOperationException(s)
    End Function

    Private Function HandlerSetsReturnValue() As Integer
        On Error GoTo H
        HandlerSetsReturnValue = Boom("a")
        Exit Function
H:
        HandlerSetsReturnValue = -1
    End Function

    <Fact>
    Sub FunctionWhoseHandlerFallsOffTheEndReturnsWhatItAssigned()
        Assert.Equal(-1, HandlerSetsReturnValue())
        Assert.Equal("boom a", Logged())
    End Sub

    Private Function HandlerOnlyLogs() As Integer
        On Error GoTo H
        Dim x = Boom("b")
        Return x
H:
        _log.Add("handled " & Err.Number)
    End Function

    <Fact>
    Sub FunctionWhoseHandlerOnlyLogsReturnsTheDefault()
        Assert.Equal(0, HandlerOnlyLogs())
        Assert.Equal("boom b, handled 5", Logged())
    End Sub

    Private ReadOnly Property Guarded As Integer
        Get
            On Error GoTo H
            Guarded = Boom("c")
            Exit Property
H:
            Guarded = -2
        End Get
    End Property

    <Fact>
    Sub PropertyGetWhoseHandlerFallsOffTheEndReturnsWhatItAssigned()
        Assert.Equal(-2, Guarded)
    End Sub

    Private Function HandlerSelectCanFallOff(code As Integer) As Integer
        On Error GoTo H
        Return Boom("d" & code)
H:
        Select Case code
            Case 1
                Return 11
        End Select
    End Function

    <Fact>
    Sub FunctionWhoseSelectCaseHandlerFallsOffTheEndReturnsTheDefault()
        Assert.Equal(11, HandlerSelectCanFallOff(1))
        Assert.Equal(0, HandlerSelectCanFallOff(2))
    End Sub

    Private Function ResumesToALabelAfterTheHandler(succeed As Boolean) As Integer
        Dim r = 0
        On Error GoTo H
        Work(succeed)
        r = 1
        GoTo Done
H:
        _log.Add("handler " & Err.Description)
        r = 5
        Resume Done
Done:
        _log.Add("done")
        Return r
    End Function

    <Fact>
    Sub HandlerResumingToALabelAfterItClearsErr()
        Assert.Equal(5, ResumesToALabelAfterTheHandler(False))
        Assert.Equal("work, handler work failed, done", Logged())
        Assert.Equal(0, Err.Number)
        _log.Clear()
        Assert.Equal(1, ResumesToALabelAfterTheHandler(True))
        Assert.Equal("work, done", Logged())
    End Sub

    Private Sub GoesToALabelAfterTheHandler(succeed As Boolean)
        Dim r = 0
        On Error GoTo H
        Work(succeed)
        GoTo Done
H:
        _log.Add("handler " & Err.Number)
        GoTo Done
Done:
        _log.Add("done")
    End Sub

    <Fact>
    Sub HandlerGoingToALabelAfterIt()
        GoesToALabelAfterTheHandler(False)
        Assert.Equal("work, handler 5, done", Logged())
        _log.Clear()
        GoesToALabelAfterTheHandler(True)
        Assert.Equal("work, done", Logged())
    End Sub

    Private Sub LeavesErrSet()
        On Error Resume Next
        Work(False)
    End Sub

    Private Sub LoneOnErrorGoToZero()
        If _log.Count > 100 Then Return
        On Error GoTo 0
        _log.Add("lone")
    End Sub

    <Fact>
    Sub OnErrorGoToZeroWithoutAHandlerOnlyClearsErr()
        LeavesErrSet()
        Assert.Equal(5, Err.Number)
        LoneOnErrorGoToZero()
        Assert.Equal("work, lone", Logged())
        Assert.Equal(0, Err.Number)
    End Sub

    Private Function TailEndsWithoutAReturn(succeed As Boolean) As Integer
        On Error GoTo H
        TailEndsWithoutAReturn = 1
        Work(succeed)
        Exit Function
H:
        TailEndsWithoutAReturn = 2
        Resume Done
Done:
        TailEndsWithoutAReturn += 10
    End Function

    <Fact>
    Sub TailAfterTheHandlerThatEndsTheFunction()
        Assert.Equal(12, TailEndsWithoutAReturn(False))
        Assert.Equal("work", Logged())
        Assert.Equal(1, TailEndsWithoutAReturn(True))
    End Sub

    Private Function RetriesFromALabelInTheCoveredCode() As Integer
        On Error GoTo EH
        Dim i As Integer = 0
Retry:
        i += 1
        _log.Add("try " & i)
        If i < 3 Then Work(False)
        RetriesFromALabelInTheCoveredCode = i
        Exit Function
EH:
        _log.Add("eh")
        Resume Retry
    End Function

    <Fact>
    Sub ResumingToALabelInTheCoveredCodeRetriesUntilItsOwnCounterStopsIt()
        Assert.Equal(3, RetriesFromALabelInTheCoveredCode())
        Assert.Equal("try 1, work, eh, try 2, work, eh, try 3", Logged())
    End Sub

    Private Sub HandlerTurnsOffErrorHandling(succeed As Boolean)
        On Error GoTo H
        Work(succeed)
        Exit Sub
H:
        _log.Add("handler " & Err.Number)
        On Error GoTo 0
        _log.Add("after " & Err.Number)
        Work(False)
        _log.Add("not reached")
    End Sub

    <Fact>
    Sub OnErrorGoToZeroInTheHandlerLetsLaterErrorsPropagate()
        Dim propagated = False
        Try
            HandlerTurnsOffErrorHandling(False)
        Catch ex As InvalidOperationException
            propagated = True
        End Try
        Assert.True(propagated)
        Assert.Equal("work, handler 5, after 0, work", Logged())
        _log.Clear()
        HandlerTurnsOffErrorHandling(True)
        Assert.Equal("work", Logged())
    End Sub

    Private Function TailTestsTheResultTheHandlerSet(succeed As Boolean) As Integer
        On Error GoTo EH
        Work(succeed) : TailTestsTheResultTheHandlerSet = 1 : Exit Function
EH:
        TailTestsTheResultTheHandlerSet = 2
        Resume Fin
Fin:
        If TailTestsTheResultTheHandlerSet > 1 Then Exit Function
        _log.Add("fin")
    End Function

    <Fact>
    Sub TailThatTestsTheResultTheHandlerSetRunsAfterTheHandler()
        Assert.Equal(2, TailTestsTheResultTheHandlerSet(False))
        Assert.Equal("work", Logged())
        _log.Clear()
        Assert.Equal(1, TailTestsTheResultTheHandlerSet(True))
        Assert.Equal("work", Logged())
    End Sub

    Private Sub CleanupTailThatClearsWhatItFrees(succeed As Boolean)
        Dim conn As Object = Nothing
        On Error GoTo EH
        conn = New Object() : Work(succeed)
        GoTo Cleanup
EH:
        _log.Add("handler")
        Resume Cleanup
Cleanup:
        If conn IsNot Nothing Then Close()
        conn = Nothing
    End Sub

    <Fact>
    Sub CleanupTailThatClearsWhatItFreesRunsOnceAfterTheHandler()
        CleanupTailThatClearsWhatItFrees(False)
        Assert.Equal("work, handler, close", Logged())
        _log.Clear()
        CleanupTailThatClearsWhatItFrees(True)
        Assert.Equal("work, close", Logged())
    End Sub
End Class
