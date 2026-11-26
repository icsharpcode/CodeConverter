Imports System
Imports System.Collections.Generic
Imports Xunit

Namespace LateboundMember
Public Class H
    Public F As Integer
    Public Property P As Integer
    Default Public Property Item(i As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
Public Class Probe
    Shared _log As New List(Of String)
    Shared _h As New H()
    Shared Function GetO() As Object
        _log.Add("GetO")
        Return _h
    End Function
    Shared Function Idx() As Integer
        _log.Add("Idx")
        Return 1
    End Function
    Public Shared Function Run() As String
        GetO().F += 1
        Return String.Join(",", _log) & ";" & _h.F & "," & _h.P
    End Function
End Class
End Namespace

Public Class LateBoundMemberCompoundAssignmentTests
    <Fact>
    Public Sub ProbeLateboundMember()
        Assert.Equal("GetO;1,0", LateboundMember.Probe.Run())
    End Sub
End Class
