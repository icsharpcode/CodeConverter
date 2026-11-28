Imports System
Imports Xunit

Namespace ProbeWith
Public Structure S
    Public Inner As S2
    Private _q As Short
    Public Property Q(i As Integer) As Short
        Get
            M.Log &= "getQ" & i & ";"
            Return _q
        End Get
        Set(value As Short)
            M.Log &= "setQ" & i & "=" & value & ";"
            _q = value
        End Set
    End Property
End Structure
Public Structure S2
    Public Y As Short
    Public Arr() As Short
End Structure
Public Class CC
    Public F As S
    Public A(2) As S
End Class
Public Module M
    Public Log As String = ""
    Function I(n As Integer) As Integer
        Log &= "I" & n & ";"
        Return n
    End Function
    Dim _c As New CC
    Function GetC() As CC
        Log &= "GetC;"
        Return _c
    End Function
    Public Function ProbeWith() As String
        Log = ""
        _c.F.Inner.Arr = New Short(3) {}
        With GetC().F
            .Q(I(1)) += 0.4 + 2
            .Inner.Y += 0.4 + 3
            .Inner.Arr(I(2)) += 0.4 + 4
        End With
        With GetC()
            .F.Q(I(1)) += 1
            .A(I(1)).Inner.Y += 0.4 + 5
            .A(I(1)).Q(I(2)) += 6
        End With
        With GetC().A(I(2))
            .Q(I(1)) += 0.4 + 7
            .Inner.Y += 0.4 + 8
        End With
        Return Log & "|" & _c.F.Q(0) & "," & _c.F.Inner.Y & "," & _c.F.Inner.Arr(2) & "," & _c.A(1).Inner.Y & "," & _c.A(1).Q(0) & "," & _c.A(2).Q(0) & "," & _c.A(2).Inner.Y
    End Function
End Module
End Namespace

Public Class CompoundAssignmentWithBlockTests
    <Fact>
    Public Sub ProbeWith_ProbeWith()
        Assert.Equal("GetC;I1;getQ1;setQ1=2;I2;GetC;I1;getQ1;setQ1=3;I1;I1;I2;getQ2;setQ2=6;GetC;I2;I1;getQ1;setQ1=7;|3,3,4,5,6,7,8", ProbeWith.M.ProbeWith())
    End Sub
End Class
