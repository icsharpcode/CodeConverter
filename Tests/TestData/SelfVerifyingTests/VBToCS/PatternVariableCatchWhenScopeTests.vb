Option Strict Off
Imports System
Imports Xunit

Public Class PatternVariableCatchWhenScopeTests
    Private Shared cnt As Integer
    Private Shared Function NI(v As Integer?) As Integer?
        cnt += 1
        Return v
    End Function

    Private Shared Function F() As String
        Dim s2 = ""
        Try
            Throw New Exception("e")
        Catch ex As Exception When NI(2) ^ NI(3) > 4
            Dim arg1 = 5
            s2 &= arg1
        End Try
        Return s2
    End Function

    <Fact>
    Public Sub HeaderPatternVariablesAvoidBodyLocals()
        cnt = 0
        Assert.Equal("5", F())
    End Sub
End Class
