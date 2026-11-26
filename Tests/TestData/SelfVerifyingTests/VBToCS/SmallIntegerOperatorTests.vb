Imports System
Imports Xunit

Public Class SmallIntegerOperatorTests
    Const C1 As UShort = 65534, C2 As UShort = 40000
    Const CB As Byte = 200
    Const K1 As Byte = CB << 1

    Private Function TakeInteger(i As Integer) As Integer
        Return i
    End Function

    Private Function TakeObject(o As Object) As Object
        Return o
    End Function

    <Fact>
    Public Sub NotOfByteWidenedToIntegerIsInByteRange()
        Dim b1 As Byte = 200
        Dim i As Integer = Not b1
        Assert.Equal(55, i)
        Assert.Equal(55, TakeInteger(Not b1))
        Assert.Equal("x55", "x" & Not b1)
        Assert.Equal("55", $"{Not b1}")
    End Sub

    <Fact>
    Public Sub NotOfUShortWidenedToIntegerIsInUShortRange()
        Dim us As UShort = 65000
        Dim i As Integer = Not us
        Assert.Equal(535, i)
    End Sub

    <Fact>
    Public Sub LeftShiftOfByteWidenedToLongIsTruncatedFirst()
        Dim b1 As Byte = 200
        Dim l As Long = b1 << 3
        Assert.Equal(64L, l)
    End Sub

    <Fact>
    Public Sub NotOfSByteWidenedToIntegerIsUnchanged()
        Dim sb As SByte = -128
        Dim i As Integer = Not sb
        Assert.Equal(127, i)
    End Sub

    <Fact>
    Public Sub UnaryMinusOfByteIsShort()
        Dim b1 As Byte = 200
        Dim o As Object = -b1
        Assert.Equal(GetType(Short), o.GetType())
        Assert.Equal(-200S, o)
    End Sub

    <Fact>
    Public Sub BoxedResultsKeepTheVbType()
        Dim b1 As Byte = 200, b2 As Byte = 50, s1 As Short = 3, us As UShort = 7, sb As SByte = -3
        Dim o As Object = b1 Or b2
        Assert.Equal(GetType(Byte), o.GetType())
        Assert.Equal(GetType(Byte), TakeObject(b1 And b2).GetType())
        Assert.Equal(GetType(Byte), TakeObject(b1 + b2).GetType())
        Assert.Equal(GetType(Byte), (b1 Xor b2).GetType())
        Assert.Equal(GetType(Short), TakeObject(s1 Or s1).GetType())
        Assert.Equal(GetType(UShort), TakeObject(Not us).GetType())
        Assert.Equal(GetType(SByte), TakeObject(sb And sb).GetType())
        Assert.True(TypeOf TakeObject(b1 >> 1) Is Byte)
    End Sub

    <Fact>
    Public Sub LambdasReturnTheVbType()
        Dim b1 As Byte = 200, b2 As Byte = 50
        Dim f1 = Function() b1 And b2
        Dim f2 = Function() Not b1
        Assert.Equal(CByte(b1 And b2), f1())
        Assert.Equal(CByte(55), f2())
        Assert.Equal(GetType(Byte), CObj(f2()).GetType())
    End Sub

    <Fact>
    Public Sub ChainedOperatorsAndComparisons()
        Dim b1 As Byte = 200, b2 As Byte = 50
        Assert.Equal(CByte(55), (Not b1) Or b2 And b2)
        Assert.True((Not b1) > b2)
        Assert.Equal(CByte(27), (Not b1) >> 1)
        Assert.Equal(CByte(5), (Not b1) \ 10)
        Assert.Equal(CByte(5), (Not b1) Mod b2)
        Assert.True((b1 And b2) = b2 - 50)
        Dim i As Integer = (Not b1) Xor b2
        Assert.Equal(5, i)
    End Sub

    <Fact>
    Public Sub NullableResultsKeepTheVbType()
        Dim nb As Byte? = 200, ns As Short? = 3, noValue As Byte? = Nothing
        Dim a = nb And nb
        Dim n = Not nb
        Dim i As Integer? = Not nb
        Assert.Equal(GetType(Byte), CObj(a).GetType())
        Assert.Equal(CByte(55), n)
        Assert.Equal(55, i)
        Assert.Equal(GetType(Short), CObj(-ns).GetType())
        Assert.Equal(GetType(Short), CObj(ns << 1).GetType())
        Assert.Null(noValue Or nb)
    End Sub

    <Fact>
    Public Sub ShiftCountsAreMaskedToTheBitsOfTheShiftedType()
        Dim b1 As Byte = 200, sb As SByte = -128, s As Short = -32768, us As UShort = 65000
        Dim nb As Byte? = 200
        Dim n = 9, big = 17, minusOne = -1
        Assert.Equal(CByte(144), b1 << n)
        Assert.Equal(CByte(100), b1 >> n)
        Assert.Equal(CByte(0), b1 << minusOne)
        Assert.Equal(CSByte(-64), sb >> n)
        Assert.Equal(0S, s << big)
        Assert.Equal(CUShort(32500), us >> big)
        Assert.Equal(CByte(144), nb << n)
        Dim i As Integer = b1 << n
        Assert.Equal(144, i)
        b1 <<= n
        Assert.Equal(CByte(144), b1)
        us >>= big
        Assert.Equal(CUShort(32500), us)
    End Sub

    <Fact>
    Public Sub ConstantsOutOfCSharpRangeKeepTheirVbValue()
        Dim us As UShort = 5, b As Byte = 10
        Dim x As UShort = (Not C1) * C2 + us
        Assert.Equal(CUShort(40005), x)
        Assert.Equal(CByte(144), K1)
        Dim y As Integer = (Not CB) Mod b
        Assert.Equal(5, y)
        Dim z As Integer = (Not C1) * C2
        Assert.Equal(40000, z)
        Assert.Equal(CByte(55), Not CB)
    End Sub

    <Fact>
    Public Sub ExplicitConversionsOfResults()
        Dim b1 As Byte = 200, us As UShort = 65400, s1 As Short = 300, sb As SByte = -128
        Assert.Equal(CByte(135), CByte(Not us))
        Assert.Equal(55, CInt(Not b1))
        Assert.Equal(CByte(44), CByte(s1 And 255S))
        Assert.Equal(127L, CLng(Not sb))
        Assert.Equal(55.0, CDbl(Not b1))
        Dim f As Func(Of Short) = Function() Not b1
        Assert.Equal(55S, f())
        Dim i As Integer = b1 Mod CByte(7)
        Assert.Equal(4, i)
        Dim j As Integer = +b1
        Assert.Equal(200, j)
    End Sub
End Class
