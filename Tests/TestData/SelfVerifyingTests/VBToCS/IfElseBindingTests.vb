Imports System
Imports System.Collections.Generic
Imports System.IO
Imports Xunit

''' <summary>
''' Each Else must stay attached to the same If after conversion (C#'s dangling else binds to the nearest if).
''' Each test runs every combination of (outer, f1, f2) in the order FFF,FFT,FTF,FTT,TFF,TFT,TTF,TTT and logs the branches taken.
''' </summary>
Public Class IfElseBindingTests
    Private ReadOnly _log As New List(Of String)

    Private Sub Log(s As String)
        _log.Add(s)
    End Sub

    Private Function RunAllCombinations(body As Action(Of Boolean, Boolean, Boolean)) As String
        Dim results As New List(Of String)
        For Each outer In {False, True}
            For Each f1 In {False, True}
                For Each f2 In {False, True}
                    _log.Clear()
                    body(outer, f1, f2)
                    results.Add(String.Join(",", _log))
                Next
            Next
        Next
        Return String.Join("|", results)
    End Function

    <Fact>
    Public Sub SingleLineIfWithExitAsLastStatementBeforeElse_Issue823()
        Assert.Equal("end|bar2|end|bar2|end|end|bar1|bar1", RunAllCombinations(AddressOf Issue823))
    End Sub

    Private Sub Issue823(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            If f1 Then Log("bar1") : Exit Sub
        Else 'NOT OUTER
            If f2 Then
                Log("bar2")
                Exit Sub
            End If
        End If 'OUTER
        Log("end")
    End Sub

    <Fact>
    Public Sub SingleLineIfElseAsLastStatementBeforeElse()
        Assert.Equal("end|c,end|end|c,end|b,end|b,end|a,end|a,end", RunAllCombinations(AddressOf SingleLineIfElseBeforeElse))
    End Sub

    Private Sub SingleLineIfElseBeforeElse(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            If f1 Then Log("a") Else Log("b")
        Else
            If f2 Then Log("c")
        End If
        Log("end")
    End Sub

    <Fact>
    Public Sub NestedSingleLineIfElseBindsToInnerIf()
        Assert.Equal("end|end|end|end|b,end|b,end|a,end|a,end", RunAllCombinations(AddressOf NestedSingleLineIfElse))
    End Sub

    Private Sub NestedSingleLineIfElse(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then If f1 Then Log("a") Else Log("b")
        Log("end")
    End Sub

    <Fact>
    Public Sub NestedSingleLineIfWithTwoElses()
        Assert.Equal("c,end|c,end|c,end|c,end|b,end|b,end|a,end|a,end", RunAllCombinations(AddressOf NestedSingleLineIfTwoElses))
    End Sub

    Private Sub NestedSingleLineIfTwoElses(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then If f1 Then Log("a") Else Log("b") Else Log("c")
        Log("end")
    End Sub

    <Fact>
    Public Sub IfWithoutElseBeforeElseIf()
        Assert.Equal("end|c,end|end|b,end|end|end|a,end|a,end", RunAllCombinations(AddressOf IfBeforeElseIf))
    End Sub

    Private Sub IfBeforeElseIf(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            If f1 Then Log("a")
        ElseIf f2 Then
            If f1 Then Log("b") Else Log("c")
        End If
        Log("end")
    End Sub

    <Fact>
    Public Sub TwoLevelsOfIfWithoutElseBeforeElses()
        Assert.Equal("c,end|c,end|c,end|c,end|b,end|b,end|end|a,end", RunAllCombinations(AddressOf TwoLevels))
    End Sub

    Private Sub TwoLevels(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            If f1 Then
                If f2 Then Log("a")
            Else
                Log("b")
            End If
        Else
            Log("c")
        End If
        Log("end")
    End Sub

    <Fact>
    Public Sub SingleLineIfWithExitForBeforeElseInLoop()
        Assert.Equal("loop,end|b,loop,end|loop,end|b,loop,end|loop,end|loop,end|a,end|a,end", RunAllCombinations(AddressOf InForLoop))
    End Sub

    Private Sub InForLoop(outer As Boolean, f1 As Boolean, f2 As Boolean)
        For i = 1 To 1
            If outer Then
                If f1 Then Log("a") : Exit For
            Else
                If f2 Then Log("b")
            End If
            Log("loop")
        Next
        Log("end")
    End Sub

    <Fact>
    Public Sub LoopContainingIfBeforeElse()
        Assert.Equal("b,end|b,end|b,end|b,end|end|end|end|a,end", RunAllCombinations(AddressOf LoopBeforeElse))
    End Sub

    Private Sub LoopBeforeElse(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            Do While f1
                f1 = False
                If f2 Then Log("a")
            Loop
        Else
            Log("b")
        End If
        Log("end")
    End Sub

    <Fact>
    Public Sub LambdaContainingIfBeforeElse()
        Assert.Equal("b,end|b,end|b,end|b,end|end|end|a,end|a,end", RunAllCombinations(AddressOf LambdaBeforeElse))
    End Sub

    Private Sub LambdaBeforeElse(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            Dim act As Action = Sub() If f1 Then Log("a")
            act()
        Else
            Log("b")
        End If
        Log("end")
    End Sub

    <Fact>
    Public Sub SingleLineLambdaWithIfElseInSingleLineIf()
        Assert.Equal("end|end|end|end|b,end|b,end|a,end|a,end", RunAllCombinations(AddressOf SingleLineLambdaInSingleLineIf))
    End Sub

    Private Sub SingleLineLambdaInSingleLineIf(outer As Boolean, f1 As Boolean, f2 As Boolean)
        Dim act As Action = Nothing
        If outer Then act = Sub() If f1 Then Log("a") Else Log("b")
        If act IsNot Nothing Then act()
        Log("end")
    End Sub

    <Fact>
    Public Sub SyncLockContainingIfBeforeElse()
        Assert.Equal("b,end|b,end|b,end|b,end|end|end|a,end|a,end", RunAllCombinations(AddressOf SyncLockBeforeElse))
    End Sub

    Private Sub SyncLockBeforeElse(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            SyncLock _log
                If f1 Then Log("a")
            End SyncLock
        Else
            Log("b")
        End If
        Log("end")
    End Sub

    <Fact>
    Public Sub UsingContainingIfBeforeElse()
        Assert.Equal("b,end|b,end|b,end|b,end|end|end|a,end|a,end", RunAllCombinations(AddressOf UsingBeforeElse))
    End Sub

    Private Sub UsingBeforeElse(outer As Boolean, f1 As Boolean, f2 As Boolean)
        If outer Then
            Using New MemoryStream()
                If f1 Then Log("a")
            End Using
        Else
            Log("b")
        End If
        Log("end")
    End Sub
End Class
