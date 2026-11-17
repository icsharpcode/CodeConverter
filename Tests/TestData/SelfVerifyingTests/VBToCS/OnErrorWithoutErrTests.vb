Imports System
Imports System.Collections.Generic
Imports Xunit

Public Class OnErrorWithoutErrTests
    Private ReadOnly _log As New List(Of String)

    Private Sub Work(succeed As Boolean)
        _log.Add("work")
        If Not succeed Then Throw New InvalidOperationException("work failed")
    End Sub

    Private Function Saved(succeed As Boolean) As Boolean
        On Error GoTo Failed
        Dim attempt = 1
        Work(succeed)
        Return True
Failed:
        _log.Add("failed attempt " & attempt)
        Return False
    End Function

    <Fact>
    Sub BareCatchRunsHandler()
        Assert.False(Saved(False))
        Assert.True(Saved(True))
        Assert.Equal("work, failed attempt 1, work", String.Join(", ", _log))
    End Sub

    Private Sub WithCleanup(succeed As Boolean)
        On Error GoTo Failed
        Work(succeed)
Cleanup:
        Dim message = "cleanup"
        _log.Add(message)
        Exit Sub
Failed:
        _log.Add("failed " & message)
        Resume Cleanup
    End Sub

    <Fact>
    Sub ResumeToCleanup()
        WithCleanup(False)
        WithCleanup(True)
        Assert.Equal("work, failed , cleanup, work, cleanup", String.Join(", ", _log))
    End Sub
End Class
