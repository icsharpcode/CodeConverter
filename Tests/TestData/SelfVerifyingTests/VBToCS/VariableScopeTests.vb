Imports System
Imports System.Linq
Imports Xunit

Public Class VariableScopeTests

    <Fact>
    Sub TestDeclarationInsideForLoop()
        For i = 1 To 2
            Dim b As Boolean
            If i = 1 Then
                Assert.False(b)
            Else
                Assert.True(b)
            End If

            b = True
            Assert.True(b)
        Next
    End Sub

    <Fact>
    Sub TestDeclarationsInsideNestedLoops()
        Dim results As New List(Of String)
        Dim i = 1
        Do
            Dim b As Integer
            b += 1
            results.Add("b=" & b)
            For j = 1 To 3
                Dim c As Integer
                c += 1
                results.Add("c1=" & c)
            Next
            For j = 1 To 3
                Dim c As Integer
                c += 1
                results.Add("c2=" & c)
            Next
            Dim k = 1
            Do While k <= 3
                Dim c As Integer
                c += 1
                results.Add("c3=" & c)
                k += 1
            Loop
            i += 1
        Loop Until i > 3
        Assert.Equal(
            "b=1, c1=1, c1=2, c1=3, c2=1, c2=2, c2=3, c3=1, c3=2, c3=3, " &
            "b=2, c1=4, c1=5, c1=6, c2=4, c2=5, c2=6, c3=4, c3=5, c3=6, " &
            "b=3, c1=7, c1=8, c1=9, c2=7, c2=8, c2=9, c3=7, c3=8, c3=9",
            String.Join(", ", results))
    End Sub

    <Fact>
    Sub TestMultipleVariablesDefinedInOneDeclarationStatement()
        For i = 1 To 2
            Dim a = True, b As Boolean, c As Integer? = Nothing, d = 4, e As New Integer(), f As Integer?
            a = Not a
            b = Not b
            c = If(Not c.HasValue, 0, c + 1)
            d += 1
            e += 1
            f = If(Not f.HasValue, 0, f + 1)

            If i = 2 Then
                Assert.Equal(False, a)
                Assert.Equal(False, b)
                Assert.Equal(0, c)
                Assert.Equal(5, d)
                Assert.Equal(1, e)
                Assert.Equal(1, f)
            End If
        Next
    End Sub

    Private _loneDeclarationLog As New List(Of String)

    Private Function LogLoneDeclaration(name As String) As Integer
        _loneDeclarationLog.Add(name)
        Return 1
    End Function

    <Fact>
    Sub TestLoneDeclarationAsBlockBodyStillRuns()
        Dim i = 0
        While i < 1
            Dim z = LogLoneDeclaration("While") + Increment(i)
        End While
        Do
            Dim z = LogLoneDeclaration("DoLoopWhile")
        Loop While False
        For j = 1 To 2
            Dim z = LogLoneDeclaration("For")
        Next
        For Each x In {1}
            Dim z = LogLoneDeclaration("ForEach")
        Next
        SyncLock _loneDeclarationLog
            Dim z = LogLoneDeclaration("SyncLock")
        End SyncLock
        Using d As IDisposable = New IO.MemoryStream()
            Using d
                Dim z = LogLoneDeclaration("Using")
            End Using
        End Using
        If True Then Dim z = LogLoneDeclaration("If") Else Dim y = LogLoneDeclaration("NotElse")
        If False Then Dim z = LogLoneDeclaration("NotIf") Else Dim y = LogLoneDeclaration("Else")
        Assert.Equal({"While", "DoLoopWhile", "For", "For", "ForEach", "SyncLock", "Using", "If", "Else"}, _loneDeclarationLog)
    End Sub

    Private Shared Function Increment(ByRef i As Integer) As Integer
        i += 1
        Return i
    End Function

End Class
