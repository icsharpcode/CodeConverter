Option Strict Off
Imports System
Imports Xunit

Namespace NothingOperands
Public Module P
  Function NI(v As Integer?) As Integer?
    Return v
  End Function
  Function S(o As Object) As String
    If o Is Nothing Then Return "N"
    Return o.ToString() & ":" & o.GetType().Name
  End Function
  Public Function DCObj() As String
    Return S(NI(2) ^ DirectCast(Nothing, Object))
  End Function
  Public Function DCObjL() As String
    Return S(DirectCast(Nothing, Object) ^ NI(2))
  End Function
  Public Function CTObj() As String
    Return S(NI(2) ^ CType(Nothing, Object))
  End Function
  Public Function CObjN() As String
    Return S(NI(2) ^ CObj(Nothing))
  End Function
  Public Function DCStr() As String
    Return S(NI(2) ^ DirectCast(Nothing, String))
  End Function
  Public Function TCObj() As String
    Return S(NI(2) ^ TryCast(Nothing, Object))
  End Function
  Public Function ParenNothing() As String
    Return S(NI(2) ^ (Nothing))
  End Function
  Public Function PlainLocalNothing() As String
    Dim x As Integer? = 2
    Return S(x ^ Nothing)
  End Function
  Public Function PlainLocalDC() As String
    Dim x As Integer? = 2
    Return S(x ^ DirectCast(Nothing, Object))
  End Function
  Public Function PlainLocalCT() As String
    Dim x As Integer? = 2
    Return S(x ^ CType(Nothing, Integer?))
  End Function
  Public Function PlainBothLit() As String
    Return S(2 ^ CType(Nothing, Integer?))
  End Function
  Public Function PlusDC() As String
    Return S(NI(2) + DirectCast(Nothing, Object))
  End Function
  Public Function CompoundDC() As String
    Dim x As Integer? = 2
    x ^= DirectCast(Nothing, Object)
    Return S(x)
  End Function
  Public Function CompoundPlusDC() As String
    Dim x As Integer? = 2
    x += DirectCast(Nothing, Object)
    Return S(x)
  End Function
  Public Function ConstObj() As String
    Const c As Object = Nothing
    Return S(NI(2) ^ c)
  End Function
End Module
End Namespace

Public Class NothingOperandExponentiationTests
    <Fact>
    Public Sub DCObj()
        Assert.Equal("N", NothingOperands.P.DCObj())
    End Sub

    <Fact>
    Public Sub DCObjL()
        Assert.Equal("N", NothingOperands.P.DCObjL())
    End Sub

    <Fact>
    Public Sub CTObj()
        Assert.Equal("N", NothingOperands.P.CTObj())
    End Sub

    <Fact>
    Public Sub CObjN()
        Assert.Equal("N", NothingOperands.P.CObjN())
    End Sub

    <Fact>
    Public Sub DCStr()
        Assert.Equal("N", NothingOperands.P.DCStr())
    End Sub

    <Fact>
    Public Sub TCObj()
        Assert.Equal("N", NothingOperands.P.TCObj())
    End Sub

    <Fact>
    Public Sub ParenNothing()
        Assert.Equal("N", NothingOperands.P.ParenNothing())
    End Sub

    <Fact>
    Public Sub PlainLocalNothing()
        Assert.Equal("N", NothingOperands.P.PlainLocalNothing())
    End Sub

    <Fact>
    Public Sub PlainLocalDC()
        Assert.Equal("N", NothingOperands.P.PlainLocalDC())
    End Sub

    <Fact>
    Public Sub PlainLocalCT()
        Assert.Equal("N", NothingOperands.P.PlainLocalCT())
    End Sub

    <Fact>
    Public Sub PlainBothLit()
        Assert.Equal("N", NothingOperands.P.PlainBothLit())
    End Sub

    <Fact>
    Public Sub PlusDC()
        Assert.Equal("N", NothingOperands.P.PlusDC())
    End Sub

    <Fact>
    Public Sub CompoundDC()
        Assert.Equal("N", NothingOperands.P.CompoundDC())
    End Sub

    <Fact>
    Public Sub ConstObj()
        Assert.Equal("N", NothingOperands.P.ConstObj())
    End Sub

    <Fact>
    Public Sub CompoundPlusDC()
        Assert.Equal("N", NothingOperands.P.CompoundPlusDC())
    End Sub
End Class
