Imports System
Imports System.Collections.Generic
Imports Xunit

Namespace OptionalProp
Public Class H
    Public Shared Log As New List(Of String)
    Private _q As Integer() = {0, 0, 0}
    Public Property Q2(i As Integer, Optional j As Integer = 0) As Integer
        Get
            Log.Add("get" & i & j)
            Return _q(i + j)
        End Get
        Set(value As Integer)
            Log.Add("set" & i & j)
            _q(i + j) = value
        End Set
    End Property
    Public Property V(Optional i As Integer = 2) As Integer
        Get
            Log.Add("vget" & i)
            Return i * 10
        End Get
        Set(newValue As Integer)
            Log.Add("vset" & i & "=" & newValue)
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
        h.Q2(Idx()) += o
        h.Q2(Idx(), Idx() - 1) += 1
        h.V() += o
        h.Q2(j:=Idx(), i:=0) += o
        Return String.Join(",", H.Log) & ";" & h.Q2(1)
    End Function
End Class
End Namespace

Public Class ParameterizedPropertyCompoundAssignmentTests
    <Fact>
    Public Sub ProbeOptionalProp()
        Assert.Equal("Idx,get10,set10,Idx,Idx,get10,set10,vget2,vset2=22,Idx,get01,set01;5", OptionalProp.Probe.Run())
    End Sub
End Class
