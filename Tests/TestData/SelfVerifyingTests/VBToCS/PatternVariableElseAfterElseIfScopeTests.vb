Option Strict Off
Imports System
Imports Xunit

Public Class PatternVariableElseAfterElseIfScopeTests
    Private Shared cnt As Integer
    Private Shared Function NI(v As Integer?) As Integer?
        cnt += 1
        Return v
    End Function

    Private Shared Function F() As String
        Dim s2 = ""
        Dim k = 1
        If k = 2 Then
            s2 &= "a"
        ElseIf NI(k) ^ NI(2) > 5 Then
            s2 &= "b"
        Else
            Dim arg2 = 6
            s2 &= arg2
        End If
        Return s2
    End Function

    <Fact>
    Public Sub HeaderPatternVariablesAvoidBodyLocals()
        cnt = 0
        Assert.Equal("6", F())
    End Sub
End Class
