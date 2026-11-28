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

    <Fact>
    Public Sub EnumsAndANullableLeftOperandConcatenateAsVbDoes()
        Dim e = Colour.Red
        Dim ne As Colour? = Colour.Red
        Dim ni As Integer? = Nothing
        Dim nb As Boolean? = Nothing
        Dim nc As Char? = Nothing
        Assert.Equal("11|11|x|y|z", (e & ne) & "|" & (e & e) & "|" & (ni & "x") & "|" & (nb & "y") & "|" & (nc & "z"))
    End Sub
End Class
