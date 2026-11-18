Imports System
Imports System.Collections.Generic
Imports Xunit

''' <summary>
''' The handler function shape in a type that never reads Err: the catches are bare.
''' </summary>
Public Class OnErrorHandlerFunctionWithoutErrTests
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

    Private Sub Handled()
        On Error GoTo H
        Boom("a")
        L("b")
        Exit Sub
H:
        L("handler")
        Resume Next
    End Sub

    <Fact>
    Sub BareCatchCallsTheHandler()
        Handled()
        Assert.Equal("eval a, handler, b", Logged())
    End Sub

    Private Sub Retried()
        Dim tries = 0
        On Error GoTo H
        Boom("a" & tries)
        Exit Sub
H:
        tries += 1
        If tries < 3 Then Resume
    End Sub

    <Fact>
    Sub RetryWithAHandlerThatFallsOffTheEndLeavesTheMethod()
        Retried()
        Assert.Equal("eval a0, eval a1, eval a2", Logged())
    End Sub
End Class
