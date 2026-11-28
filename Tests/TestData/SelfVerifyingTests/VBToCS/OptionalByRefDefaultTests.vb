Imports System
Imports Xunit

Namespace Exact
Public Class Probe
    Shared Function F(Optional ByRef c As Char = "a"c, Optional ByRef s As Single = 1, Optional ByRef u As ULong = 1, Optional ByRef b As Boolean = 1, Optional ByRef e As DayOfWeek = 1, Optional ByRef n As Short? = 1, Optional ByRef by As Byte = 2.5, Optional ByRef dd As Double = 1.5F, Optional ByRef ui As UInteger = 3L, Optional ByRef st As String = "q"c, Optional ByRef sb As SByte = -1, Optional ByRef lo As Long = Integer.MaxValue) As String
        Return c & s & u & b & e.ToString() & n & by & dd & ui & st & sb & lo
    End Function
    Public Shared Function Run() As String
        Return F()
    End Function
End Class
End Namespace

Public Class OptionalByRefDefaultTests
    <Fact>
    Public Sub ProbeExact()
        Assert.Equal("a11TrueMonday121.53q-12147483647", Exact.Probe.Run())
    End Sub
End Class
