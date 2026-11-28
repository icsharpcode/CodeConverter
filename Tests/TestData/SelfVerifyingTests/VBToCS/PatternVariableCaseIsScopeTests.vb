Option Strict Off
Imports System
Imports Xunit

Public Class PatternVariableCaseIsScopeTests
    Private Shared cnt As Integer
    Private Shared Function NI(v As Integer?) As Integer?
        cnt += 1
        Return v
    End Function

    Private Shared Function F() As String
        Dim s2 = ""
        For k = 1 To 3
            Select Case k
                Case Is < NI(2) ^ NI(1)
                    Dim arg1 = 9
                    s2 &= arg1
                Case Else
                    Dim arg2 = 8
                    s2 &= arg2
            End Select
        Next
        Return s2
    End Function

    <Fact>
    Public Sub HeaderPatternVariablesAvoidBodyLocals()
        cnt = 0
        Assert.Equal("988", F())
    End Sub
End Class
