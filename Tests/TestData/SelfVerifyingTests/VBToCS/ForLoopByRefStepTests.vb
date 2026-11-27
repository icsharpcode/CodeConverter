Imports System
Imports Xunit

Public Class ForLoopByRefStepTests
    Private fld As Integer

    Private Function StepThroughAlias(ByRef s As Integer) As String
        Dim r = ""
        For i = 1 To 9 Step s
            r &= i
            fld = 1
        Next
        Return r
    End Function

    Private Shared Function StepAliasingTheControlVariable(ByRef c As Integer, ByRef s As Integer) As String
        Dim r = ""
        For c = 1 To 9 Step s
            r &= c & ","
        Next
        Return r
    End Function

    <Fact>
    Public Sub ByRefStepIsEvaluatedOnce()
        fld = 2
        Assert.Equal("13579", StepThroughAlias(fld))
    End Sub

    <Fact>
    Public Sub ByRefStepAliasingTheControlVariableIsEvaluatedOnce()
        Dim x = 2
        Assert.Equal("1,3,5,7,9,", StepAliasingTheControlVariable(x, x))
    End Sub
End Class
