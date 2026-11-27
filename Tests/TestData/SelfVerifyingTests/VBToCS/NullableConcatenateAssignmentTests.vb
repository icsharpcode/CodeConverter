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

    Public Enum Colour
        Red = 1
    End Enum

    <Fact>
    Public Sub NullableDateAndEnumConcatenateAsVbFormatsThem()
        Dim d As Date? = #2/3/2001#
        Dim e As Colour? = Colour.Red
        Dim none As Date? = Nothing
        Dim s = "a"
        s &= d
        s &= e
        s &= none
        Dim b = "b" & d & e & none
        Assert.Equal(("a" & CStr(#2/3/2001#) & "1") & "|" & ("b" & CStr(#2/3/2001#) & "1"), s & "|" & b)
    End Sub

    <Fact>
    Public Sub CharConcatenatedWithAStringKeepsItsFirstChar()
        Dim c As Char = "a"c
        c &= "z"
        Assert.Equal("a", CStr(c))
    End Sub
End Class
