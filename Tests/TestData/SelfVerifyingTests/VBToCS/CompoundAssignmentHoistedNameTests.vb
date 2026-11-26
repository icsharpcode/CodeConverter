Imports System
Imports Xunit

Namespace ProbeLater
Public Structure S
    Public X As Integer
End Structure
Public Module M
    Function I(n As Integer) As Integer
        Return n
    End Function
    Public Function ProbeLater() As String
        Dim a(3) As S
        a(I(1)).X += 0.4 + 1
        Dim index1 = 5
        Dim receiver = 6
        Return a(1).X & index1 & receiver
    End Function
    Public Function ProbeLaterInt() As String
        Dim a(3) As Integer
        a(I(1)) += 0.4 + 1
        Dim index1 = 5
        Return a(1) & index1
    End Function
    Public Function ProbeNestedForEach() As String
        Dim a(3) As S
        For Each index1 In {1, 2}
            Dim z = index1
        Next
        a(I(1)).X += 0.4 + 1
        Return CStr(a(1).X)
    End Function
    Public Function ProbeNestedBlock() As String
        Dim a(3) As S
        If a.Length > 0 Then
            Dim index1 = 5
        End If
        a(I(1)).X += 0.4 + 1
        Dim b(3) As Integer
        For Each index1 In {1, 2}
            b(I(index1)) += 0.4 + 1
        Next
        b(I(1)) += 0.4 + 1
        Return a(1).X & "," & b(1) & b(2)
    End Function
End Module
End Namespace

Public Class CompoundAssignmentHoistedNameTests
    <Fact>
    Public Sub ProbeLater_ProbeLater()
        Assert.Equal("156", ProbeLater.M.ProbeLater())
    End Sub

    <Fact>
    Public Sub ProbeLater_ProbeLaterInt()
        Assert.Equal("15", ProbeLater.M.ProbeLaterInt())
    End Sub
    <Fact>
    Public Sub ProbeLater_ProbeNestedForEach()
        Assert.Equal("1", ProbeLater.M.ProbeNestedForEach())
    End Sub

    <Fact>
    Public Sub ProbeLater_ProbeNestedBlock()
        Assert.Equal("1,21", ProbeLater.M.ProbeNestedBlock())
    End Sub
End Class
