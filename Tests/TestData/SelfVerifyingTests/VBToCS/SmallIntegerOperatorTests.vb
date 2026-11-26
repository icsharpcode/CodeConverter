Imports System
Imports Xunit

Public Class SmallIntegerOperatorTests

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
End Class
