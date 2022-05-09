Imports System
Imports System.Linq
Imports Xunit

Public Class EnumTests
    Private Enum RankEnum As SByte
        First = 1
        Second = 2
        Third = 3
    End Enum

    Private Class AClass
        Public Property TheDateTime As DateTime
        Public Property TheString As String
        Public Property TheInteger As Integer
        Public Property TheDriveType As System.IO.DriveType
    End Class

    <Fact>
    Sub TestEnumCType()
        Dim eEnum = RankEnum.Second
        Dim sEnum = "2" 'Has to be an integer within the string, CType doesn't parse enums
        Dim iEnum = 2
        Dim boxedString = CType(sEnum, Object)
        Dim boxedInt = CType(iEnum, Object)

        Dim enumToString = CType(eEnum, String)
        Dim enumToInt = CType(eEnum, Integer)
        Dim stringToEnum = CType(sEnum, RankEnum)
        Dim intToEnum = CType(iEnum, RankEnum)
        Dim boxedStringToEnum = CType(boxedString, RankEnum)
        Dim boxedIntToEnum = CType(boxedInt, RankEnum)

        Assert.Equal(sEnum, enumToString)
        Assert.Equal(iEnum, enumToInt)
        Assert.Equal(eEnum, stringToEnum)
        Assert.Equal(eEnum, intToEnum)
        Assert.Equal(eEnum, boxedStringToEnum)
        Assert.Equal(eEnum, boxedIntToEnum)
    End Sub

    <Fact>
    Sub TestEnumConversions()
        Dim eEnum = RankEnum.Second
        Dim sEnum = "2" 'Has to be an integer within the string, CType doesn't parse enums
        Dim iEnum = 2
        Dim boxedString = CType(sEnum, Object)
        Dim boxedInt = CType(iEnum, Object)

        Dim enumToString As String = eEnum
        Dim enumToInt As Integer = iEnum
        Dim stringToEnum As RankEnum = sEnum
        Dim intToEnum As RankEnum = iEnum
        Dim boxedStringToEnum As RankEnum = boxedString
        Dim boxedIntToEnum As RankEnum = boxedInt

        Assert.Equal(sEnum, enumToString)
        Assert.Equal(iEnum, enumToInt)
        Assert.Equal(eEnum, stringToEnum)
        Assert.Equal(eEnum, intToEnum)
        Assert.Equal(eEnum, boxedStringToEnum)
        Assert.Equal(eEnum, boxedIntToEnum)
    End Sub

    <Fact>
    Sub TestEnumEquality()
        Dim eEnum = RankEnum.Second
        Dim enumEnumEquality As Boolean = eEnum = RankEnum.First
        Dim enumIntEquality As Boolean = eEnum = 2
        Dim enumNothingEquality As Boolean = eEnum = Nothing
        Dim enumNotEqualNothingEquality As Boolean = eEnum <> Nothing

        Assert.Equal(False, enumEnumEquality)
        Assert.Equal(True, enumIntEquality)
        Assert.Equal(False, enumNothingEquality)
        Assert.Equal(True, enumNotEqualNothingEquality)
    End Sub

    <Fact>
    Public Sub TestWithCastsAndGlobalImport()
        Dim a = New AClass With {
                .TheDateTime = Now,
                .TheString = System.IO.DriveType.Network,
                .TheInteger = System.IO.DriveType.Fixed,
                .TheDriveType = 3
                }
        Console.WriteLine(a)
    End Sub

    <Fact>
    Public Sub TestEnumConcat()
        Assert.Equal(RankEnum.Second.ToString() & RankEnum.Second, "Second2")
    End Sub

    <Fact>
    Public Sub TestEnumstringCoerce()
        Dim s As String = RankEnum.Second
        Assert.Equal(s & RankEnum.Second, "22")
    End Sub

    <Fact>
    Public Sub NegatedEnumNegatesUnderlyingNumber()
        Dim initialEnum = RankEnum.First Or RankEnum.Second
        Dim withSecondRemoved = initialEnum And Not RankEnum.Second
        Dim i As RankEnum = (1 Or 2) And Not 2
        Assert.Equal(withSecondRemoved, i)
        Assert.Equal(withSecondRemoved, RankEnum.First)
    End Sub
End Class
