Option Strict Off
Imports System
Imports Xunit

Public Class ForLoopThrowingBoundsTests
    Private Shared Function L(v As Integer) As Integer
        Return v
    End Function

    Private Shared Function Thr() As Integer
        Throw New InvalidOperationException()
    End Function

    <Fact>
    Public Sub ControlVariableIsUnassignedWhenToThrows()
        Dim i = 3
        Try
            For i = L(1) To Thr()
            Next
        Catch ex As InvalidOperationException
        End Try
        Assert.Equal(3, i)
    End Sub

    <Fact>
    Public Sub ControlVariableIsUnassignedWhenStepThrows()
        Dim i = 3
        Try
            For i = L(1) To 5 Step Thr()
            Next
        Catch ex As InvalidOperationException
        End Try
        Assert.Equal(3, i)
    End Sub

    <Fact>
    Public Sub StringFromConvertsToTheControlVariable()
        Dim s = ""
        Dim i As Integer
        Dim f As String = "2"
        For i = f To L(4)
            s &= i
        Next
        Assert.Equal("234", s)
    End Sub
End Class
