Imports System
Imports Xunit

Namespace ProbeSharedParam
Public Class C
    Public Shared Log As String = ""
    Shared _v(3) As Integer
    Public Shared Property SQ(i As Integer) As Integer
        Get
            Log &= "get" & i & ";"
            Return _v(i)
        End Get
        Set(value As Integer)
            Log &= "set" & i & "=" & value & ";"
            _v(i) = value
        End Set
    End Property
End Class
Public Module M
    Dim _m(3) As Short
    Property MQ(i As Integer) As Short
        Get
            C.Log &= "mget" & i & ";"
            Return _m(i)
        End Get
        Set(value As Short)
            C.Log &= "mset" & i & "=" & value & ";"
            _m(i) = value
        End Set
    End Property
    Function I(n As Integer) As Integer
        C.Log &= "I" & n & ";"
        Return n
    End Function
    Public Function ProbeShared() As String
        C.Log = ""
        C.SQ(I(1)) += 2
        MQ(I(2)) += 3
        MQ(I(2)) += 0.4
        Return C.Log
    End Function
End Module
End Namespace

Public Class SharedParameterizedPropertyCompoundAssignmentTests
    <Fact>
    Public Sub ProbeSharedParam_ProbeShared()
        Assert.Equal("I1;get1;set1=2;I2;mget2;mset2=3;I2;mget2;mset2=3;", ProbeSharedParam.M.ProbeShared())
    End Sub
End Class
