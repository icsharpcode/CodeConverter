Imports System
Imports System.Collections.Generic
Imports Xunit

Namespace ProbeTprop
Public Interface IHasX
    Property X As Integer
End Interface
Public Class CX
    Implements IHasX
    Public Property X As Integer Implements IHasX.X
End Class
Public Class Holder(Of T As IHasX)
    Public Log As String = ""
    Private _t As T
    Public Sub New(t As T)
        _t = t
    End Sub
    Public ReadOnly Property P As T
        Get
            Log &= "P;"
            Return _t
        End Get
    End Property
    Public Function GetT() As T
        Log &= "GetT;"
        Return _t
    End Function
    Public Sub Work()
        P.X += 0.4 + 1
        GetT().X += 0.4 + 2
    End Sub
End Class
Public Module M
    Public Function ProbeT() As String
        Dim h As New Holder(Of CX)(New CX)
        h.Work()
        Return h.Log & h.P.X
    End Function
End Module
End Namespace

Namespace ProbeTidx
Public Interface IHasX
    Property X As Integer
End Interface
Public Class CX
    Implements IHasX
    Public Property X As Integer Implements IHasX.X
End Class
Public Module M
    Public Log As String = ""
    Function I(n As Integer) As Integer
        Log &= "I" & n & ";"
        Return n
    End Function
    Sub Work(Of T As IHasX)(l As List(Of T))
        l(I(0)).X += 0.4 + 1
        l(I(0)).X += 1
    End Sub
    Public Function ProbeList() As String
        Log = ""
        Dim l As New List(Of CX) From {New CX}
        Work(l)
        Return Log & l(0).X
    End Function
End Module
End Namespace

Public Class TypeParameterValueReceiverCompoundAssignmentTests
    <Fact>
    Public Sub ProbeTprop_ProbeT()
        Assert.Equal("P;GetT;3", ProbeTprop.M.ProbeT())
    End Sub

    <Fact>
    Public Sub ProbeTidx_ProbeList()
        Assert.Equal("I0;I0;2", ProbeTidx.M.ProbeList())
    End Sub
End Class
