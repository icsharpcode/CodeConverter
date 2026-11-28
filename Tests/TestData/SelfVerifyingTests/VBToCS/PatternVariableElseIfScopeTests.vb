Option Strict Off
Imports System
Imports Xunit

Public Class PatternVariableElseIfScopeTests
    Private Shared cnt As Integer
    Private Shared Function NI(v As Integer?) As Integer?
        cnt += 1
        Return v
    End Function

    Private Shared Function F() As String
        Dim s2 = ""
        Dim z = 0
        If z = 1 Then
            s2 = "a"
        ElseIf NI(2) ^ NI(3) > 4 Then
            Dim arg1 = 5
            s2 &= arg1
        End If
        Return s2
    End Function

    <Fact>
    Public Sub HeaderPatternVariablesAvoidBodyLocals()
        cnt = 0
        Assert.Equal("5", F())
    End Sub
End Class
