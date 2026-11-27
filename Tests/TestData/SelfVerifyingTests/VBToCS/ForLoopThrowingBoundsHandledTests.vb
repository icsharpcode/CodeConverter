Option Strict Off
Imports System
Imports Xunit

Public Class ForLoopThrowingBoundsHandledTests
    Private Shared Function L(v As Integer) As Integer
        Return v
    End Function

    Private Shared Function Thr() As Integer
        Throw New InvalidOperationException()
    End Function

    Private Shared Function G() As Integer
        G = 7
        Try
            For G = L(1) To Thr()
            Next
        Catch ex As InvalidOperationException
        End Try
    End Function

    <Fact>
    Public Sub ReturnVariableIsUnassignedWhenToThrows()
        Assert.Equal(7, G())
    End Sub

    <Fact>
    Public Sub ControlVariableIsUnassignedWhenANullableToIsNothing()
        Dim i = 3
        Dim n As Integer? = Nothing
        Try
            For i = L(1) To n
            Next
        Catch ex As InvalidOperationException
        End Try
        Assert.Equal(3, i)
    End Sub

    Private Shared Function WithOnError() As Integer
        Dim i = 3
        On Error GoTo Handler
        For i = L(1) To Thr()
        Next
        Return -1
Handler:
        Return i
    End Function

    <Fact>
    Public Sub ControlVariableIsUnassignedWhenToThrowsUnderOnError()
        Assert.Equal(3, WithOnError())
    End Sub
End Class
