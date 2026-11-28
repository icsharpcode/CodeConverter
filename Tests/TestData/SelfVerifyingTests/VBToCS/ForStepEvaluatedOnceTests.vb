Option Strict Off
Imports System
Imports Xunit

Public Class ForStepEvaluatedOnceTests
    Private Shared cnt As Integer
    Private Shared Function NI(v As Integer?) As Integer?
        cnt += 1
        Return v
    End Function

    Private Shared Function F() As String
        Dim s2 = ""
        For k As Double = NI(1) ^ NI(1) To 4 Step NI(1) ^ NI(1)
            Dim arg1 = k
            s2 &= arg1
        Next
        Return s2 & "," & cnt
    End Function

    <Fact>
    Public Sub HeaderPatternVariablesAvoidBodyLocals()
        cnt = 0
        Assert.Equal("1234,4", F())
    End Sub
End Class
