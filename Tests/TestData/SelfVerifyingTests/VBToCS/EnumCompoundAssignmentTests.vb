Option Strict Off
Imports System
Imports Xunit

Public Class EnumCompoundAssignmentTests
    Public Enum E As Short
        A = 1
        B = 5
    End Enum

    <Fact>
    Public Sub OperatorsCSharpLacksOnEnums()
        Dim i As Integer = 2
        Dim x As E = E.B
        x *= 3
        Dim afterMultiply = CInt(x)
        x \= 2
        Dim afterIntDivide = CInt(x)
        x -= i
        Dim afterSubtract = CInt(x)
        x += E.A
        Dim afterAddEnum = CInt(x)
        x /= 4
        Dim n As E? = Nothing
        n *= 3
        Dim m As E? = E.A
        m *= 3
        Assert.Equal("15,7,5,6,2,True,3", String.Join(",", afterMultiply, afterIntDivide, afterSubtract, afterAddEnum, CInt(x), n Is Nothing, CInt(m)))
    End Sub

    Public Enum ByteEnum As Byte
        A = 1
    End Enum
    Public Enum SByteEnum As SByte
        A = 100
    End Enum
    Public Enum IntegerEnum As Integer
        A = 1
    End Enum
    Public Enum ULongEnum As ULong
        A = 1
    End Enum

    <Fact>
    Public Sub PlusOrMinusConstantThatDoesntFitTheUnderlyingType()
        Dim b = ByteEnum.A
        b += -1
        Dim s = SByteEnum.A
        s -= 200
        Dim i = IntegerEnum.A
        i += 1L
        i += 1UI
        Dim u = ULongEnum.A
        u += 1
        Assert.Equal("0,-100,3,2", String.Join(",", CInt(b), CInt(s), CInt(i), CULng(u)))
    End Sub
End Class
