Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports Xunit

Namespace Attr
Public Class AAttribute
    Inherits Attribute
    Public Which As String
    Public Sub New(s As Short)
        Which = "Short"
    End Sub
    Public Sub New(l As Long)
        Which = "Long"
    End Sub
    Public Sub New(d As DayOfWeek)
        Which = "DOW" & d.ToString()
    End Sub
    Public Sub New(f As Single, ParamArray rest As Short())
        Which = "F" & f & "/" & String.Join("|", rest)
    End Sub
    Public Property P As Byte
    Public O As Object
    Public C As Char
End Class
<A(1)>
Public Class C1
End Class
<A(CType(2, DayOfWeek))>
Public Class C2
End Class
<A(1.5, 2.5, 3, P:=2.5, O:=CShort(3), C:="x"c)>
Public Class C3
End Class
Public Class Probe
    Shared Function D(t As Type) As String
        Dim a = CType(t.GetCustomAttributes(GetType(AAttribute), False)(0), AAttribute)
        Return a.Which & ":" & a.P & ":" & If(a.O Is Nothing, "", a.O.GetType().Name & a.O.ToString()) & ":" & AscW(a.C)
    End Function
    Public Shared Function Run() As String
        Return D(GetType(C1)) & ";" & D(GetType(C2)) & ";" & D(GetType(C3))
    End Function
End Class
End Namespace

Namespace Caller
Public Class Probe
    Shared Function F(Optional o As Object = 1, <CallerMemberName> Optional name As String = "", <CallerLineNumber> Optional line As Integer = 0) As String
        Return o.ToString() & name & (line > 0)
    End Function
    Public Shared Function Run() As String
        Return F()
    End Function
End Class
End Namespace

Public Class DeclaredConstantAttributeTests
    <Fact>
    Public Sub ProbeAttr()
        Assert.Equal("Long:0::0;DOWTuesday:0::0;F1.5/2|3:2:Int163:120", Attr.Probe.Run())
    End Sub

    <Fact>
    Public Sub ProbeCaller()
        Assert.Equal("1RunTrue", Caller.Probe.Run())
    End Sub
End Class
