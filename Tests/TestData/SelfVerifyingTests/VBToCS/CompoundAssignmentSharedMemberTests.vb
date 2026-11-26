Imports System
Imports Xunit

Namespace SharedViaInstance
Public Class H
    Public Shared SF As Integer
    Public Shared Property SP As Integer
End Class
Public Class Probe
    Shared _h As New H()
    Shared Function GetH() As H
        Return _h
    End Function
    Public Shared Function Run() As String
        Dim o As Object = 2
        GetH().SF += o
        GetH().SP += 2.5
        GetH().SF += 2.5
        Return H.SF & "," & H.SP
    End Function
End Class
End Namespace

Public Class CompoundAssignmentSharedMemberTests
    <Fact>
    Public Sub ProbeSharedViaInstance()
        Assert.Equal("4,2", SharedViaInstance.Probe.Run())
    End Sub
End Class
