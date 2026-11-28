Option Strict Off
Imports System
Imports Xunit

Namespace ProbeDec
Public Class C
    Public Shared Function F(Optional o As Object = 1, Optional a As Decimal = 0.1F, Optional b As Decimal? = 2.25, Optional c As Decimal = True, Optional d As Decimal? = 1.0R / 3, Optional e As Object = CSng(0.1) * 1D) As String
        Return String.Join("|", o, a, b, c, d, e)
    End Function
    Public Shared Function G(ByRef Optional a As Decimal = 0.1F, ByRef Optional b As Decimal? = -7.5D, Optional c As Decimal = 79228162514264337593543950335D) As String
        Return String.Join("|", a, b, c)
    End Function
End Class
Public Module M
    Public Function ProbeDec() As String
        Return C.F() & "#" & C.G()
    End Function
    Public Function ProbeReflect() As String
        Return String.Join("|", GetType(C).GetMethod("F").GetParameters().Select(Function(p) If(p.DefaultValue, "null").ToString())) & "#" &
            String.Join("|", GetType(C).GetMethod("G").GetParameters().Select(Function(p) If(p.DefaultValue, "null").ToString()))
    End Function
End Module
End Namespace

Public Class DecimalOptionalDefaultTests
    <Fact>
    Public Sub ProbeDec_ProbeDec()
        Assert.Equal("1|0.100000001490116|2.25|-1|0.333333333333333|0.1#0.100000001490116|-7.5|79228162514264337593543950335", ProbeDec.M.ProbeDec())
    End Sub

    <Fact>
    Public Sub ProbeDec_ProbeReflect()
        Assert.Equal("1|0.100000001490116|2.25|-1|0.333333333333333|0.1#0.100000001490116|-7.5|79228162514264337593543950335", ProbeDec.M.ProbeReflect())
    End Sub
End Class
