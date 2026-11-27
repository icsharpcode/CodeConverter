Option Strict Off
Imports System
Imports Xunit

Public Class NullableConcatenateAssignmentTests
    <Fact>
    Public Sub StringConcatenatesNullableValueOrNothing()
        Dim s = "a"
        Dim n As Integer? = 2
        s &= n
        Dim none As Integer? = Nothing
        s &= none
        Dim b As Boolean? = True
        s &= b
        Dim l As Long? = 3
        s &= l
        Dim arr = {"x"}
        arr(0) &= n
        Assert.Equal("a2True3,x2", s & "," & arr(0))
    End Sub
End Class
