Option Strict Off
Imports System
Imports Xunit

Namespace ProbeIntdiv
Public Module M
    Public Function ProbeIntDiv() As String
        Dim sg As Single = 7.5F
        sg \= 2
        Dim d As Double = 7.5
        d \= 2
        Dim m As Decimal = 7.5D
        m \= 2
        Dim a() As Single = {7.5F}
        a(0) \= 2
        Return sg & "," & d & "," & m & "," & a(0)
    End Function
End Module
End Namespace

Public Class CompoundIntegerDivisionTests
    <Fact>
    Public Sub ProbeIntdiv_ProbeIntDiv()
        Assert.Equal("4,4,4,4", ProbeIntdiv.M.ProbeIntDiv())
    End Sub
End Class
