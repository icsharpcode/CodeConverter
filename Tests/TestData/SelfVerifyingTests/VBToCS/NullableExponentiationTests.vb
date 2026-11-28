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
    Public Sub DecimalExponentIsConvertedToDouble()
        Dim d As Double? = 4
        d ^= 0.5D
        Dim arr As Double?() = {4}
        arr(Logged(0, "I")) ^= 0.5D
        Dim n As Double? = Nothing
        Dim x As Double? = d ^ 2.5D
        Dim y As Double? = n ^ 2.5D
        Assert.Equal("2,2,5.656854249492381,Nothing", String.Join(",", Show(d), Show(arr(0)), Show(x), Show(y)))
    End Sub

    Private Shared Function Two() As Integer?
        Return 2
    End Function

    Private Shared Function Same(n As Integer?) As Integer?
        Return n
    End Function

    <Fact>
    Public Sub OperandWhoseCSharpTypeDiffersFromVbs()
        Dim s As Short? = 3
        Dim b As Byte? = 3
        Dim i As Integer = 3
        Dim d As Double? = 2
        d ^= CType(3, Long?)
        Assert.Equal("1,1,9,9,9,8", String.Join(",", Show((s Mod 2S) ^ Two()), Show((b And CByte(1)) ^ Two()), Show(CType(3, Double?) ^ Two()), Show(CType(i, Double?) ^ Two()), Show(CType(s, Integer?) ^ Two()), Show(d)))
    End Sub

    <Fact>
    Public Sub NothingOperandGivesNothing()
        Dim x As Integer? = 3
        Dim r = x ^ Nothing
        Dim l = Nothing ^ x
        Dim c As Integer? = 3
        c ^= Nothing
        Dim cd As Double? = 3
        cd ^= Nothing
        Dim both = 2 ^ Nothing
        Assert.Equal("Nothing,Nothing,Nothing,Nothing,1", String.Join(",", Show(r), Show(l), Show(c), Show(cd), Show(both)))
    End Sub

    <Fact>
    Public Sub PatternVariablesAvoidUserLocals()
        Dim arg1 = 5
        Dim n As Integer? = 3
        Dim r = Same(n) ^ 2
        Dim result = Show(r) & "," & arg1
        If n > 0 Then
            Dim arg3 = 1
            result &= "," & Show(Same(n) ^ 2) & arg3
        End If
        Assert.Equal("9,5,91", result)
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
