Imports System
Imports System.Collections.Generic
Imports Xunit

Namespace Misc
Public Structure Pt
    Public X As Integer
End Structure
Public Class H
    Public Shared SF As Integer
    Public Shared Log As New List(Of String)
    Private _q As New Dictionary(Of String, Integer)
    Public ReadOnly RoPts As Pt() = {New Pt()}
End Class
Public Class Probe
    Shared _h As New H()
    Shared Function GetH() As H
        H.Log.Add("GetH")
        Return _h
    End Function
    Shared Function Key() As Object
        H.Log.Add("Key")
        Return "k"
    End Function
    Public Shared Function Run() As String
        Dim o As Object = 2
        GetH().SF += o
        GetH().RoPts(0).X += o
        Return String.Join(",", H.Log) & ";" & H.SF & "," & _h.RoPts(0).X
    End Function
End Class
End Namespace

Namespace NullableLate
Public Class Probe
    Public Shared Function Run() As String
        Dim r = ""
        Dim ob As Object = 2
        Try
            Dim n As Integer? = 1
            n &= ob
            r &= "a" & n.ToString() & ";"
        Catch ex As Exception
            r &= "a:" & ex.GetType().Name & ";"
        End Try
        Try
            Dim n As Integer? = 7
            n /= ob
            r &= "b" & n.ToString() & ";"
        Catch ex As Exception
            r &= "b:" & ex.GetType().Name & ";"
        End Try
        Try
            Dim n As Short? = 7
            n += ob
            r &= "c" & n.ToString() & ";"
        Catch ex As Exception
            r &= "c:" & ex.GetType().Name & ";"
        End Try
        Try
            Dim n As Integer? = 7
            n ^= ob
            r &= "d" & n.ToString() & ";"
        Catch ex As Exception
            r &= "d:" & ex.GetType().Name & ";"
        End Try
        Try
            Dim n As Long? = 7
            n \= ob
            r &= "e" & n.ToString() & ";"
        Catch ex As Exception
            r &= "e:" & ex.GetType().Name & ";"
        End Try
        Return r
    End Function
End Class
End Namespace

Public Class CompoundAssignmentMiscellaneousTests
    <Fact>
    Public Sub ProbeMisc()
        Assert.Equal("GetH;2,2", Misc.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeNullableLate()
        Assert.Equal("a:InvalidCastException;b:InvalidCastException;c:InvalidCastException;d:InvalidCastException;e3;", NullableLate.Probe.Run())
    End Sub
End Class
