Imports System
Imports System.Collections.Generic
Imports System.IO
Imports Xunit

''' <summary>
''' A body whose only statement is a declaration must still run its initializer each time (C# can't embed a declaration without braces).
''' </summary>
Public Class LoneDeclarationBodyTests
    Private ReadOnly _log As New List(Of String)

    Private Function Log(s As String) As Integer
        _log.Add(s)
        Return _log.Count
    End Function

    <Fact>
    Public Sub LoneDeclarationBodiesRunTheirInitializers()
        Dim a = True
        If a Then Dim v = Log("if")
        Dim n = 0
        While n < 2
            Dim y = Log("while" & n) + Math.Min(0, Threading.Interlocked.Increment(n))
        End While
        Do
            Dim z = Log("do")
        Loop While False
        For i = 1 To 2
            Dim i2 = Log("for" & i)
        Next
        For Each x In {"a", "b"}
            Dim x2 = Log("each" & x)
        Next
        SyncLock _log
            Dim l = Log("lock")
        End SyncLock
        Using New MemoryStream()
            Dim u = Log("using")
        End Using
        Assert.Equal("if,while0,while1,do,for1,for2,eacha,eachb,lock,using", String.Join(",", _log))
    End Sub
End Class
