Imports System
Imports System.Collections.Generic
Imports Xunit

''' <summary>
''' Methods that ignore errors with On Error Resume Next: each statement that fails is skipped, and the log shows exactly which statements ran.
''' </summary>
Public Class OnErrorResumeNextTests
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

    Private Class Resource
        Implements IDisposable

        Private ReadOnly _log As List(Of String)

        Public Sub New(log As List(Of String))
            _log = log
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _log.Add("dispose")
        End Sub
    End Class

    Private Sub IfCondition()
        On Error Resume Next
        If Boom("if") = 1 Then
            L("in if body")
        Else
            L("in else body")
        End If
        L("after if")
    End Sub

    <Fact>
    Sub FailingIfConditionResumesInTheBranch()
        IfCondition()
        Assert.Equal("eval if, in if body, after if", Logged())
    End Sub

    Private Sub ElseIfCondition()
        On Error Resume Next
        If Ok("if", 0) = 1 Then
            L("in if body")
        ElseIf Boom("elseif") = 1 Then
            L("in elseif body")
        Else
            L("in else body")
        End If
        L("after if")
    End Sub

    <Fact>
    Sub FailingElseIfConditionResumesInItsBranch()
        ElseIfCondition()
        Assert.Equal("eval if, eval elseif, in elseif body, after if", Logged())
    End Sub

    Private Sub SingleLineIf()
        On Error Resume Next
        If Boom("if") = 1 Then L("in if body") Else L("in else body")
        L("after if")
    End Sub

    <Fact>
    Sub FailingSingleLineIfConditionResumesInTheBranch()
        SingleLineIf()
        Assert.Equal("eval if, in if body, after if", Logged())
    End Sub

    Private Sub WhileCondition()
        On Error Resume Next
        Dim n = 0
        While If(n < 2, Boom("while"), 0) = 1
            n += 1
            L("body " & n)
        End While
        L("after while")
    End Sub

    <Fact>
    Sub FailingWhileConditionEntersTheBody()
        WhileCondition()
        Assert.Equal("eval while, body 1, eval while, body 2, after while", Logged())
    End Sub

    Private Sub DoUntilCondition()
        On Error Resume Next
        Dim n = 0
        Do Until If(n >= 2, 1, Boom("until")) = 1
            n += 1
            L("body " & n)
        Loop
        L("after do")
    End Sub

    <Fact>
    Sub FailingDoUntilConditionEntersTheBody()
        DoUntilCondition()
        Assert.Equal("eval until, body 1, eval until, body 2, after do", Logged())
    End Sub

    Private Sub LoopUntilCondition()
        On Error Resume Next
        Dim n = 0
        Do
            n += 1
            L("body " & n)
        Loop Until If(n < 2, Boom("until"), 1) = 1
        L("after do")
    End Sub

    <Fact>
    Sub FailingLoopUntilConditionLeavesTheLoop()
        LoopUntilCondition()
        Assert.Equal("body 1, eval until, after do", Logged())
    End Sub

    Private Sub LoopWhileCondition()
        On Error Resume Next
        Dim n = 0
        Do
            n += 1
            L("body " & n)
        Loop While If(n < 2, Ok("while", 1), Boom("while")) = 1
        L("after do")
    End Sub

    <Fact>
    Sub FailingLoopWhileConditionLeavesTheLoop()
        LoopWhileCondition()
        Assert.Equal("body 1, eval while, body 2, eval while, after do", Logged())
    End Sub

    Private Sub StatementsInALoopBody()
        On Error Resume Next
        For i = 1 To 2
            L("a " & i)
            Boom("b" & i)
            L("c " & i)
        Next
        L("after for")
    End Sub

    <Fact>
    Sub FailingStatementInALoopBodyResumesWithTheNextStatement()
        StatementsInALoopBody()
        Assert.Equal("a 1, eval b1, c 1, a 2, eval b2, c 2, after for", Logged())
    End Sub

    Private Sub SelectHeader()
        On Error Resume Next
        Select Case Boom("select")
            Case 1
                L("case 1")
            Case Else
                L("case else")
        End Select
        L("after select")
    End Sub

    <Fact>
    Sub FailingSelectExpressionResumesAfterTheBlock()
        SelectHeader()
        Assert.Equal("eval select, after select", Logged())
    End Sub

    Private Sub SelectBody(value As Integer)
        On Error Resume Next
        Select Case value
            Case 1
                Boom("one")
                L("after one")
            Case Else
                L("other")
        End Select
        L("after select")
    End Sub

    <Fact>
    Sub FailingStatementInASelectBodyResumesInTheBody()
        SelectBody(1)
        Assert.Equal("eval one, after one, after select", Logged())
    End Sub

    Private Sub UsingBody()
        On Error Resume Next
        Using resource As New Resource(_log)
            L("using a")
            Boom("using")
            L("using c")
        End Using
        L("after using")
    End Sub

    <Fact>
    Sub FailureInAUsingBodyResumesAfterTheBlockAfterDisposing()
        UsingBody()
        Assert.Equal("using a, eval using, dispose, after using", Logged())
    End Sub

    Private Sub SyncLockBody()
        On Error Resume Next
        SyncLock _log
            L("lock a")
            Boom("lock")
            L("lock c")
        End SyncLock
        L("after lock")
    End Sub

    <Fact>
    Sub FailureInASyncLockBodyResumesAfterTheBlock()
        SyncLockBody()
        Assert.Equal("lock a, eval lock, after lock", Logged())
    End Sub

    Private Sub MultipleDeclarators()
        On Error Resume Next
        Dim a = Boom("a"), b = Ok("b", 2)
        L("a=" & a & " b=" & b)
    End Sub

    <Fact>
    Sub EachDeclaratorIsItsOwnResumePointAndTheLocalsStayInScope()
        MultipleDeclarators()
        Assert.Equal("eval a, eval b, a=0 b=2", Logged())
    End Sub

    Private Function ReturnThatFails() As Integer
        On Error Resume Next
        ReturnThatFails = 7
        Return Boom("ret")
        L("after return")
    End Function

    <Fact>
    Sub FailingReturnFallsThroughToTheReturnVariable()
        Assert.Equal(7, ReturnThatFails())
        Assert.Equal("eval ret, after return", Logged())
    End Sub

    Private Function OnlyReturnFails() As Integer
        On Error Resume Next
        Return Boom("only")
    End Function

    <Fact>
    Sub FailingReturnInAFunctionWithoutAReturnVariableReturnsTheDefault()
        Assert.Equal(0, OnlyReturnFails())
        Assert.Equal("eval only", Logged())
    End Sub

    Private Sub NestedBlocks(limit As Integer)
        On Error Resume Next
        For i = 1 To limit
            If i = 1 Then
                Dim value As Integer = Boom("nested")
                L("value " & value)
            End If
            L("end of " & i)
        Next
    End Sub

    <Fact>
    Sub DeclarationsInNestedBlocksAreGuardedToo()
        NestedBlocks(2)
        Assert.Equal("eval nested, value 0, end of 1, end of 2", Logged())
    End Sub

    Private Sub ErrorsStopBeingIgnoredAfterGoTo0()
        On Error Resume Next
        Boom("a")
        On Error GoTo 0
        L("after")
        Boom("b")
        L("unreachable")
    End Sub

    <Fact>
    Sub OnErrorGoTo0EndsTheGuarding()
        Assert.Throws(Of InvalidOperationException)(Sub() ErrorsStopBeingIgnoredAfterGoTo0())
        Assert.Equal("eval a, after, eval b", Logged())
    End Sub

    Private Sub StatementsBeforeOnErrorAreNotGuarded()
        L("before")
        Boom("early")
        On Error Resume Next
        L("unreachable")
    End Sub

    <Fact>
    Sub StatementsBeforeOnErrorPropagate()
        Assert.Throws(Of InvalidOperationException)(Sub() StatementsBeforeOnErrorAreNotGuarded())
        Assert.Equal("before, eval early", Logged())
    End Sub
End Class
