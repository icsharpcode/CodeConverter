Imports System
Imports Xunit

Namespace Consts
Public Enum Col
    Red
    Green
End Enum
Public Class TAttribute
    Inherits Attribute
    Public V As String
    Public Sub New(s As String)
        V = s
    End Sub
    Public Sub New(c As Col)
        V = "col" & c.ToString()
    End Sub
    Public Property B As Boolean
End Class
<T("x"c, B:=Col.Green)>
Public Class C1
End Class
Public Class Probe
    Shared Function F(Optional b As Boolean = Col.Green, Optional c As Char = ChrW(65), Optional s As String = ChrW(66) & "b", Optional i As Integer = AscW("C"c)) As String
        Return b & c & s & i
    End Function
    Shared Function G(Optional e As Col = True) As String
        Return e.ToString()
    End Function
    Public Shared Function Run() As String
        Dim a = CType(GetType(C1).GetCustomAttributes(GetType(TAttribute), False)(0), TAttribute)
        Return F() & G() & a.V & a.B
    End Function
End Class
End Namespace

Namespace ConstsExact
Public Enum Col
    Red
    Green
End Enum
Public Class Probe
    Shared Function F(Optional o As Object = 1, Optional b As Boolean = Col.Green, Optional c As Char = ChrW(65), Optional s As String = ChrW(66) & "b", Optional i As Integer = AscW("C"c), Optional d As Double = Col.Green, Optional e As DayOfWeek? = 2) As String
        Return b & c & s & i & d & e.ToString()
    End Function
    Public Shared Function Run() As String
        Return F()
    End Function
End Class
End Namespace

Public Class DeclaredConstantWithoutConversionTests
    <Fact>
    Public Sub ProbeConsts()
        Assert.Equal("TrueABb67-1xTrue", Consts.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeConstsExact()
        Assert.Equal("TrueABb671Tuesday", ConstsExact.Probe.Run())
    End Sub
End Class
