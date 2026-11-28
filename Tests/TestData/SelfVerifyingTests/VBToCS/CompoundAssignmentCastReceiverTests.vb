Imports System
Imports Xunit

Namespace ProbeParen
Public Class CX
    Public X As Integer
    Public Property P As Integer
End Class
Public Module M
    Dim Log As String = ""
    Function G() As Object
        Log &= "G;"
        Return _c
    End Function
    Dim _c As New CX
    Public Function ProbeP() As String
        DirectCast(G(), CX).X += 0.4 + 1
        CType(G(), CX).P += 0.4 + 2
        TryCast(G(), CX).P += 0.4 + 2
        Return Log & _c.X & _c.P
    End Function
End Module
End Namespace

Public Class CompoundAssignmentCastReceiverTests
    <Fact>
    Public Sub ProbeParen_ProbeP()
        Assert.Equal("G;G;G;14", ProbeParen.M.ProbeP())
    End Sub
End Class
