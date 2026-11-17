Imports System
Imports System.Collections.Generic
Imports Xunit

''' <summary>
''' On Error GoTo a handler that resumes: the handler becomes a function that every guard calls, and Resume / Resume Next / leaving decide what happens next.
''' </summary>
Public Class OnErrorHandlerFunctionTests
    Private ReadOnly _log As New List(Of String)

    Private Sub L(message As String)
        _log.Add(message)
    End Sub

    Private Function Boom(name As String) As Integer
        L("eval " & name)
        Throw New InvalidOperationException(name)
    End Function

    Private Function Ok(name As String, value As Integer) As Integer
        L("eval " & name)
        Return value
    End Function

    Private Function Logged() As String
        Return String.Join(", ", _log)
    End Function

    Private Sub ResumeNextHandler()
        On Error GoTo H
        L("a")
        Boom("b")
        L("c")
        Boom("d")
        L("e")
        Exit Sub
H:
        L("handler " & Err.Description)
        Resume Next
    End Sub

    <Fact>
    Sub HandlerRunsForEachFailureAndExecutionResumesWithTheNextStatement()
        Err.Clear()
        ResumeNextHandler()
        Assert.Equal("a, eval b, handler b, c, eval d, handler d, e", Logged())
        Assert.Equal(0, Err.Number)
    End Sub

    Private Sub ResumeNextInALoop()
        On Error GoTo H
        For i = 1 To 2
            L("a " & i)
            Boom("b" & i)
            L("c " & i)
        Next
        Exit Sub
H:
        L("handler " & Err.Description)
        Resume Next
    End Sub

    <Fact>
    Sub HandlerResumesInsideALoopBody()
        ResumeNextInALoop()
        Assert.Equal("a 1, eval b1, handler b1, c 1, a 2, eval b2, handler b2, c 2", Logged())
    End Sub

    Private Sub RetryHandler()
        Dim tries = 0
        On Error GoTo H
        L("a")
        Ok("pre", 0) : If tries < 2 Then Boom("b" & tries)
        L("c")
        Exit Sub
H:
        tries += 1
        L("handler " & Err.Description)
        If tries > 3 Then Exit Sub
        Resume
    End Sub

    <Fact>
    Sub ResumeRunsTheFailingStatementAgainWithoutRetestingItsCondition()
        RetryHandler()
        Assert.Equal("a, eval pre, eval b0, handler b0, eval b1, handler b1, eval b2, handler b2, eval b3, handler b3", Logged())
    End Sub

    Private Sub ResumeNextOrRetry()
        Dim tries = 0
        On Error GoTo H
        Boom("first")
        L("second")
        Exit Sub
H:
        tries += 1
        If tries < 3 Then Resume
        Resume Next
    End Sub

    <Fact>
    Sub HandlerThatRetriesTwiceThenResumesNext()
        ResumeNextOrRetry()
        Assert.Equal("eval first, eval first, eval first, second", Logged())
    End Sub

    Private Sub ResumeNextOrExit()
        Dim errors = 0
        On Error GoTo H
        Boom("a")
        L("b")
        Boom("c")
        L("d")
        Exit Sub
H:
        errors += 1
        L("handler " & errors)
        If errors >= 2 Then Exit Sub
        Resume Next
    End Sub

    <Fact>
    Sub HandlerThatResumesNextThenLeavesTheMethod()
        ResumeNextOrExit()
        Assert.Equal("eval a, handler 1, b, eval c, handler 2", Logged())
        Assert.Equal(0, Err.Number)
    End Sub

    Private Function FunctionWithValueFromHandler(tries As Integer) As Integer
        Dim attempts = 0
        On Error GoTo H
        If attempts < tries Then Boom("work")
        Return 100
H:
        attempts += 1
        If attempts < 3 Then Resume
        Return -attempts
    End Function

    <Fact>
    Sub ReturnInTheHandlerReturnsItsValueFromTheFunction()
        Assert.Equal(-3, FunctionWithValueFromHandler(5))
        Assert.Equal("eval work, eval work, eval work", Logged())
    End Sub

    <Fact>
    Sub FunctionThatNeverFailsIsUnaffected()
        Assert.Equal(100, FunctionWithValueFromHandler(0))
        Assert.Equal("", Logged())
    End Sub

    Private Function FunctionResultFromNameAndExit() As Integer
        FunctionResultFromNameAndExit = 5
        On Error GoTo H
        Boom("a")
        FunctionResultFromNameAndExit = 6
        Exit Function
H:
        L("handler")
        If Err.Number = 5 Then Exit Function
        Resume Next
    End Function

    <Fact>
    Sub ExitFunctionInTheHandlerReturnsTheReturnVariable()
        Assert.Equal(5, FunctionResultFromNameAndExit())
        Assert.Equal("eval a, handler", Logged())
    End Sub

    Private Sub HandlerThatThrows()
        On Error GoTo H
        Boom("a")
        L("b")
        Exit Sub
H:
        L("handler")
        Boom("in handler")
        Resume Next
    End Sub

    <Fact>
    Sub ErrorInTheHandlerPropagates()
        Assert.Throws(Of InvalidOperationException)(Sub() HandlerThatThrows())
        Assert.Equal("eval a, handler, eval in handler", Logged())
    End Sub

    Private Sub ConditionsAndDeclarations()
        On Error GoTo H
        If Boom("if") = 1 Then
            L("in if body")
        End If
        Dim value As Integer = Boom("value")
        L("value " & value & " " & seen)
        Exit Sub
H:
        seen += 1
        L("handler " & seen)
        Resume Next
    End Sub

    Private seen As Integer

    <Fact>
    Sub FailingConditionResumesInTheBranchAndLocalsStayInScope()
        ConditionsAndDeclarations()
        Assert.Equal("eval if, handler 1, in if body, eval value, handler 2, value 0 2", Logged())
    End Sub

    Private Sub LoopConditionRetries()
        Dim n = 0
        Dim failures = 0
        On Error GoTo H
        While If(failures < 2, Boom("while"), 0) = 1
            n += 1
            If n > 5 Then Exit While
        End While
        L("n " & n & " failures " & failures)
        Exit Sub
H:
        failures += 1
        Resume Next
    End Sub

    <Fact>
    Sub FailingLoopConditionCallsTheHandlerAndEntersTheBody()
        LoopConditionRetries()
        Assert.Equal("eval while, eval while, n 2 failures 2", Logged())
    End Sub

    Private Sub HandlerLocalsAreNotShared()
        Dim total = 0
        On Error GoTo H
        total += Boom("a")
        L("total " & total)
        Exit Sub
H:
        Dim note = "failed"
        L(note)
        Resume Next
    End Sub

    <Fact>
    Sub LocalsDeclaredInTheHandlerStayInTheHandler()
        HandlerLocalsAreNotShared()
        Assert.Equal("eval a, failed, total 0", Logged())
    End Sub

    Private Sub ErrorsAfterGoTo0Propagate()
        On Error GoTo H
        Boom("a")
        On Error GoTo 0
        Boom("b")
        Exit Sub
H:
        L("handler")
        Resume Next
    End Sub

    <Fact>
    Sub OnErrorGoTo0StopsTheHandlerBeingCalled()
        Assert.Throws(Of InvalidOperationException)(Sub() ErrorsAfterGoTo0Propagate())
        Assert.Equal("eval a, handler, eval b", Logged())
    End Sub

    Private Function HandlerWithALambdaReturn() As Integer
        On Error GoTo H
        Boom("a")
        Return 1
H:
        Dim twice As Func(Of Integer, Integer) = Function(x)
                                                     Return x * 2
                                                 End Function
        If twice(1) = 3 Then Resume Next
        Return twice(21)
    End Function

    <Fact>
    Sub ReturnsInsideALambdaInTheHandlerAreNotTheMethodsReturns()
        Assert.Equal(42, HandlerWithALambdaReturn())
        Assert.Equal("eval a", Logged())
    End Sub
End Class
