Imports System
Imports Xunit

Public Enum ByteFlags As Byte
    Top = 128
End Enum

Public Enum RoundedEnum
    A = 1.5
    B = 3.5
End Enum

Public Class SmallIntegerOperatorTests
    Const C1 As UShort = 65534, C2 As UShort = 40000
    Const CB As Byte = 200
    Const K1 As Byte = CB << 1
    Const HalfD As Double = 2.5
    Const OneAndHalf As Double = 1.5
    Const FourPointFour As Double = 4.4
    Const NotHalfD As Long = Not HalfD

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

    <Fact>
    Public Sub ShiftCountsAreMaskedForConstantsAndEnums()
        Dim b1 As Byte = 200, n As Integer = 9
        Const Nine As Byte = 9
        Assert.Equal(CByte(144), b1 << 9L)
        Assert.Equal(CByte(144), b1 << Nine)
        Assert.Equal(CByte(144), b1 << (n And 1))
        Dim flags As ByteFlags = ByteFlags.Top
        flags >>= n
        Assert.Equal(CType(64, ByteFlags), flags)
        Dim s As Short = 300, b2 As Byte = 100
        s += s << 1
        Assert.Equal(900S, s)
        b2 += b2 + CByte(50)
        Assert.Equal(CByte(250), b2)
    End Sub

    <Fact>
    Public Sub CoalesceAndInterpolationOfResults()
        Dim nb1 As Byte? = 200, nb2 As Byte? = 50, noValue As Byte? = Nothing, sb As SByte = -1, b1 As Byte = 200
        Assert.Equal(0, If(nb1 And nb2, 7))
        Assert.Equal(7, If(noValue And nb2, 7))
        Assert.Equal(55, If(Not nb1, 7))
        Assert.Equal("FF", $"{sb And sb:X}")
        Assert.Equal("  200", $"{b1 Or b1,5}")
    End Sub

    Private _q As Byte = 200
    Private Property Q(i As Integer) As Byte
        Get
            Return _q
        End Get
        Set(value As Byte)
            _q = value
        End Set
    End Property

    <Fact>
    Public Sub CompoundAssignmentsToParameterizedProperty()
        Dim n = 9
        Q(1) <<= n
        Assert.Equal(CByte(144), Q(1))
        Q(1) += CByte(50)
        Assert.Equal(CByte(194), Q(1))
        Q(1) >>= 9
        Assert.Equal(CByte(97), Q(1))
    End Sub

    <Fact>
    Public Sub LambdaBodiesConvertToTheReturnType()
        Dim i As Integer = 300, b1 As Byte = 200
        Dim narrowed As Func(Of Byte) = Function() i - 100
        Assert.Equal(CByte(200), narrowed())
        Dim tree As System.Linq.Expressions.Expression(Of Func(Of Short)) = Function() b1 Or CByte(1)
        Assert.Equal(201S, tree.Compile()())
        Dim nested As Func(Of Func(Of Integer)) = Function() Function() Not b1
        Assert.Equal(55, nested()())
        Dim boxed As Func(Of Object) = Function() b1 Or b1
        Assert.Equal(GetType(Byte), boxed().GetType())
    End Sub

    <Fact>
    Public Sub ConversionsToEnumOrFromNullable()
        Dim b1 As Byte = 200, nb As Byte? = 100, noValue As Byte? = Nothing
        Assert.Equal(CType(55, ByteFlags), CType(Not b1, ByteFlags))
        Assert.Equal(CByte(200), CByte(nb + nb))
        Assert.Throws(Of InvalidOperationException)(Function() CByte(noValue + nb))
        Assert.Equal(CType(55, DayOfWeek), CType(Not b1, DayOfWeek))
    End Sub

    <Fact>
    Public Sub NotOfFractionalRoundsToLong()
        Dim d As Double = 2.5, d2 As Double = 3.5, nd As Double? = 2.5, noValue As Double? = Nothing, m As Decimal = 2.5D, f As Single = 3.5F
        Assert.Equal(-3L, Not d)
        Assert.Equal(-5L, Not d2)
        Assert.Equal(-3L, Not nd)
        Assert.False((Not noValue).HasValue)
        Assert.Equal(-3L, Not m)
        Assert.Equal(-5L, Not f)
    End Sub

    <Fact>
    Public Sub NegativeHexIntegerLiteralInsideUncheckedConstant()
        Dim b As Byte = Not CByte((&H80000010 \ 3) And &HFF)
        Assert.Equal(CByte(164), b)
        Dim us As UShort = Not CUShort((&H80000010 >> 20) And &HFFFF)
        Assert.Equal(CUShort(2047), us)
        Dim m As Byte = Not CByte((&H80000010 Mod 7) + 7)
        Assert.Equal(CByte(248), m)
        Assert.True((Not CByte((&H80000010 \ 3) And &HFF)) > 100)
    End Sub

    <Fact>
    Public Sub FractionalConstantsRoundToEven()
        Assert.Equal(-3L, NotHalfD)
        Dim x As Long = 2.5
        Assert.Equal(2L, x)
        Dim y As Long = 3.5
        Assert.Equal(4L, y)
        Dim b As Byte = 4.5
        Assert.Equal(CByte(4), b)
        Select Case -3L
            Case Not HalfD
            Case Else
                Assert.Fail("Not HalfD")
        End Select
    End Sub

    <Fact>
    Public Sub CompoundShiftsOfNonIntegralLeftSides()
        Dim bo As Boolean = True, d As Double = 2.5, s As String = "3", nd As Double? = 1.5, noValue As Double? = Nothing
        Dim f As Single = 9.5F, m As Decimal = 2.5D
        bo <<= 1
        Assert.True(bo)
        d <<= 2
        Assert.Equal(8.0, d)
        s <<= 2
        Assert.Equal("12", s)
        nd <<= 2
        Assert.Equal(8.0, nd)
        noValue <<= 2
        Assert.False(noValue.HasValue)
        f >>= 1
        Assert.Equal(5.0F, f)
        m <<= 1
        Assert.Equal(4D, m)
        Dim x As Short = True << 3
        Assert.Equal(-8S, x)
    End Sub

    <Fact>
    Public Sub SelectCaseAndWiderEnumOfResults()
        Dim b1 As Byte = 200, b2 As Byte = 72, result = ""
        Select Case b1 And b2
            Case 72 : result &= "a"
            Case Else : result &= "x"
        End Select
        Select Case Not b1
            Case 55 : result &= "b"
            Case Else : result &= "y"
        End Select
        Assert.Equal("ab", result)
        Assert.Equal(CType(200, DayOfWeek), CType(b1 Or b1, DayOfWeek))
    End Sub

    Private Property IntProperty As Integer = 1

    <Fact>
    Public Sub CompoundAssignmentsWithFractionalConstants()
        Dim i As Integer = 10, b As Byte = 10, l As Long = 10, s As Short = 10, ni As Integer? = 10
        i += 2.5
        Assert.Equal(12, i)
        b *= 1.5
        Assert.Equal(CByte(15), b)
        l -= OneAndHalf
        Assert.Equal(8L, l)
        s /= 4.0
        Assert.Equal(2S, s)
        IntProperty += 0.5
        Assert.Equal(2, IntProperty)
        ni += 2.5
        Assert.Equal(12, ni)
        Q(1) = 200
        Q(1) += 2.5
        Assert.Equal(CByte(202), Q(1))
    End Sub

    <Fact>
    Public Sub ArrayBoundsWithFractionalConstants()
        Dim c(FourPointFour) As Integer
        Assert.Equal(5, c.Length)
        Dim a(2.5) As Integer
        Assert.Equal(3, a.Length)
        ReDim a(OneAndHalf)
        Assert.Equal(3, a.Length)
        Dim d(3.5) As Integer
        Assert.Equal(5, d.Length)
    End Sub

    Private Function TakeValue(i As Integer) As String
        Return "Integer"
    End Function

    Private Function TakeValue(b As Byte) As String
        Return "Byte"
    End Function

    <Fact>
    Public Sub CoalesceWithSmallIntegerSecondOperand()
        Dim nb As Byte? = 200, b As Byte = 7, b2 As Byte = 72
        Assert.Equal(CByte(55), If(Not nb, CByte(0)))
        Dim o As Object = If(nb << 2, b)
        Assert.Equal(CByte(32), o)
        Dim s As String = If(Not nb, b)
        Assert.Equal("55", s)
        Dim o2 As Object = If(nb And b2, CByte(0))
        Assert.Equal(GetType(Byte), o2.GetType())
        Assert.Equal("Byte", TakeValue(If(nb And b2, CByte(0))))
        Dim v = If(nb And b2, CByte(0))
        Assert.Equal(CByte(72), v)
    End Sub

    <Fact>
    Public Sub LateBoundCompoundAssignments()
        Dim b As Byte = 200, ob As Object = 9, o As Object = 3, i As Integer = 1024
        b <<= ob
        Assert.Equal(CByte(144), b)
        i >>= ob
        Assert.Equal(2, i)
        o <<= 2
        Assert.Equal(12, o)
        o += 1
        Assert.Equal(13, o)
        b += ob
        Assert.Equal(CByte(153), b)
        Dim b2 As Byte = 200
        b2 <<= 1.5
        Assert.Equal(CByte(32), b2)
        Assert.Equal(CByte(32), CByte(200) << 2.5)
        Assert.Equal(CByte(32), CByte(200) << 9.5)
    End Sub

    Private Function Defaults(Optional x As Integer = 1.5, Optional y As Long = OneAndHalf, Optional z As Short = True << 3) As String
        Return $"{x} {y} {z}"
    End Function

    <Fact>
    Public Sub ConstantDefaultsAndEnumMembersAreConvertedAtCompileTime()
        Assert.Equal("2 2 -8", Defaults())
        Assert.Equal(2, CInt(RoundedEnum.A))
        Assert.Equal(4, CInt(RoundedEnum.B))
    End Sub
End Class
