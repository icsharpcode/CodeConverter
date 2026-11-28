Option Strict Off
Imports System
Imports Xunit

Public Class ObjectNothingCompoundAssignmentTests
    Public Structure SV
        Public X As Integer
        Public Sub New(x As Integer)
            Me.X = x
        End Sub
        Public Shared Operator +(a As SV, b As SV) As SV
            Return New SV(a.X + b.X + 1000)
        End Operator
    End Structure

    Private Shared Function Outcome(f As Func(Of Object)) As String
        Try
            Dim o = f()
            Return If(o Is Nothing, "Nothing", o.ToString())
        Catch ex As Exception
            Return ex.GetType().Name
        End Try
    End Function

    <Fact>
    Public Sub StructureWithOperatorTakesNothingAsDefault()
        Dim x As New SV(3)
        x += DirectCast(Nothing, Object)
        Dim arr = {New SV(3)}
        arr(0) += CObj(Nothing)
        Assert.Equal("1003,1003", x.X & "," & arr(0).X)
    End Sub

    <Fact>
    Public Sub IntegralDivisionByObjectNothingOverflows()
        Assert.Equal("OverflowException", Outcome(Function()
                                                       Dim x As Integer = 5
                                                       x /= DirectCast(Nothing, Object)
                                                       Return x
                                                   End Function))
        Assert.Equal("OverflowException", Outcome(Function()
                                                       Dim x As Short = 5
                                                       x /= CObj(Nothing)
                                                       Return x
                                                   End Function))
    End Sub

    <Fact>
    Public Sub DateGivenObjectNothingThrows()
        Assert.Equal("InvalidCastException", Outcome(Function()
                                                          Dim x As Date = #1/2/2000#
                                                          x += DirectCast(Nothing, Object)
                                                          Return x
                                                      End Function))
    End Sub

    <Fact>
    Public Sub NullableConcatenationOfNothingWithObjectNothingThrows()
        Assert.Equal("InvalidCastException", Outcome(Function()
                                                          Dim x As Integer? = Nothing
                                                          x &= DirectCast(Nothing, Object)
                                                          Return x
                                                      End Function))
    End Sub

    <Fact>
    Public Sub NullableArithmeticWithObjectNothingIsNothing()
        Assert.Equal("Nothing,Nothing,Nothing", String.Join(",", Outcome(Function()
                                                                              Dim x As Integer? = 4
                                                                              x += DirectCast(Nothing, Object)
                                                                              Return x
                                                                          End Function), Outcome(Function()
                                                                                                     Dim x As Decimal? = 4
                                                                                                     x /= CObj(Nothing)
                                                                                                     Return x
                                                                                                 End Function), Outcome(Function()
                                                                                                                            Dim x As Double? = 4
                                                                                                                            x ^= CObj(Nothing)
                                                                                                                            Return x
                                                                                                                        End Function)))
    End Sub

    <Fact>
    Public Sub VbBindsObjectNothingToTheOtherOperandsType()
        Dim n As Integer? = 5
        n &= DirectCast(Nothing, Object)
        Dim d As Double? = 1.5
        d &= CObj(Nothing)
        Dim ns As SV? = New SV(3)
        ns += DirectCast(Nothing, Object)
        Dim s As String = Nothing
        s += DirectCast(Nothing, Object)
        Dim t As String = "a"
        t &= CType(Nothing, Object)
        Assert.Equal("5,1.5,Nothing,,a", String.Join(",", n, CStr(d).Replace(Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."), If(ns.HasValue, "SV", "Nothing"), s, t))
    End Sub

    <Fact>
    Public Sub ObjectNothingConvertedToAStructureIsItsDefault()
        Dim d As Date = DirectCast(Nothing, Object)
        Dim sv As SV = CObj(Nothing)
        Assert.Equal("True,0", (d = Nothing) & "," & sv.X)
    End Sub
End Class
