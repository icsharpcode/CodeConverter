Imports System
Imports Xunit

Namespace DecimalDefault
Public Class Probe
    Shared Function F(Optional o As Object = 1, Optional d As Decimal = 1.5D) As String
        Return o.ToString() & "|" & d.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function
    Shared Function F2(Optional ByRef i As Integer = 1, Optional d As Decimal = 2.5D) As String
        Return d.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function
    Shared Function F3(Optional ByRef d As Decimal = 2.5D) As String
        Return d.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function
    Public Shared Function Run() As String
        Return F() & F2() & F3()
    End Function
End Class
End Namespace

Public Class DecimalDefaultTests
    <Fact>
    Public Sub ProbeDecimalDefault()
        Assert.Equal("1|1.52.52.5", DecimalDefault.Probe.Run())
    End Sub
End Class
