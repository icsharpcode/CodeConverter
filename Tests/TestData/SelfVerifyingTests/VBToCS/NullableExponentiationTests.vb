Option Strict Off
Imports System
Imports Xunit

Public Class NullableExponentiationTests
    Private Shared Log As String = ""

    Private Shared Function Logged(Of T)(value As T, name As String) As T
        Log &= name & ";"
        Return value
    End Function

    Private Shared Function Show(value As Object) As String
        Return If(value Is Nothing, "Nothing", Convert.ToString(value, Globalization.CultureInfo.InvariantCulture))
    End Function

    <Fact>
    Public Sub CompoundExponentiationOfNothingIsNothing()
        Dim d As Double? = Nothing
        d ^= 2
        Dim i As Integer? = Nothing
        i ^= 2
        Dim s As Short? = Nothing
        s ^= 2
        Dim sg As Single? = Nothing
        sg ^= 2
        Dim m As Decimal? = Nothing
        m ^= 2
        Dim b As Boolean? = Nothing
        b ^= 2
        Dim n As Integer? = Nothing
        Dim d2 As Double = 3
        Dim d3 As Double? = 3
        d3 ^= n
        Assert.Equal("Nothing,Nothing,Nothing,Nothing,Nothing,Nothing,Nothing", String.Join(",", Show(d), Show(i), Show(s), Show(sg), Show(m), Show(b), Show(d3)))
    End Sub

    <Fact>
    Public Sub CompoundExponentiationOfValues()
        Dim d As Double? = 3
        d ^= 2
        Dim i As Integer? = 3
        i ^= 2
        Dim s As Short? = 3
        s ^= 2
        Dim sg As Single? = 3
        sg ^= 2
        Dim m As Decimal? = 3
        m ^= 2
        Dim b As Boolean? = True
        b ^= 2
        Dim n As Integer? = 2
        Dim d3 As Double? = 3
        d3 ^= n
        Assert.Equal("9,9,9,9,9,True,9", String.Join(",", Show(d), Show(i), Show(s), Show(sg), Show(m), Show(b), Show(d3)))
    End Sub

    <Fact>
    Public Sub ExponentiationOfNothingIsNothing()
        Dim d As Double? = Nothing
        Dim n As Integer? = Nothing
        Dim two As Integer? = 2
        Dim x As Double? = d ^ 2
        Dim y As Double? = 3 ^ n
        Dim z As Double? = two ^ two
        Dim w As Integer? = n ^ 2
        Assert.Equal("Nothing,Nothing,4,Nothing", String.Join(",", Show(x), Show(y), Show(z), Show(w)))
    End Sub

    <Fact>
    Public Sub BothOperandsAreEvaluatedInOrder()
        Log = ""
        Dim n As Integer? = Nothing
        Dim x As Double? = Logged(n, "L") ^ Logged(2, "R")
        Dim y As Double? = Logged(2, "L") ^ Logged(n, "R")
        Dim arr As Double?() = {Nothing, 2}
        arr(Logged(1, "I")) ^= Logged(n, "V")
        Assert.Equal("L;R;L;R;I;V;|Nothing,Nothing,Nothing", Log & "|" & String.Join(",", Show(x), Show(y), Show(arr(1))))
    End Sub
End Class
