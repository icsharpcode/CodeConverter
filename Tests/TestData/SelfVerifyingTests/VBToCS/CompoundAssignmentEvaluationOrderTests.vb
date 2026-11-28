Imports System
Imports System.Collections.Generic
Imports Xunit

Namespace Order
Public Class Probe
    Shared _arr As Integer() = {1, 2}
    Shared _old As Integer()
    Shared Function Idx() As Integer
        _old = _arr
        _arr = New Integer() {100, 200}
        Return 1
    End Function
    Public Shared Function Run() As String
        Dim o As Object = 5
        _arr(Idx()) += o
        Return _old(1) & "," & _arr(1)
    End Function
End Class
End Namespace

Namespace Writeback
Public Class H
    Private _p As Integer = 3
    Public Property P As Integer
        Get
            Probe.Log.Add("getP")
            Return _p
        End Get
        Set(value As Integer)
            Probe.Log.Add("setP")
            _p = value
        End Set
    End Property
End Class
Public Structure Pt
    Public X As Integer
End Structure
Public Class Probe
    Public Shared Log As New List(Of String)
    Shared _ints As Integer() = {1, 2}
    Shared _pts As Pt() = {New Pt(), New Pt()}
    Shared Function Idx() As Integer
        Log.Add("Idx")
        Return 1
    End Function
    Shared Function Inc(ByRef x As Integer) As Object
        Log.Add("Inc")
        x += 1
        Return x
    End Function
    Public Shared Function Run() As String
        Dim h As New H()
        _ints(Idx()) += Inc(h.P)
        _pts(Idx()).X += Inc(h.P)
        Return String.Join(",", Log) & ";" & _ints(1) & "," & _pts(1).X & "," & h.P
    End Function
End Class
End Namespace

Namespace NamedIndexer
Public Class H
    Public Shared Log As New List(Of String)
    Private _q As Integer() = {0, 0, 0}
    Default Public Property Item(i As Integer, j As Integer) As Integer
        Get
            Log.Add("get" & i & j)
            Return _q(i + j)
        End Get
        Set(value As Integer)
            Log.Add("set" & i & j)
            _q(i + j) = value
        End Set
    End Property
End Class
Public Class Probe
    Shared Function Idx() As Integer
        H.Log.Add("Idx")
        Return 1
    End Function
    Public Shared Function Run() As String
        Dim h As New H()
        Dim o As Object = 2
        h(j:=Idx(), i:=0) += o
        Return String.Join(",", H.Log) & ";" & h(0, 1)
    End Function
End Class
End Namespace

Public Class CompoundAssignmentEvaluationOrderTests
    <Fact>
    Public Sub ProbeOrder()
        Assert.Equal("7,200", Order.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeWriteback()
        Assert.Equal("Idx,getP,Inc,setP,Idx,getP,Inc,setP;6,5,5", Writeback.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeNamedIndexer()
        Assert.Equal("Idx,get01,set01;2", NamedIndexer.Probe.Run())
    End Sub
End Class
