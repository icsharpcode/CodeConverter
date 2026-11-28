Option Strict Off
Imports System
Imports Xunit

Public Class NullableBooleanCompoundAssignmentTests
    Private Shared Log As String = ""

    Private Shared Function Idx() As Integer
        Log &= "Idx;"
        Return 0
    End Function

    <Fact>
    Public Sub ArrayElementWithHoistedIndex()
        Log = ""
        Dim x() As Boolean? = {True, True, True, True, True, Nothing}
        x(Idx()) += False
        x(Idx() + 1) -= True
        x(Idx() + 2) *= False
        x(Idx() + 3) <<= False
        x(Idx() + 4) \= True
        x(Idx() + 5) += True
        Assert.Equal("Idx;Idx;Idx;Idx;Idx;Idx;|True,False,False,True,True,", Log & "|" & String.Join(",", x))
    End Sub
End Class
