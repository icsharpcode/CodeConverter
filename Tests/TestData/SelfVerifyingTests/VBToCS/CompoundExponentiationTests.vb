Option Strict Off
Imports System
Imports Xunit

Namespace ProbePow
Public Class H
    Public Property P As Integer = 3
    Public Property Q(i As Integer) As Short
        Get
            Return 3
        End Get
        Set(value As Short)
            Last = value
        End Set
    End Property
    Public Last As Short
    Default Public Property Item(i As Integer) As Byte
        Get
            Return 3
        End Get
        Set(value As Byte)
            Last = value
        End Set
    End Property
End Class
Public Module M
    Public Function ProbePow() As String
        Dim o As Object = 2
        o ^= 3
        Dim s As String = "2"
        s ^= 2
        Dim i As Integer = 3
        i ^= o
        Dim d As Double = 2
        d ^= 2D
        Dim sg As Single = 2
        sg ^= 0.5
        Dim l As Long = 2
        l ^= 0.5
        Dim h As New H
        h.P ^= 2
        h.Q(1) ^= 2
        Dim r1 = h.Last
        h(1) ^= 2
        Dim b As Boolean = True
        b ^= 2
        Dim ul As ULong = 3
        ul ^= 2
        Dim io As Integer = 2
        io ^= CObj(Nothing)
        Return String.Join(",", o, s, i, d, sg, l, h.P, r1, h.Last, b, ul, io)
    End Function
    Public Function ProbePowTyped() As String
        Dim o As Object = 2
        o ^= 3
        Dim s As String = "2"
        s ^= 2
        Return o.GetType().Name & s.GetType().Name
    End Function
End Module
End Namespace

Public Class CompoundExponentiationTests
    <Fact>
    Public Sub ProbePow_ProbePow()
        Assert.Equal("8,4,6561,4,1.4142135,1,9,9,9,True,9,1", ProbePow.M.ProbePow())
    End Sub

    <Fact>
    Public Sub ProbePow_ProbePowTyped()
        Assert.Equal("DoubleString", ProbePow.M.ProbePowTyped())
    End Sub
End Class
