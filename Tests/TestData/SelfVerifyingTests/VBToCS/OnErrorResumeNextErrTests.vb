Imports System
Imports System.Collections.Generic
Imports Xunit

''' <summary>
''' On Error Resume Next in a type that reads Err: the converted catches must record each error, and On Error statements must clear it.
''' </summary>
Public Class OnErrorResumeNextErrTests
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

    Private Function Deep() As Integer
        Return Boom("deep")
    End Function

    Private Function Logged() As String
        Return String.Join(", ", _log)
    End Function

    Private Sub InspectsErr()
        On Error Resume Next
        Dim x = Deep()
        L("err " & Err.Number & " " & Err.Description)
        Dim y = Ok("y", 1)
        L("err still " & Err.Number)
    End Sub

    <Fact>
    Sub ErrIsSetByAnIgnoredErrorAndStaysSetAfterTheMethodReturns()
        Err.Clear()
        InspectsErr()
        Assert.Equal("eval deep, err 5 deep, eval y, err still 5", Logged())
        Assert.Equal(5, Err.Number)
        Err.Clear()
    End Sub

    Private Sub OnErrorClearsErr()
        On Error Resume Next
        Boom("a")
        L("err " & Err.Number)
        On Error Resume Next
        L("err after on error " & Err.Number)
    End Sub

    <Fact>
    Sub OnErrorStatementClearsErr()
        Err.Clear()
        OnErrorClearsErr()
        Assert.Equal("eval a, err 5, err after on error 0", Logged())
        Err.Clear()
    End Sub

    Private Sub GoTo0ClearsErr()
        On Error Resume Next
        Boom("a")
        On Error GoTo 0
        L("err after goto0 " & Err.Number)
    End Sub

    <Fact>
    Sub OnErrorGoTo0ClearsErr()
        Err.Clear()
        GoTo0ClearsErr()
        Assert.Equal("eval a, err after goto0 0", Logged())
    End Sub

    Private Function ConditionAndReturnReadErr() As Integer
        On Error Resume Next
        If Boom("if") = 1 Then
            L("body err " & Err.Number)
        End If
        Return Boom("ret")
    End Function

    <Fact>
    Sub GuardedConditionsAndReturnsRecordErr()
        Err.Clear()
        Assert.Equal(0, ConditionAndReturnReadErr())
        Assert.Equal("eval if, body err 5, eval ret", Logged())
        Assert.Equal(5, Err.Number)
        Err.Clear()
    End Sub
End Class
