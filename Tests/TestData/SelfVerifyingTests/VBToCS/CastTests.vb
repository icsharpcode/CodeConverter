Imports System
Imports System.Linq
Imports Xunit

Public Class CastTests
    <Fact>
    Sub TestCast()
        Dim vBoolean As Boolean = Nothing
        Dim vSByte As SByte = Nothing
        Dim vByte As Byte = Nothing
        Dim vShort As Short = Nothing
        Dim vUShort As UShort = Nothing
        Dim vInteger As Integer = Nothing
        Dim vUInteger As UInteger = Nothing
        Dim vLong As Long = Nothing
        Dim vULong As ULong = Nothing
        Dim vDecimal As Decimal = Nothing
        Dim vSingle As Single = Nothing
        Dim vDouble As Double = Nothing
        Dim vDate As Date = Nothing
        Dim vChar As Char = Nothing
        Dim vString As String = Nothing
        Dim vObject As Object = Nothing

        Dim vBooleanCatBoolean = vBoolean & vBoolean
        Assert.Equal(vBooleanCatBoolean.GetType(), GetType(String))
        Dim vBooleanCatSByte = vBoolean & vSByte
        Assert.Equal(vBooleanCatSByte.GetType(), GetType(String))
        Dim vBooleanCatByte = vBoolean & vByte
        Assert.Equal(vBooleanCatByte.GetType(), GetType(String))
        Dim vBooleanCatShort = vBoolean & vShort
        Assert.Equal(vBooleanCatShort.GetType(), GetType(String))
        Dim vBooleanCatUShort = vBoolean & vUShort
        Assert.Equal(vBooleanCatUShort.GetType(), GetType(String))
        Dim vBooleanCatInteger = vBoolean & vInteger
        Assert.Equal(vBooleanCatInteger.GetType(), GetType(String))
        Dim vBooleanCatUInteger = vBoolean & vUInteger
        Assert.Equal(vBooleanCatUInteger.GetType(), GetType(String))
        Dim vBooleanCatLong = vBoolean & vLong
        Assert.Equal(vBooleanCatLong.GetType(), GetType(String))
        Dim vBooleanCatULong = vBoolean & vULong
        Assert.Equal(vBooleanCatULong.GetType(), GetType(String))
        Dim vBooleanCatDecimal = vBoolean & vDecimal
        Assert.Equal(vBooleanCatDecimal.GetType(), GetType(String))
        Dim vBooleanCatSingle = vBoolean & vSingle
        Assert.Equal(vBooleanCatSingle.GetType(), GetType(String))
        Dim vBooleanCatDouble = vBoolean & vDouble
        Assert.Equal(vBooleanCatDouble.GetType(), GetType(String))
        Dim vBooleanCatDate = vBoolean & vDate
        Assert.Equal(vBooleanCatDate.GetType(), GetType(String))
        Dim vBooleanCatChar = vBoolean & vChar
        Assert.Equal(vBooleanCatChar.GetType(), GetType(String))
        Dim vBooleanCatString = vBoolean & vString
        Assert.Equal(vBooleanCatString.GetType(), GetType(String))
        Dim vBooleanCatObject = vBoolean & vObject
        Assert.Equal(vBooleanCatObject.GetType(), GetType(String))
        Dim vSByteCatBoolean = vSByte & vBoolean
        Assert.Equal(vSByteCatBoolean.GetType(), GetType(String))
        Dim vSByteCatSByte = vSByte & vSByte
        Assert.Equal(vSByteCatSByte.GetType(), GetType(String))
        Dim vSByteCatByte = vSByte & vByte
        Assert.Equal(vSByteCatByte.GetType(), GetType(String))
        Dim vSByteCatShort = vSByte & vShort
        Assert.Equal(vSByteCatShort.GetType(), GetType(String))
        Dim vSByteCatUShort = vSByte & vUShort
        Assert.Equal(vSByteCatUShort.GetType(), GetType(String))
        Dim vSByteCatInteger = vSByte & vInteger
        Assert.Equal(vSByteCatInteger.GetType(), GetType(String))
        Dim vSByteCatUInteger = vSByte & vUInteger
        Assert.Equal(vSByteCatUInteger.GetType(), GetType(String))
        Dim vSByteCatLong = vSByte & vLong
        Assert.Equal(vSByteCatLong.GetType(), GetType(String))
        Dim vSByteCatULong = vSByte & vULong
        Assert.Equal(vSByteCatULong.GetType(), GetType(String))
        Dim vSByteCatDecimal = vSByte & vDecimal
        Assert.Equal(vSByteCatDecimal.GetType(), GetType(String))
        Dim vSByteCatSingle = vSByte & vSingle
        Assert.Equal(vSByteCatSingle.GetType(), GetType(String))
        Dim vSByteCatDouble = vSByte & vDouble
        Assert.Equal(vSByteCatDouble.GetType(), GetType(String))
        Dim vSByteCatDate = vSByte & vDate
        Assert.Equal(vSByteCatDate.GetType(), GetType(String))
        Dim vSByteCatChar = vSByte & vChar
        Assert.Equal(vSByteCatChar.GetType(), GetType(String))
        Dim vSByteCatString = vSByte & vString
        Assert.Equal(vSByteCatString.GetType(), GetType(String))
        Dim vSByteCatObject = vSByte & vObject
        Assert.Equal(vSByteCatObject.GetType(), GetType(String))
        Dim vByteCatBoolean = vByte & vBoolean
        Assert.Equal(vByteCatBoolean.GetType(), GetType(String))
        Dim vByteCatSByte = vByte & vSByte
        Assert.Equal(vByteCatSByte.GetType(), GetType(String))
        Dim vByteCatByte = vByte & vByte
        Assert.Equal(vByteCatByte.GetType(), GetType(String))
        Dim vByteCatShort = vByte & vShort
        Assert.Equal(vByteCatShort.GetType(), GetType(String))
        Dim vByteCatUShort = vByte & vUShort
        Assert.Equal(vByteCatUShort.GetType(), GetType(String))
        Dim vByteCatInteger = vByte & vInteger
        Assert.Equal(vByteCatInteger.GetType(), GetType(String))
        Dim vByteCatUInteger = vByte & vUInteger
        Assert.Equal(vByteCatUInteger.GetType(), GetType(String))
        Dim vByteCatLong = vByte & vLong
        Assert.Equal(vByteCatLong.GetType(), GetType(String))
        Dim vByteCatULong = vByte & vULong
        Assert.Equal(vByteCatULong.GetType(), GetType(String))
        Dim vByteCatDecimal = vByte & vDecimal
        Assert.Equal(vByteCatDecimal.GetType(), GetType(String))
        Dim vByteCatSingle = vByte & vSingle
        Assert.Equal(vByteCatSingle.GetType(), GetType(String))
        Dim vByteCatDouble = vByte & vDouble
        Assert.Equal(vByteCatDouble.GetType(), GetType(String))
        Dim vByteCatDate = vByte & vDate
        Assert.Equal(vByteCatDate.GetType(), GetType(String))
        Dim vByteCatChar = vByte & vChar
        Assert.Equal(vByteCatChar.GetType(), GetType(String))
        Dim vByteCatString = vByte & vString
        Assert.Equal(vByteCatString.GetType(), GetType(String))
        Dim vByteCatObject = vByte & vObject
        Assert.Equal(vByteCatObject.GetType(), GetType(String))
        Dim vShortCatBoolean = vShort & vBoolean
        Assert.Equal(vShortCatBoolean.GetType(), GetType(String))
        Dim vShortCatSByte = vShort & vSByte
        Assert.Equal(vShortCatSByte.GetType(), GetType(String))
        Dim vShortCatByte = vShort & vByte
        Assert.Equal(vShortCatByte.GetType(), GetType(String))
        Dim vShortCatShort = vShort & vShort
        Assert.Equal(vShortCatShort.GetType(), GetType(String))
        Dim vShortCatUShort = vShort & vUShort
        Assert.Equal(vShortCatUShort.GetType(), GetType(String))
        Dim vShortCatInteger = vShort & vInteger
        Assert.Equal(vShortCatInteger.GetType(), GetType(String))
        Dim vShortCatUInteger = vShort & vUInteger
        Assert.Equal(vShortCatUInteger.GetType(), GetType(String))
        Dim vShortCatLong = vShort & vLong
        Assert.Equal(vShortCatLong.GetType(), GetType(String))
        Dim vShortCatULong = vShort & vULong
        Assert.Equal(vShortCatULong.GetType(), GetType(String))
        Dim vShortCatDecimal = vShort & vDecimal
        Assert.Equal(vShortCatDecimal.GetType(), GetType(String))
        Dim vShortCatSingle = vShort & vSingle
        Assert.Equal(vShortCatSingle.GetType(), GetType(String))
        Dim vShortCatDouble = vShort & vDouble
        Assert.Equal(vShortCatDouble.GetType(), GetType(String))
        Dim vShortCatDate = vShort & vDate
        Assert.Equal(vShortCatDate.GetType(), GetType(String))
        Dim vShortCatChar = vShort & vChar
        Assert.Equal(vShortCatChar.GetType(), GetType(String))
        Dim vShortCatString = vShort & vString
        Assert.Equal(vShortCatString.GetType(), GetType(String))
        Dim vShortCatObject = vShort & vObject
        Assert.Equal(vShortCatObject.GetType(), GetType(String))
        Dim vUShortCatBoolean = vUShort & vBoolean
        Assert.Equal(vUShortCatBoolean.GetType(), GetType(String))
        Dim vUShortCatSByte = vUShort & vSByte
        Assert.Equal(vUShortCatSByte.GetType(), GetType(String))
        Dim vUShortCatByte = vUShort & vByte
        Assert.Equal(vUShortCatByte.GetType(), GetType(String))
        Dim vUShortCatShort = vUShort & vShort
        Assert.Equal(vUShortCatShort.GetType(), GetType(String))
        Dim vUShortCatUShort = vUShort & vUShort
        Assert.Equal(vUShortCatUShort.GetType(), GetType(String))
        Dim vUShortCatInteger = vUShort & vInteger
        Assert.Equal(vUShortCatInteger.GetType(), GetType(String))
        Dim vUShortCatUInteger = vUShort & vUInteger
        Assert.Equal(vUShortCatUInteger.GetType(), GetType(String))
        Dim vUShortCatLong = vUShort & vLong
        Assert.Equal(vUShortCatLong.GetType(), GetType(String))
        Dim vUShortCatULong = vUShort & vULong
        Assert.Equal(vUShortCatULong.GetType(), GetType(String))
        Dim vUShortCatDecimal = vUShort & vDecimal
        Assert.Equal(vUShortCatDecimal.GetType(), GetType(String))
        Dim vUShortCatSingle = vUShort & vSingle
        Assert.Equal(vUShortCatSingle.GetType(), GetType(String))
        Dim vUShortCatDouble = vUShort & vDouble
        Assert.Equal(vUShortCatDouble.GetType(), GetType(String))
        Dim vUShortCatDate = vUShort & vDate
        Assert.Equal(vUShortCatDate.GetType(), GetType(String))
        Dim vUShortCatChar = vUShort & vChar
        Assert.Equal(vUShortCatChar.GetType(), GetType(String))
        Dim vUShortCatString = vUShort & vString
        Assert.Equal(vUShortCatString.GetType(), GetType(String))
        Dim vUShortCatObject = vUShort & vObject
        Assert.Equal(vUShortCatObject.GetType(), GetType(String))
        Dim vIntegerCatBoolean = vInteger & vBoolean
        Assert.Equal(vIntegerCatBoolean.GetType(), GetType(String))
        Dim vIntegerCatSByte = vInteger & vSByte
        Assert.Equal(vIntegerCatSByte.GetType(), GetType(String))
        Dim vIntegerCatByte = vInteger & vByte
        Assert.Equal(vIntegerCatByte.GetType(), GetType(String))
        Dim vIntegerCatShort = vInteger & vShort
        Assert.Equal(vIntegerCatShort.GetType(), GetType(String))
        Dim vIntegerCatUShort = vInteger & vUShort
        Assert.Equal(vIntegerCatUShort.GetType(), GetType(String))
        Dim vIntegerCatInteger = vInteger & vInteger
        Assert.Equal(vIntegerCatInteger.GetType(), GetType(String))
        Dim vIntegerCatUInteger = vInteger & vUInteger
        Assert.Equal(vIntegerCatUInteger.GetType(), GetType(String))
        Dim vIntegerCatLong = vInteger & vLong
        Assert.Equal(vIntegerCatLong.GetType(), GetType(String))
        Dim vIntegerCatULong = vInteger & vULong
        Assert.Equal(vIntegerCatULong.GetType(), GetType(String))
        Dim vIntegerCatDecimal = vInteger & vDecimal
        Assert.Equal(vIntegerCatDecimal.GetType(), GetType(String))
        Dim vIntegerCatSingle = vInteger & vSingle
        Assert.Equal(vIntegerCatSingle.GetType(), GetType(String))
        Dim vIntegerCatDouble = vInteger & vDouble
        Assert.Equal(vIntegerCatDouble.GetType(), GetType(String))
        Dim vIntegerCatDate = vInteger & vDate
        Assert.Equal(vIntegerCatDate.GetType(), GetType(String))
        Dim vIntegerCatChar = vInteger & vChar
        Assert.Equal(vIntegerCatChar.GetType(), GetType(String))
        Dim vIntegerCatString = vInteger & vString
        Assert.Equal(vIntegerCatString.GetType(), GetType(String))
        Dim vIntegerCatObject = vInteger & vObject
        Assert.Equal(vIntegerCatObject.GetType(), GetType(String))
        Dim vUIntegerCatBoolean = vUInteger & vBoolean
        Assert.Equal(vUIntegerCatBoolean.GetType(), GetType(String))
        Dim vUIntegerCatSByte = vUInteger & vSByte
        Assert.Equal(vUIntegerCatSByte.GetType(), GetType(String))
        Dim vUIntegerCatByte = vUInteger & vByte
        Assert.Equal(vUIntegerCatByte.GetType(), GetType(String))
        Dim vUIntegerCatShort = vUInteger & vShort
        Assert.Equal(vUIntegerCatShort.GetType(), GetType(String))
        Dim vUIntegerCatUShort = vUInteger & vUShort
        Assert.Equal(vUIntegerCatUShort.GetType(), GetType(String))
        Dim vUIntegerCatInteger = vUInteger & vInteger
        Assert.Equal(vUIntegerCatInteger.GetType(), GetType(String))
        Dim vUIntegerCatUInteger = vUInteger & vUInteger
        Assert.Equal(vUIntegerCatUInteger.GetType(), GetType(String))
        Dim vUIntegerCatLong = vUInteger & vLong
        Assert.Equal(vUIntegerCatLong.GetType(), GetType(String))
        Dim vUIntegerCatULong = vUInteger & vULong
        Assert.Equal(vUIntegerCatULong.GetType(), GetType(String))
        Dim vUIntegerCatDecimal = vUInteger & vDecimal
        Assert.Equal(vUIntegerCatDecimal.GetType(), GetType(String))
        Dim vUIntegerCatSingle = vUInteger & vSingle
        Assert.Equal(vUIntegerCatSingle.GetType(), GetType(String))
        Dim vUIntegerCatDouble = vUInteger & vDouble
        Assert.Equal(vUIntegerCatDouble.GetType(), GetType(String))
        Dim vUIntegerCatDate = vUInteger & vDate
        Assert.Equal(vUIntegerCatDate.GetType(), GetType(String))
        Dim vUIntegerCatChar = vUInteger & vChar
        Assert.Equal(vUIntegerCatChar.GetType(), GetType(String))
        Dim vUIntegerCatString = vUInteger & vString
        Assert.Equal(vUIntegerCatString.GetType(), GetType(String))
        Dim vUIntegerCatObject = vUInteger & vObject
        Assert.Equal(vUIntegerCatObject.GetType(), GetType(String))
        Dim vLongCatBoolean = vLong & vBoolean
        Assert.Equal(vLongCatBoolean.GetType(), GetType(String))
        Dim vLongCatSByte = vLong & vSByte
        Assert.Equal(vLongCatSByte.GetType(), GetType(String))
        Dim vLongCatByte = vLong & vByte
        Assert.Equal(vLongCatByte.GetType(), GetType(String))
        Dim vLongCatShort = vLong & vShort
        Assert.Equal(vLongCatShort.GetType(), GetType(String))
        Dim vLongCatUShort = vLong & vUShort
        Assert.Equal(vLongCatUShort.GetType(), GetType(String))
        Dim vLongCatInteger = vLong & vInteger
        Assert.Equal(vLongCatInteger.GetType(), GetType(String))
        Dim vLongCatUInteger = vLong & vUInteger
        Assert.Equal(vLongCatUInteger.GetType(), GetType(String))
        Dim vLongCatLong = vLong & vLong
        Assert.Equal(vLongCatLong.GetType(), GetType(String))
        Dim vLongCatULong = vLong & vULong
        Assert.Equal(vLongCatULong.GetType(), GetType(String))
        Dim vLongCatDecimal = vLong & vDecimal
        Assert.Equal(vLongCatDecimal.GetType(), GetType(String))
        Dim vLongCatSingle = vLong & vSingle
        Assert.Equal(vLongCatSingle.GetType(), GetType(String))
        Dim vLongCatDouble = vLong & vDouble
        Assert.Equal(vLongCatDouble.GetType(), GetType(String))
        Dim vLongCatDate = vLong & vDate
        Assert.Equal(vLongCatDate.GetType(), GetType(String))
        Dim vLongCatChar = vLong & vChar
        Assert.Equal(vLongCatChar.GetType(), GetType(String))
        Dim vLongCatString = vLong & vString
        Assert.Equal(vLongCatString.GetType(), GetType(String))
        Dim vLongCatObject = vLong & vObject
        Assert.Equal(vLongCatObject.GetType(), GetType(String))
        Dim vULongCatBoolean = vULong & vBoolean
        Assert.Equal(vULongCatBoolean.GetType(), GetType(String))
        Dim vULongCatSByte = vULong & vSByte
        Assert.Equal(vULongCatSByte.GetType(), GetType(String))
        Dim vULongCatByte = vULong & vByte
        Assert.Equal(vULongCatByte.GetType(), GetType(String))
        Dim vULongCatShort = vULong & vShort
        Assert.Equal(vULongCatShort.GetType(), GetType(String))
        Dim vULongCatUShort = vULong & vUShort
        Assert.Equal(vULongCatUShort.GetType(), GetType(String))
        Dim vULongCatInteger = vULong & vInteger
        Assert.Equal(vULongCatInteger.GetType(), GetType(String))
        Dim vULongCatUInteger = vULong & vUInteger
        Assert.Equal(vULongCatUInteger.GetType(), GetType(String))
        Dim vULongCatLong = vULong & vLong
        Assert.Equal(vULongCatLong.GetType(), GetType(String))
        Dim vULongCatULong = vULong & vULong
        Assert.Equal(vULongCatULong.GetType(), GetType(String))
        Dim vULongCatDecimal = vULong & vDecimal
        Assert.Equal(vULongCatDecimal.GetType(), GetType(String))
        Dim vULongCatSingle = vULong & vSingle
        Assert.Equal(vULongCatSingle.GetType(), GetType(String))
        Dim vULongCatDouble = vULong & vDouble
        Assert.Equal(vULongCatDouble.GetType(), GetType(String))
        Dim vULongCatDate = vULong & vDate
        Assert.Equal(vULongCatDate.GetType(), GetType(String))
        Dim vULongCatChar = vULong & vChar
        Assert.Equal(vULongCatChar.GetType(), GetType(String))
        Dim vULongCatString = vULong & vString
        Assert.Equal(vULongCatString.GetType(), GetType(String))
        Dim vULongCatObject = vULong & vObject
        Assert.Equal(vULongCatObject.GetType(), GetType(String))
        Dim vDecimalCatBoolean = vDecimal & vBoolean
        Assert.Equal(vDecimalCatBoolean.GetType(), GetType(String))
        Dim vDecimalCatSByte = vDecimal & vSByte
        Assert.Equal(vDecimalCatSByte.GetType(), GetType(String))
        Dim vDecimalCatByte = vDecimal & vByte
        Assert.Equal(vDecimalCatByte.GetType(), GetType(String))
        Dim vDecimalCatShort = vDecimal & vShort
        Assert.Equal(vDecimalCatShort.GetType(), GetType(String))
        Dim vDecimalCatUShort = vDecimal & vUShort
        Assert.Equal(vDecimalCatUShort.GetType(), GetType(String))
        Dim vDecimalCatInteger = vDecimal & vInteger
        Assert.Equal(vDecimalCatInteger.GetType(), GetType(String))
        Dim vDecimalCatUInteger = vDecimal & vUInteger
        Assert.Equal(vDecimalCatUInteger.GetType(), GetType(String))
        Dim vDecimalCatLong = vDecimal & vLong
        Assert.Equal(vDecimalCatLong.GetType(), GetType(String))
        Dim vDecimalCatULong = vDecimal & vULong
        Assert.Equal(vDecimalCatULong.GetType(), GetType(String))
        Dim vDecimalCatDecimal = vDecimal & vDecimal
        Assert.Equal(vDecimalCatDecimal.GetType(), GetType(String))
        Dim vDecimalCatSingle = vDecimal & vSingle
        Assert.Equal(vDecimalCatSingle.GetType(), GetType(String))
        Dim vDecimalCatDouble = vDecimal & vDouble
        Assert.Equal(vDecimalCatDouble.GetType(), GetType(String))
        Dim vDecimalCatDate = vDecimal & vDate
        Assert.Equal(vDecimalCatDate.GetType(), GetType(String))
        Dim vDecimalCatChar = vDecimal & vChar
        Assert.Equal(vDecimalCatChar.GetType(), GetType(String))
        Dim vDecimalCatString = vDecimal & vString
        Assert.Equal(vDecimalCatString.GetType(), GetType(String))
        Dim vDecimalCatObject = vDecimal & vObject
        Assert.Equal(vDecimalCatObject.GetType(), GetType(String))
        Dim vSingleCatBoolean = vSingle & vBoolean
        Assert.Equal(vSingleCatBoolean.GetType(), GetType(String))
        Dim vSingleCatSByte = vSingle & vSByte
        Assert.Equal(vSingleCatSByte.GetType(), GetType(String))
        Dim vSingleCatByte = vSingle & vByte
        Assert.Equal(vSingleCatByte.GetType(), GetType(String))
        Dim vSingleCatShort = vSingle & vShort
        Assert.Equal(vSingleCatShort.GetType(), GetType(String))
        Dim vSingleCatUShort = vSingle & vUShort
        Assert.Equal(vSingleCatUShort.GetType(), GetType(String))
        Dim vSingleCatInteger = vSingle & vInteger
        Assert.Equal(vSingleCatInteger.GetType(), GetType(String))
        Dim vSingleCatUInteger = vSingle & vUInteger
        Assert.Equal(vSingleCatUInteger.GetType(), GetType(String))
        Dim vSingleCatLong = vSingle & vLong
        Assert.Equal(vSingleCatLong.GetType(), GetType(String))
        Dim vSingleCatULong = vSingle & vULong
        Assert.Equal(vSingleCatULong.GetType(), GetType(String))
        Dim vSingleCatDecimal = vSingle & vDecimal
        Assert.Equal(vSingleCatDecimal.GetType(), GetType(String))
        Dim vSingleCatSingle = vSingle & vSingle
        Assert.Equal(vSingleCatSingle.GetType(), GetType(String))
        Dim vSingleCatDouble = vSingle & vDouble
        Assert.Equal(vSingleCatDouble.GetType(), GetType(String))
        Dim vSingleCatDate = vSingle & vDate
        Assert.Equal(vSingleCatDate.GetType(), GetType(String))
        Dim vSingleCatChar = vSingle & vChar
        Assert.Equal(vSingleCatChar.GetType(), GetType(String))
        Dim vSingleCatString = vSingle & vString
        Assert.Equal(vSingleCatString.GetType(), GetType(String))
        Dim vSingleCatObject = vSingle & vObject
        Assert.Equal(vSingleCatObject.GetType(), GetType(String))
        Dim vDoubleCatBoolean = vDouble & vBoolean
        Assert.Equal(vDoubleCatBoolean.GetType(), GetType(String))
        Dim vDoubleCatSByte = vDouble & vSByte
        Assert.Equal(vDoubleCatSByte.GetType(), GetType(String))
        Dim vDoubleCatByte = vDouble & vByte
        Assert.Equal(vDoubleCatByte.GetType(), GetType(String))
        Dim vDoubleCatShort = vDouble & vShort
        Assert.Equal(vDoubleCatShort.GetType(), GetType(String))
        Dim vDoubleCatUShort = vDouble & vUShort
        Assert.Equal(vDoubleCatUShort.GetType(), GetType(String))
        Dim vDoubleCatInteger = vDouble & vInteger
        Assert.Equal(vDoubleCatInteger.GetType(), GetType(String))
        Dim vDoubleCatUInteger = vDouble & vUInteger
        Assert.Equal(vDoubleCatUInteger.GetType(), GetType(String))
        Dim vDoubleCatLong = vDouble & vLong
        Assert.Equal(vDoubleCatLong.GetType(), GetType(String))
        Dim vDoubleCatULong = vDouble & vULong
        Assert.Equal(vDoubleCatULong.GetType(), GetType(String))
        Dim vDoubleCatDecimal = vDouble & vDecimal
        Assert.Equal(vDoubleCatDecimal.GetType(), GetType(String))
        Dim vDoubleCatSingle = vDouble & vSingle
        Assert.Equal(vDoubleCatSingle.GetType(), GetType(String))
        Dim vDoubleCatDouble = vDouble & vDouble
        Assert.Equal(vDoubleCatDouble.GetType(), GetType(String))
        Dim vDoubleCatDate = vDouble & vDate
        Assert.Equal(vDoubleCatDate.GetType(), GetType(String))
        Dim vDoubleCatChar = vDouble & vChar
        Assert.Equal(vDoubleCatChar.GetType(), GetType(String))
        Dim vDoubleCatString = vDouble & vString
        Assert.Equal(vDoubleCatString.GetType(), GetType(String))
        Dim vDoubleCatObject = vDouble & vObject
        Assert.Equal(vDoubleCatObject.GetType(), GetType(String))
        Dim vDateCatBoolean = vDate & vBoolean
        Assert.Equal(vDateCatBoolean.GetType(), GetType(String))
        Dim vDateCatSByte = vDate & vSByte
        Assert.Equal(vDateCatSByte.GetType(), GetType(String))
        Dim vDateCatByte = vDate & vByte
        Assert.Equal(vDateCatByte.GetType(), GetType(String))
        Dim vDateCatShort = vDate & vShort
        Assert.Equal(vDateCatShort.GetType(), GetType(String))
        Dim vDateCatUShort = vDate & vUShort
        Assert.Equal(vDateCatUShort.GetType(), GetType(String))
        Dim vDateCatInteger = vDate & vInteger
        Assert.Equal(vDateCatInteger.GetType(), GetType(String))
        Dim vDateCatUInteger = vDate & vUInteger
        Assert.Equal(vDateCatUInteger.GetType(), GetType(String))
        Dim vDateCatLong = vDate & vLong
        Assert.Equal(vDateCatLong.GetType(), GetType(String))
        Dim vDateCatULong = vDate & vULong
        Assert.Equal(vDateCatULong.GetType(), GetType(String))
        Dim vDateCatDecimal = vDate & vDecimal
        Assert.Equal(vDateCatDecimal.GetType(), GetType(String))
        Dim vDateCatSingle = vDate & vSingle
        Assert.Equal(vDateCatSingle.GetType(), GetType(String))
        Dim vDateCatDouble = vDate & vDouble
        Assert.Equal(vDateCatDouble.GetType(), GetType(String))
        Dim vDateCatDate = vDate & vDate
        Assert.Equal(vDateCatDate.GetType(), GetType(String))
        Dim vDateCatChar = vDate & vChar
        Assert.Equal(vDateCatChar.GetType(), GetType(String))
        Dim vDateCatString = vDate & vString
        Assert.Equal(vDateCatString.GetType(), GetType(String))
        Dim vDateCatObject = vDate & vObject
        Assert.Equal(vDateCatObject.GetType(), GetType(String))
        Dim vCharCatBoolean = vChar & vBoolean
        Assert.Equal(vCharCatBoolean.GetType(), GetType(String))
        Dim vCharCatSByte = vChar & vSByte
        Assert.Equal(vCharCatSByte.GetType(), GetType(String))
        Dim vCharCatByte = vChar & vByte
        Assert.Equal(vCharCatByte.GetType(), GetType(String))
        Dim vCharCatShort = vChar & vShort
        Assert.Equal(vCharCatShort.GetType(), GetType(String))
        Dim vCharCatUShort = vChar & vUShort
        Assert.Equal(vCharCatUShort.GetType(), GetType(String))
        Dim vCharCatInteger = vChar & vInteger
        Assert.Equal(vCharCatInteger.GetType(), GetType(String))
        Dim vCharCatUInteger = vChar & vUInteger
        Assert.Equal(vCharCatUInteger.GetType(), GetType(String))
        Dim vCharCatLong = vChar & vLong
        Assert.Equal(vCharCatLong.GetType(), GetType(String))
        Dim vCharCatULong = vChar & vULong
        Assert.Equal(vCharCatULong.GetType(), GetType(String))
        Dim vCharCatDecimal = vChar & vDecimal
        Assert.Equal(vCharCatDecimal.GetType(), GetType(String))
        Dim vCharCatSingle = vChar & vSingle
        Assert.Equal(vCharCatSingle.GetType(), GetType(String))
        Dim vCharCatDouble = vChar & vDouble
        Assert.Equal(vCharCatDouble.GetType(), GetType(String))
        Dim vCharCatDate = vChar & vDate
        Assert.Equal(vCharCatDate.GetType(), GetType(String))
        Dim vCharCatChar = vChar & vChar
        Assert.Equal(vCharCatChar.GetType(), GetType(String))
        Dim vCharCatString = vChar & vString
        Assert.Equal(vCharCatString.GetType(), GetType(String))
        Dim vCharCatObject = vChar & vObject
        Assert.Equal(vCharCatObject.GetType(), GetType(String))
        Dim vStringCatBoolean = vString & vBoolean
        Assert.Equal(vStringCatBoolean.GetType(), GetType(String))
        Dim vStringCatSByte = vString & vSByte
        Assert.Equal(vStringCatSByte.GetType(), GetType(String))
        Dim vStringCatByte = vString & vByte
        Assert.Equal(vStringCatByte.GetType(), GetType(String))
        Dim vStringCatShort = vString & vShort
        Assert.Equal(vStringCatShort.GetType(), GetType(String))
        Dim vStringCatUShort = vString & vUShort
        Assert.Equal(vStringCatUShort.GetType(), GetType(String))
        Dim vStringCatInteger = vString & vInteger
        Assert.Equal(vStringCatInteger.GetType(), GetType(String))
        Dim vStringCatUInteger = vString & vUInteger
        Assert.Equal(vStringCatUInteger.GetType(), GetType(String))
        Dim vStringCatLong = vString & vLong
        Assert.Equal(vStringCatLong.GetType(), GetType(String))
        Dim vStringCatULong = vString & vULong
        Assert.Equal(vStringCatULong.GetType(), GetType(String))
        Dim vStringCatDecimal = vString & vDecimal
        Assert.Equal(vStringCatDecimal.GetType(), GetType(String))
        Dim vStringCatSingle = vString & vSingle
        Assert.Equal(vStringCatSingle.GetType(), GetType(String))
        Dim vStringCatDouble = vString & vDouble
        Assert.Equal(vStringCatDouble.GetType(), GetType(String))
        Dim vStringCatDate = vString & vDate
        Assert.Equal(vStringCatDate.GetType(), GetType(String))
        Dim vStringCatChar = vString & vChar
        Assert.Equal(vStringCatChar.GetType(), GetType(String))
        Dim vStringCatString = vString & vString
        Assert.Equal(vStringCatString.GetType(), GetType(String))
        Dim vStringCatObject = vString & vObject
        Assert.Equal(vStringCatObject.GetType(), GetType(String))
        Dim vObjectCatBoolean = vObject & vBoolean
        Assert.Equal(vObjectCatBoolean.GetType(), GetType(String))
        Dim vObjectCatSByte = vObject & vSByte
        Assert.Equal(vObjectCatSByte.GetType(), GetType(String))
        Dim vObjectCatByte = vObject & vByte
        Assert.Equal(vObjectCatByte.GetType(), GetType(String))
        Dim vObjectCatShort = vObject & vShort
        Assert.Equal(vObjectCatShort.GetType(), GetType(String))
        Dim vObjectCatUShort = vObject & vUShort
        Assert.Equal(vObjectCatUShort.GetType(), GetType(String))
        Dim vObjectCatInteger = vObject & vInteger
        Assert.Equal(vObjectCatInteger.GetType(), GetType(String))
        Dim vObjectCatUInteger = vObject & vUInteger
        Assert.Equal(vObjectCatUInteger.GetType(), GetType(String))
        Dim vObjectCatLong = vObject & vLong
        Assert.Equal(vObjectCatLong.GetType(), GetType(String))
        Dim vObjectCatULong = vObject & vULong
        Assert.Equal(vObjectCatULong.GetType(), GetType(String))
        Dim vObjectCatDecimal = vObject & vDecimal
        Assert.Equal(vObjectCatDecimal.GetType(), GetType(String))
        Dim vObjectCatSingle = vObject & vSingle
        Assert.Equal(vObjectCatSingle.GetType(), GetType(String))
        Dim vObjectCatDouble = vObject & vDouble
        Assert.Equal(vObjectCatDouble.GetType(), GetType(String))
        Dim vObjectCatDate = vObject & vDate
        Assert.Equal(vObjectCatDate.GetType(), GetType(String))
        Dim vObjectCatChar = vObject & vChar
        Assert.Equal(vObjectCatChar.GetType(), GetType(String))
        Dim vObjectCatString = vObject & vString
        Assert.Equal(vObjectCatString.GetType(), GetType(String))
        Dim vObjectCatObject = vObject & vObject
        Assert.Equal(vObjectCatObject.GetType(), GetType(String))
    End Sub

    <Fact>
    Sub TestMul()
        Dim vBoolean As Boolean = Nothing
        Dim vSByte As SByte = Nothing
        Dim vByte As Byte = Nothing
        Dim vShort As Short = Nothing
        Dim vUShort As UShort = Nothing
        Dim vInteger As Integer = Nothing
        Dim vUInteger As UInteger = Nothing
        Dim vLong As Long = Nothing
        Dim vULong As ULong = Nothing
        Dim vDecimal As Decimal = Nothing
        Dim vSingle As Single = Nothing
        Dim vDouble As Double = Nothing
        Dim vString As String = Nothing

        Dim vBooleanMulBoolean = vBoolean * vBoolean
        Assert.Equal(vBooleanMulBoolean.GetType(), GetType(Short))
        Dim vBooleanMulSByte = vBoolean * vSByte
        Assert.Equal(vBooleanMulSByte.GetType(), GetType(SByte))
        Dim vBooleanMulByte = vBoolean * vByte
        Assert.Equal(vBooleanMulByte.GetType(), GetType(Short))
        Dim vBooleanMulShort = vBoolean * vShort
        Assert.Equal(vBooleanMulShort.GetType(), GetType(Short))
        Dim vBooleanMulUShort = vBoolean * vUShort
        Assert.Equal(vBooleanMulUShort.GetType(), GetType(Integer))
        Dim vBooleanMulInteger = vBoolean * vInteger
        Assert.Equal(vBooleanMulInteger.GetType(), GetType(Integer))
        Dim vBooleanMulUInteger = vBoolean * vUInteger
        Assert.Equal(vBooleanMulUInteger.GetType(), GetType(Long))
        Dim vBooleanMulLong = vBoolean * vLong
        Assert.Equal(vBooleanMulLong.GetType(), GetType(Long))
        Dim vBooleanMulULong = vBoolean * vULong
        Assert.Equal(vBooleanMulULong.GetType(), GetType(Decimal))
        Dim vBooleanMulDecimal = vBoolean * vDecimal
        Assert.Equal(vBooleanMulDecimal.GetType(), GetType(Decimal))
        Dim vBooleanMulSingle = vBoolean * vSingle
        Assert.Equal(vBooleanMulSingle.GetType(), GetType(Single))
        Dim vBooleanMulDouble = vBoolean * vDouble
        Assert.Equal(vBooleanMulDouble.GetType(), GetType(Double))
        Dim vBooleanMulString = vBoolean * vString
        Assert.Equal(vBooleanMulString.GetType(), GetType(Double))
        Dim vSByteMulBoolean = vSByte * vBoolean
        Assert.Equal(vSByteMulBoolean.GetType(), GetType(SByte))
        Dim vSByteMulSByte = vSByte * vSByte
        Assert.Equal(vSByteMulSByte.GetType(), GetType(SByte))
        Dim vSByteMulByte = vSByte * vByte
        Assert.Equal(vSByteMulByte.GetType(), GetType(Short))
        Dim vSByteMulShort = vSByte * vShort
        Assert.Equal(vSByteMulShort.GetType(), GetType(Short))
        Dim vSByteMulUShort = vSByte * vUShort
        Assert.Equal(vSByteMulUShort.GetType(), GetType(Integer))
        Dim vSByteMulInteger = vSByte * vInteger
        Assert.Equal(vSByteMulInteger.GetType(), GetType(Integer))
        Dim vSByteMulUInteger = vSByte * vUInteger
        Assert.Equal(vSByteMulUInteger.GetType(), GetType(Long))
        Dim vSByteMulLong = vSByte * vLong
        Assert.Equal(vSByteMulLong.GetType(), GetType(Long))
        Dim vSByteMulULong = vSByte * vULong
        Assert.Equal(vSByteMulULong.GetType(), GetType(Decimal))
        Dim vSByteMulDecimal = vSByte * vDecimal
        Assert.Equal(vSByteMulDecimal.GetType(), GetType(Decimal))
        Dim vSByteMulSingle = vSByte * vSingle
        Assert.Equal(vSByteMulSingle.GetType(), GetType(Single))
        Dim vSByteMulDouble = vSByte * vDouble
        Assert.Equal(vSByteMulDouble.GetType(), GetType(Double))
        Dim vSByteMulString = vSByte * vString
        Assert.Equal(vSByteMulString.GetType(), GetType(Double))
        Dim vByteMulBoolean = vByte * vBoolean
        Assert.Equal(vByteMulBoolean.GetType(), GetType(Short))
        Dim vByteMulSByte = vByte * vSByte
        Assert.Equal(vByteMulSByte.GetType(), GetType(Short))
        Dim vByteMulByte = vByte * vByte
        Assert.Equal(vByteMulByte.GetType(), GetType(Byte))
        Dim vByteMulShort = vByte * vShort
        Assert.Equal(vByteMulShort.GetType(), GetType(Short))
        Dim vByteMulUShort = vByte * vUShort
        Assert.Equal(vByteMulUShort.GetType(), GetType(UShort))
        Dim vByteMulInteger = vByte * vInteger
        Assert.Equal(vByteMulInteger.GetType(), GetType(Integer))
        Dim vByteMulUInteger = vByte * vUInteger
        Assert.Equal(vByteMulUInteger.GetType(), GetType(UInteger))
        Dim vByteMulLong = vByte * vLong
        Assert.Equal(vByteMulLong.GetType(), GetType(Long))
        Dim vByteMulULong = vByte * vULong
        Assert.Equal(vByteMulULong.GetType(), GetType(ULong))
        Dim vByteMulDecimal = vByte * vDecimal
        Assert.Equal(vByteMulDecimal.GetType(), GetType(Decimal))
        Dim vByteMulSingle = vByte * vSingle
        Assert.Equal(vByteMulSingle.GetType(), GetType(Single))
        Dim vByteMulDouble = vByte * vDouble
        Assert.Equal(vByteMulDouble.GetType(), GetType(Double))
        Dim vByteMulString = vByte * vString
        Assert.Equal(vByteMulString.GetType(), GetType(Double))
        Dim vShortMulBoolean = vShort * vBoolean
        Assert.Equal(vShortMulBoolean.GetType(), GetType(Short))
        Dim vShortMulSByte = vShort * vSByte
        Assert.Equal(vShortMulSByte.GetType(), GetType(Short))
        Dim vShortMulByte = vShort * vByte
        Assert.Equal(vShortMulByte.GetType(), GetType(Short))
        Dim vShortMulShort = vShort * vShort
        Assert.Equal(vShortMulShort.GetType(), GetType(Short))
        Dim vShortMulUShort = vShort * vUShort
        Assert.Equal(vShortMulUShort.GetType(), GetType(Integer))
        Dim vShortMulInteger = vShort * vInteger
        Assert.Equal(vShortMulInteger.GetType(), GetType(Integer))
        Dim vShortMulUInteger = vShort * vUInteger
        Assert.Equal(vShortMulUInteger.GetType(), GetType(Long))
        Dim vShortMulLong = vShort * vLong
        Assert.Equal(vShortMulLong.GetType(), GetType(Long))
        Dim vShortMulULong = vShort * vULong
        Assert.Equal(vShortMulULong.GetType(), GetType(Decimal))
        Dim vShortMulDecimal = vShort * vDecimal
        Assert.Equal(vShortMulDecimal.GetType(), GetType(Decimal))
        Dim vShortMulSingle = vShort * vSingle
        Assert.Equal(vShortMulSingle.GetType(), GetType(Single))
        Dim vShortMulDouble = vShort * vDouble
        Assert.Equal(vShortMulDouble.GetType(), GetType(Double))
        Dim vShortMulString = vShort * vString
        Assert.Equal(vShortMulString.GetType(), GetType(Double))
        Dim vUShortMulBoolean = vUShort * vBoolean
        Assert.Equal(vUShortMulBoolean.GetType(), GetType(Integer))
        Dim vUShortMulSByte = vUShort * vSByte
        Assert.Equal(vUShortMulSByte.GetType(), GetType(Integer))
        Dim vUShortMulByte = vUShort * vByte
        Assert.Equal(vUShortMulByte.GetType(), GetType(UShort))
        Dim vUShortMulShort = vUShort * vShort
        Assert.Equal(vUShortMulShort.GetType(), GetType(Integer))
        Dim vUShortMulUShort = vUShort * vUShort
        Assert.Equal(vUShortMulUShort.GetType(), GetType(UShort))
        Dim vUShortMulInteger = vUShort * vInteger
        Assert.Equal(vUShortMulInteger.GetType(), GetType(Integer))
        Dim vUShortMulUInteger = vUShort * vUInteger
        Assert.Equal(vUShortMulUInteger.GetType(), GetType(UInteger))
        Dim vUShortMulLong = vUShort * vLong
        Assert.Equal(vUShortMulLong.GetType(), GetType(Long))
        Dim vUShortMulULong = vUShort * vULong
        Assert.Equal(vUShortMulULong.GetType(), GetType(ULong))
        Dim vUShortMulDecimal = vUShort * vDecimal
        Assert.Equal(vUShortMulDecimal.GetType(), GetType(Decimal))
        Dim vUShortMulSingle = vUShort * vSingle
        Assert.Equal(vUShortMulSingle.GetType(), GetType(Single))
        Dim vUShortMulDouble = vUShort * vDouble
        Assert.Equal(vUShortMulDouble.GetType(), GetType(Double))
        Dim vUShortMulString = vUShort * vString
        Assert.Equal(vUShortMulString.GetType(), GetType(Double))
        Dim vIntegerMulBoolean = vInteger * vBoolean
        Assert.Equal(vIntegerMulBoolean.GetType(), GetType(Integer))
        Dim vIntegerMulSByte = vInteger * vSByte
        Assert.Equal(vIntegerMulSByte.GetType(), GetType(Integer))
        Dim vIntegerMulByte = vInteger * vByte
        Assert.Equal(vIntegerMulByte.GetType(), GetType(Integer))
        Dim vIntegerMulShort = vInteger * vShort
        Assert.Equal(vIntegerMulShort.GetType(), GetType(Integer))
        Dim vIntegerMulUShort = vInteger * vUShort
        Assert.Equal(vIntegerMulUShort.GetType(), GetType(Integer))
        Dim vIntegerMulInteger = vInteger * vInteger
        Assert.Equal(vIntegerMulInteger.GetType(), GetType(Integer))
        Dim vIntegerMulUInteger = vInteger * vUInteger
        Assert.Equal(vIntegerMulUInteger.GetType(), GetType(Long))
        Dim vIntegerMulLong = vInteger * vLong
        Assert.Equal(vIntegerMulLong.GetType(), GetType(Long))
        Dim vIntegerMulULong = vInteger * vULong
        Assert.Equal(vIntegerMulULong.GetType(), GetType(Decimal))
        Dim vIntegerMulDecimal = vInteger * vDecimal
        Assert.Equal(vIntegerMulDecimal.GetType(), GetType(Decimal))
        Dim vIntegerMulSingle = vInteger * vSingle
        Assert.Equal(vIntegerMulSingle.GetType(), GetType(Single))
        Dim vIntegerMulDouble = vInteger * vDouble
        Assert.Equal(vIntegerMulDouble.GetType(), GetType(Double))
        Dim vIntegerMulString = vInteger * vString
        Assert.Equal(vIntegerMulString.GetType(), GetType(Double))
        Dim vUIntegerMulBoolean = vUInteger * vBoolean
        Assert.Equal(vUIntegerMulBoolean.GetType(), GetType(Long))
        Dim vUIntegerMulSByte = vUInteger * vSByte
        Assert.Equal(vUIntegerMulSByte.GetType(), GetType(Long))
        Dim vUIntegerMulByte = vUInteger * vByte
        Assert.Equal(vUIntegerMulByte.GetType(), GetType(UInteger))
        Dim vUIntegerMulShort = vUInteger * vShort
        Assert.Equal(vUIntegerMulShort.GetType(), GetType(Long))
        Dim vUIntegerMulUShort = vUInteger * vUShort
        Assert.Equal(vUIntegerMulUShort.GetType(), GetType(UInteger))
        Dim vUIntegerMulInteger = vUInteger * vInteger
        Assert.Equal(vUIntegerMulInteger.GetType(), GetType(Long))
        Dim vUIntegerMulUInteger = vUInteger * vUInteger
        Assert.Equal(vUIntegerMulUInteger.GetType(), GetType(UInteger))
        Dim vUIntegerMulLong = vUInteger * vLong
        Assert.Equal(vUIntegerMulLong.GetType(), GetType(Long))
        Dim vUIntegerMulULong = vUInteger * vULong
        Assert.Equal(vUIntegerMulULong.GetType(), GetType(ULong))
        Dim vUIntegerMulDecimal = vUInteger * vDecimal
        Assert.Equal(vUIntegerMulDecimal.GetType(), GetType(Decimal))
        Dim vUIntegerMulSingle = vUInteger * vSingle
        Assert.Equal(vUIntegerMulSingle.GetType(), GetType(Single))
        Dim vUIntegerMulDouble = vUInteger * vDouble
        Assert.Equal(vUIntegerMulDouble.GetType(), GetType(Double))
        Dim vUIntegerMulString = vUInteger * vString
        Assert.Equal(vUIntegerMulString.GetType(), GetType(Double))
        Dim vLongMulBoolean = vLong * vBoolean
        Assert.Equal(vLongMulBoolean.GetType(), GetType(Long))
        Dim vLongMulSByte = vLong * vSByte
        Assert.Equal(vLongMulSByte.GetType(), GetType(Long))
        Dim vLongMulByte = vLong * vByte
        Assert.Equal(vLongMulByte.GetType(), GetType(Long))
        Dim vLongMulShort = vLong * vShort
        Assert.Equal(vLongMulShort.GetType(), GetType(Long))
        Dim vLongMulUShort = vLong * vUShort
        Assert.Equal(vLongMulUShort.GetType(), GetType(Long))
        Dim vLongMulInteger = vLong * vInteger
        Assert.Equal(vLongMulInteger.GetType(), GetType(Long))
        Dim vLongMulUInteger = vLong * vUInteger
        Assert.Equal(vLongMulUInteger.GetType(), GetType(Long))
        Dim vLongMulLong = vLong * vLong
        Assert.Equal(vLongMulLong.GetType(), GetType(Long))
        Dim vLongMulULong = vLong * vULong
        Assert.Equal(vLongMulULong.GetType(), GetType(Decimal))
        Dim vLongMulDecimal = vLong * vDecimal
        Assert.Equal(vLongMulDecimal.GetType(), GetType(Decimal))
        Dim vLongMulSingle = vLong * vSingle
        Assert.Equal(vLongMulSingle.GetType(), GetType(Single))
        Dim vLongMulDouble = vLong * vDouble
        Assert.Equal(vLongMulDouble.GetType(), GetType(Double))
        Dim vLongMulString = vLong * vString
        Assert.Equal(vLongMulString.GetType(), GetType(Double))
        Dim vULongMulBoolean = vULong * vBoolean
        Assert.Equal(vULongMulBoolean.GetType(), GetType(Decimal))
        Dim vULongMulSByte = vULong * vSByte
        Assert.Equal(vULongMulSByte.GetType(), GetType(Decimal))
        Dim vULongMulByte = vULong * vByte
        Assert.Equal(vULongMulByte.GetType(), GetType(ULong))
        Dim vULongMulShort = vULong * vShort
        Assert.Equal(vULongMulShort.GetType(), GetType(Decimal))
        Dim vULongMulUShort = vULong * vUShort
        Assert.Equal(vULongMulUShort.GetType(), GetType(ULong))
        Dim vULongMulInteger = vULong * vInteger
        Assert.Equal(vULongMulInteger.GetType(), GetType(Decimal))
        Dim vULongMulUInteger = vULong * vUInteger
        Assert.Equal(vULongMulUInteger.GetType(), GetType(ULong))
        Dim vULongMulLong = vULong * vLong
        Assert.Equal(vULongMulLong.GetType(), GetType(Decimal))
        Dim vULongMulULong = vULong * vULong
        Assert.Equal(vULongMulULong.GetType(), GetType(ULong))
        Dim vULongMulDecimal = vULong * vDecimal
        Assert.Equal(vULongMulDecimal.GetType(), GetType(Decimal))
        Dim vULongMulSingle = vULong * vSingle
        Assert.Equal(vULongMulSingle.GetType(), GetType(Single))
        Dim vULongMulDouble = vULong * vDouble
        Assert.Equal(vULongMulDouble.GetType(), GetType(Double))
        Dim vULongMulString = vULong * vString
        Assert.Equal(vULongMulString.GetType(), GetType(Double))
        Dim vDecimalMulBoolean = vDecimal * vBoolean
        Assert.Equal(vDecimalMulBoolean.GetType(), GetType(Decimal))
        Dim vDecimalMulSByte = vDecimal * vSByte
        Assert.Equal(vDecimalMulSByte.GetType(), GetType(Decimal))
        Dim vDecimalMulByte = vDecimal * vByte
        Assert.Equal(vDecimalMulByte.GetType(), GetType(Decimal))
        Dim vDecimalMulShort = vDecimal * vShort
        Assert.Equal(vDecimalMulShort.GetType(), GetType(Decimal))
        Dim vDecimalMulUShort = vDecimal * vUShort
        Assert.Equal(vDecimalMulUShort.GetType(), GetType(Decimal))
        Dim vDecimalMulInteger = vDecimal * vInteger
        Assert.Equal(vDecimalMulInteger.GetType(), GetType(Decimal))
        Dim vDecimalMulUInteger = vDecimal * vUInteger
        Assert.Equal(vDecimalMulUInteger.GetType(), GetType(Decimal))
        Dim vDecimalMulLong = vDecimal * vLong
        Assert.Equal(vDecimalMulLong.GetType(), GetType(Decimal))
        Dim vDecimalMulULong = vDecimal * vULong
        Assert.Equal(vDecimalMulULong.GetType(), GetType(Decimal))
        Dim vDecimalMulDecimal = vDecimal * vDecimal
        Assert.Equal(vDecimalMulDecimal.GetType(), GetType(Decimal))
        Dim vDecimalMulSingle = vDecimal * vSingle
        Assert.Equal(vDecimalMulSingle.GetType(), GetType(Single))
        Dim vDecimalMulDouble = vDecimal * vDouble
        Assert.Equal(vDecimalMulDouble.GetType(), GetType(Double))
        Dim vDecimalMulString = vDecimal * vString
        Assert.Equal(vDecimalMulString.GetType(), GetType(Double))
        Dim vSingleMulBoolean = vSingle * vBoolean
        Assert.Equal(vSingleMulBoolean.GetType(), GetType(Single))
        Dim vSingleMulSByte = vSingle * vSByte
        Assert.Equal(vSingleMulSByte.GetType(), GetType(Single))
        Dim vSingleMulByte = vSingle * vByte
        Assert.Equal(vSingleMulByte.GetType(), GetType(Single))
        Dim vSingleMulShort = vSingle * vShort
        Assert.Equal(vSingleMulShort.GetType(), GetType(Single))
        Dim vSingleMulUShort = vSingle * vUShort
        Assert.Equal(vSingleMulUShort.GetType(), GetType(Single))
        Dim vSingleMulInteger = vSingle * vInteger
        Assert.Equal(vSingleMulInteger.GetType(), GetType(Single))
        Dim vSingleMulUInteger = vSingle * vUInteger
        Assert.Equal(vSingleMulUInteger.GetType(), GetType(Single))
        Dim vSingleMulLong = vSingle * vLong
        Assert.Equal(vSingleMulLong.GetType(), GetType(Single))
        Dim vSingleMulULong = vSingle * vULong
        Assert.Equal(vSingleMulULong.GetType(), GetType(Single))
        Dim vSingleMulDecimal = vSingle * vDecimal
        Assert.Equal(vSingleMulDecimal.GetType(), GetType(Single))
        Dim vSingleMulSingle = vSingle * vSingle
        Assert.Equal(vSingleMulSingle.GetType(), GetType(Single))
        Dim vSingleMulDouble = vSingle * vDouble
        Assert.Equal(vSingleMulDouble.GetType(), GetType(Double))
        Dim vSingleMulString = vSingle * vString
        Assert.Equal(vSingleMulString.GetType(), GetType(Double))
        Dim vDoubleMulBoolean = vDouble * vBoolean
        Assert.Equal(vDoubleMulBoolean.GetType(), GetType(Double))
        Dim vDoubleMulSByte = vDouble * vSByte
        Assert.Equal(vDoubleMulSByte.GetType(), GetType(Double))
        Dim vDoubleMulByte = vDouble * vByte
        Assert.Equal(vDoubleMulByte.GetType(), GetType(Double))
        Dim vDoubleMulShort = vDouble * vShort
        Assert.Equal(vDoubleMulShort.GetType(), GetType(Double))
        Dim vDoubleMulUShort = vDouble * vUShort
        Assert.Equal(vDoubleMulUShort.GetType(), GetType(Double))
        Dim vDoubleMulInteger = vDouble * vInteger
        Assert.Equal(vDoubleMulInteger.GetType(), GetType(Double))
        Dim vDoubleMulUInteger = vDouble * vUInteger
        Assert.Equal(vDoubleMulUInteger.GetType(), GetType(Double))
        Dim vDoubleMulLong = vDouble * vLong
        Assert.Equal(vDoubleMulLong.GetType(), GetType(Double))
        Dim vDoubleMulULong = vDouble * vULong
        Assert.Equal(vDoubleMulULong.GetType(), GetType(Double))
        Dim vDoubleMulDecimal = vDouble * vDecimal
        Assert.Equal(vDoubleMulDecimal.GetType(), GetType(Double))
        Dim vDoubleMulSingle = vDouble * vSingle
        Assert.Equal(vDoubleMulSingle.GetType(), GetType(Double))
        Dim vDoubleMulDouble = vDouble * vDouble
        Assert.Equal(vDoubleMulDouble.GetType(), GetType(Double))
        Dim vDoubleMulString = vDouble * vString
        Assert.Equal(vDoubleMulString.GetType(), GetType(Double))
        Dim vStringMulBoolean = vString * vBoolean
        Assert.Equal(vStringMulBoolean.GetType(), GetType(Double))
        Dim vStringMulSByte = vString * vSByte
        Assert.Equal(vStringMulSByte.GetType(), GetType(Double))
        Dim vStringMulByte = vString * vByte
        Assert.Equal(vStringMulByte.GetType(), GetType(Double))
        Dim vStringMulShort = vString * vShort
        Assert.Equal(vStringMulShort.GetType(), GetType(Double))
        Dim vStringMulUShort = vString * vUShort
        Assert.Equal(vStringMulUShort.GetType(), GetType(Double))
        Dim vStringMulInteger = vString * vInteger
        Assert.Equal(vStringMulInteger.GetType(), GetType(Double))
        Dim vStringMulUInteger = vString * vUInteger
        Assert.Equal(vStringMulUInteger.GetType(), GetType(Double))
        Dim vStringMulLong = vString * vLong
        Assert.Equal(vStringMulLong.GetType(), GetType(Double))
        Dim vStringMulULong = vString * vULong
        Assert.Equal(vStringMulULong.GetType(), GetType(Double))
        Dim vStringMulDecimal = vString * vDecimal
        Assert.Equal(vStringMulDecimal.GetType(), GetType(Double))
        Dim vStringMulSingle = vString * vSingle
        Assert.Equal(vStringMulSingle.GetType(), GetType(Double))
        Dim vStringMulDouble = vString * vDouble
        Assert.Equal(vStringMulDouble.GetType(), GetType(Double))
        Dim vStringMulString = vString * vString
        Assert.Equal(vStringMulString.GetType(), GetType(Double))
    End Sub

    <Fact>
    Sub TestAdd()
        Dim vBoolean As Boolean = Nothing
        Dim vSByte As SByte = Nothing
        Dim vByte As Byte = Nothing
        Dim vShort As Short = Nothing
        Dim vUShort As UShort = Nothing
        Dim vInteger As Integer = Nothing
        Dim vUInteger As UInteger = Nothing
        Dim vLong As Long = Nothing
        Dim vULong As ULong = Nothing
        Dim vDecimal As Decimal = Nothing
        Dim vSingle As Single = Nothing
        Dim vDouble As Double = Nothing
        Dim vDate As Date = Nothing
        Dim vChar As Char = Nothing
        Dim vString As String = Nothing
        Dim vObject As Object = Nothing

        Dim vBooleanAddBoolean = vBoolean + vBoolean
        Assert.Equal(vBooleanAddBoolean.GetType(), GetType(Short))
        Dim vBooleanAddSByte = vBoolean + vSByte
        Assert.Equal(vBooleanAddSByte.GetType(), GetType(SByte))
        Dim vBooleanAddByte = vBoolean + vByte
        Assert.Equal(vBooleanAddByte.GetType(), GetType(Short))
        Dim vBooleanAddShort = vBoolean + vShort
        Assert.Equal(vBooleanAddShort.GetType(), GetType(Short))
        Dim vBooleanAddUShort = vBoolean + vUShort
        Assert.Equal(vBooleanAddUShort.GetType(), GetType(Integer))
        Dim vBooleanAddInteger = vBoolean + vInteger
        Assert.Equal(vBooleanAddInteger.GetType(), GetType(Integer))
        Dim vBooleanAddUInteger = vBoolean + vUInteger
        Assert.Equal(vBooleanAddUInteger.GetType(), GetType(Long))
        Dim vBooleanAddLong = vBoolean + vLong
        Assert.Equal(vBooleanAddLong.GetType(), GetType(Long))
        Dim vBooleanAddULong = vBoolean + vULong
        Assert.Equal(vBooleanAddULong.GetType(), GetType(Decimal))
        Dim vBooleanAddDecimal = vBoolean + vDecimal
        Assert.Equal(vBooleanAddDecimal.GetType(), GetType(Decimal))
        Dim vBooleanAddSingle = vBoolean + vSingle
        Assert.Equal(vBooleanAddSingle.GetType(), GetType(Single))
        Dim vBooleanAddDouble = vBoolean + vDouble
        Assert.Equal(vBooleanAddDouble.GetType(), GetType(Double))
        Dim vBooleanAddString = vBoolean + vString
        Assert.Equal(vBooleanAddString.GetType(), GetType(Double))
        Dim vSByteAddBoolean = vSByte + vBoolean
        Assert.Equal(vSByteAddBoolean.GetType(), GetType(SByte))
        Dim vSByteAddSByte = vSByte + vSByte
        Assert.Equal(vSByteAddSByte.GetType(), GetType(SByte))
        Dim vSByteAddByte = vSByte + vByte
        Assert.Equal(vSByteAddByte.GetType(), GetType(Short))
        Dim vSByteAddShort = vSByte + vShort
        Assert.Equal(vSByteAddShort.GetType(), GetType(Short))
        Dim vSByteAddUShort = vSByte + vUShort
        Assert.Equal(vSByteAddUShort.GetType(), GetType(Integer))
        Dim vSByteAddInteger = vSByte + vInteger
        Assert.Equal(vSByteAddInteger.GetType(), GetType(Integer))
        Dim vSByteAddUInteger = vSByte + vUInteger
        Assert.Equal(vSByteAddUInteger.GetType(), GetType(Long))
        Dim vSByteAddLong = vSByte + vLong
        Assert.Equal(vSByteAddLong.GetType(), GetType(Long))
        Dim vSByteAddULong = vSByte + vULong
        Assert.Equal(vSByteAddULong.GetType(), GetType(Decimal))
        Dim vSByteAddDecimal = vSByte + vDecimal
        Assert.Equal(vSByteAddDecimal.GetType(), GetType(Decimal))
        Dim vSByteAddSingle = vSByte + vSingle
        Assert.Equal(vSByteAddSingle.GetType(), GetType(Single))
        Dim vSByteAddDouble = vSByte + vDouble
        Assert.Equal(vSByteAddDouble.GetType(), GetType(Double))
        Dim vSByteAddString = vSByte + vString
        Assert.Equal(vSByteAddString.GetType(), GetType(Double))
        Dim vByteAddBoolean = vByte + vBoolean
        Assert.Equal(vByteAddBoolean.GetType(), GetType(Short))
        Dim vByteAddSByte = vByte + vSByte
        Assert.Equal(vByteAddSByte.GetType(), GetType(Short))
        Dim vByteAddByte = vByte + vByte
        Assert.Equal(vByteAddByte.GetType(), GetType(Byte))
        Dim vByteAddShort = vByte + vShort
        Assert.Equal(vByteAddShort.GetType(), GetType(Short))
        Dim vByteAddUShort = vByte + vUShort
        Assert.Equal(vByteAddUShort.GetType(), GetType(UShort))
        Dim vByteAddInteger = vByte + vInteger
        Assert.Equal(vByteAddInteger.GetType(), GetType(Integer))
        Dim vByteAddUInteger = vByte + vUInteger
        Assert.Equal(vByteAddUInteger.GetType(), GetType(UInteger))
        Dim vByteAddLong = vByte + vLong
        Assert.Equal(vByteAddLong.GetType(), GetType(Long))
        Dim vByteAddULong = vByte + vULong
        Assert.Equal(vByteAddULong.GetType(), GetType(ULong))
        Dim vByteAddDecimal = vByte + vDecimal
        Assert.Equal(vByteAddDecimal.GetType(), GetType(Decimal))
        Dim vByteAddSingle = vByte + vSingle
        Assert.Equal(vByteAddSingle.GetType(), GetType(Single))
        Dim vByteAddDouble = vByte + vDouble
        Assert.Equal(vByteAddDouble.GetType(), GetType(Double))
        Dim vByteAddString = vByte + vString
        Assert.Equal(vByteAddString.GetType(), GetType(Double))
        Dim vShortAddBoolean = vShort + vBoolean
        Assert.Equal(vShortAddBoolean.GetType(), GetType(Short))
        Dim vShortAddSByte = vShort + vSByte
        Assert.Equal(vShortAddSByte.GetType(), GetType(Short))
        Dim vShortAddByte = vShort + vByte
        Assert.Equal(vShortAddByte.GetType(), GetType(Short))
        Dim vShortAddShort = vShort + vShort
        Assert.Equal(vShortAddShort.GetType(), GetType(Short))
        Dim vShortAddUShort = vShort + vUShort
        Assert.Equal(vShortAddUShort.GetType(), GetType(Integer))
        Dim vShortAddInteger = vShort + vInteger
        Assert.Equal(vShortAddInteger.GetType(), GetType(Integer))
        Dim vShortAddUInteger = vShort + vUInteger
        Assert.Equal(vShortAddUInteger.GetType(), GetType(Long))
        Dim vShortAddLong = vShort + vLong
        Assert.Equal(vShortAddLong.GetType(), GetType(Long))
        Dim vShortAddULong = vShort + vULong
        Assert.Equal(vShortAddULong.GetType(), GetType(Decimal))
        Dim vShortAddDecimal = vShort + vDecimal
        Assert.Equal(vShortAddDecimal.GetType(), GetType(Decimal))
        Dim vShortAddSingle = vShort + vSingle
        Assert.Equal(vShortAddSingle.GetType(), GetType(Single))
        Dim vShortAddDouble = vShort + vDouble
        Assert.Equal(vShortAddDouble.GetType(), GetType(Double))
        Dim vShortAddString = vShort + vString
        Assert.Equal(vShortAddString.GetType(), GetType(Double))
        Dim vUShortAddBoolean = vUShort + vBoolean
        Assert.Equal(vUShortAddBoolean.GetType(), GetType(Integer))
        Dim vUShortAddSByte = vUShort + vSByte
        Assert.Equal(vUShortAddSByte.GetType(), GetType(Integer))
        Dim vUShortAddByte = vUShort + vByte
        Assert.Equal(vUShortAddByte.GetType(), GetType(UShort))
        Dim vUShortAddShort = vUShort + vShort
        Assert.Equal(vUShortAddShort.GetType(), GetType(Integer))
        Dim vUShortAddUShort = vUShort + vUShort
        Assert.Equal(vUShortAddUShort.GetType(), GetType(UShort))
        Dim vUShortAddInteger = vUShort + vInteger
        Assert.Equal(vUShortAddInteger.GetType(), GetType(Integer))
        Dim vUShortAddUInteger = vUShort + vUInteger
        Assert.Equal(vUShortAddUInteger.GetType(), GetType(UInteger))
        Dim vUShortAddLong = vUShort + vLong
        Assert.Equal(vUShortAddLong.GetType(), GetType(Long))
        Dim vUShortAddULong = vUShort + vULong
        Assert.Equal(vUShortAddULong.GetType(), GetType(ULong))
        Dim vUShortAddDecimal = vUShort + vDecimal
        Assert.Equal(vUShortAddDecimal.GetType(), GetType(Decimal))
        Dim vUShortAddSingle = vUShort + vSingle
        Assert.Equal(vUShortAddSingle.GetType(), GetType(Single))
        Dim vUShortAddDouble = vUShort + vDouble
        Assert.Equal(vUShortAddDouble.GetType(), GetType(Double))
        Dim vUShortAddString = vUShort + vString
        Assert.Equal(vUShortAddString.GetType(), GetType(Double))
        Dim vIntegerAddBoolean = vInteger + vBoolean
        Assert.Equal(vIntegerAddBoolean.GetType(), GetType(Integer))
        Dim vIntegerAddSByte = vInteger + vSByte
        Assert.Equal(vIntegerAddSByte.GetType(), GetType(Integer))
        Dim vIntegerAddByte = vInteger + vByte
        Assert.Equal(vIntegerAddByte.GetType(), GetType(Integer))
        Dim vIntegerAddShort = vInteger + vShort
        Assert.Equal(vIntegerAddShort.GetType(), GetType(Integer))
        Dim vIntegerAddUShort = vInteger + vUShort
        Assert.Equal(vIntegerAddUShort.GetType(), GetType(Integer))
        Dim vIntegerAddInteger = vInteger + vInteger
        Assert.Equal(vIntegerAddInteger.GetType(), GetType(Integer))
        Dim vIntegerAddUInteger = vInteger + vUInteger
        Assert.Equal(vIntegerAddUInteger.GetType(), GetType(Long))
        Dim vIntegerAddLong = vInteger + vLong
        Assert.Equal(vIntegerAddLong.GetType(), GetType(Long))
        Dim vIntegerAddULong = vInteger + vULong
        Assert.Equal(vIntegerAddULong.GetType(), GetType(Decimal))
        Dim vIntegerAddDecimal = vInteger + vDecimal
        Assert.Equal(vIntegerAddDecimal.GetType(), GetType(Decimal))
        Dim vIntegerAddSingle = vInteger + vSingle
        Assert.Equal(vIntegerAddSingle.GetType(), GetType(Single))
        Dim vIntegerAddDouble = vInteger + vDouble
        Assert.Equal(vIntegerAddDouble.GetType(), GetType(Double))
        Dim vIntegerAddString = vInteger + vString
        Assert.Equal(vIntegerAddString.GetType(), GetType(Double))
        Dim vUIntegerAddBoolean = vUInteger + vBoolean
        Assert.Equal(vUIntegerAddBoolean.GetType(), GetType(Long))
        Dim vUIntegerAddSByte = vUInteger + vSByte
        Assert.Equal(vUIntegerAddSByte.GetType(), GetType(Long))
        Dim vUIntegerAddByte = vUInteger + vByte
        Assert.Equal(vUIntegerAddByte.GetType(), GetType(UInteger))
        Dim vUIntegerAddShort = vUInteger + vShort
        Assert.Equal(vUIntegerAddShort.GetType(), GetType(Long))
        Dim vUIntegerAddUShort = vUInteger + vUShort
        Assert.Equal(vUIntegerAddUShort.GetType(), GetType(UInteger))
        Dim vUIntegerAddInteger = vUInteger + vInteger
        Assert.Equal(vUIntegerAddInteger.GetType(), GetType(Long))
        Dim vUIntegerAddUInteger = vUInteger + vUInteger
        Assert.Equal(vUIntegerAddUInteger.GetType(), GetType(UInteger))
        Dim vUIntegerAddLong = vUInteger + vLong
        Assert.Equal(vUIntegerAddLong.GetType(), GetType(Long))
        Dim vUIntegerAddULong = vUInteger + vULong
        Assert.Equal(vUIntegerAddULong.GetType(), GetType(ULong))
        Dim vUIntegerAddDecimal = vUInteger + vDecimal
        Assert.Equal(vUIntegerAddDecimal.GetType(), GetType(Decimal))
        Dim vUIntegerAddSingle = vUInteger + vSingle
        Assert.Equal(vUIntegerAddSingle.GetType(), GetType(Single))
        Dim vUIntegerAddDouble = vUInteger + vDouble
        Assert.Equal(vUIntegerAddDouble.GetType(), GetType(Double))
        Dim vUIntegerAddString = vUInteger + vString
        Assert.Equal(vUIntegerAddString.GetType(), GetType(Double))
        Dim vLongAddBoolean = vLong + vBoolean
        Assert.Equal(vLongAddBoolean.GetType(), GetType(Long))
        Dim vLongAddSByte = vLong + vSByte
        Assert.Equal(vLongAddSByte.GetType(), GetType(Long))
        Dim vLongAddByte = vLong + vByte
        Assert.Equal(vLongAddByte.GetType(), GetType(Long))
        Dim vLongAddShort = vLong + vShort
        Assert.Equal(vLongAddShort.GetType(), GetType(Long))
        Dim vLongAddUShort = vLong + vUShort
        Assert.Equal(vLongAddUShort.GetType(), GetType(Long))
        Dim vLongAddInteger = vLong + vInteger
        Assert.Equal(vLongAddInteger.GetType(), GetType(Long))
        Dim vLongAddUInteger = vLong + vUInteger
        Assert.Equal(vLongAddUInteger.GetType(), GetType(Long))
        Dim vLongAddLong = vLong + vLong
        Assert.Equal(vLongAddLong.GetType(), GetType(Long))
        Dim vLongAddULong = vLong + vULong
        Assert.Equal(vLongAddULong.GetType(), GetType(Decimal))
        Dim vLongAddDecimal = vLong + vDecimal
        Assert.Equal(vLongAddDecimal.GetType(), GetType(Decimal))
        Dim vLongAddSingle = vLong + vSingle
        Assert.Equal(vLongAddSingle.GetType(), GetType(Single))
        Dim vLongAddDouble = vLong + vDouble
        Assert.Equal(vLongAddDouble.GetType(), GetType(Double))
        Dim vLongAddString = vLong + vString
        Assert.Equal(vLongAddString.GetType(), GetType(Double))
        Dim vULongAddBoolean = vULong + vBoolean
        Assert.Equal(vULongAddBoolean.GetType(), GetType(Decimal))
        Dim vULongAddSByte = vULong + vSByte
        Assert.Equal(vULongAddSByte.GetType(), GetType(Decimal))
        Dim vULongAddByte = vULong + vByte
        Assert.Equal(vULongAddByte.GetType(), GetType(ULong))
        Dim vULongAddShort = vULong + vShort
        Assert.Equal(vULongAddShort.GetType(), GetType(Decimal))
        Dim vULongAddUShort = vULong + vUShort
        Assert.Equal(vULongAddUShort.GetType(), GetType(ULong))
        Dim vULongAddInteger = vULong + vInteger
        Assert.Equal(vULongAddInteger.GetType(), GetType(Decimal))
        Dim vULongAddUInteger = vULong + vUInteger
        Assert.Equal(vULongAddUInteger.GetType(), GetType(ULong))
        Dim vULongAddLong = vULong + vLong
        Assert.Equal(vULongAddLong.GetType(), GetType(Decimal))
        Dim vULongAddULong = vULong + vULong
        Assert.Equal(vULongAddULong.GetType(), GetType(ULong))
        Dim vULongAddDecimal = vULong + vDecimal
        Assert.Equal(vULongAddDecimal.GetType(), GetType(Decimal))
        Dim vULongAddSingle = vULong + vSingle
        Assert.Equal(vULongAddSingle.GetType(), GetType(Single))
        Dim vULongAddDouble = vULong + vDouble
        Assert.Equal(vULongAddDouble.GetType(), GetType(Double))
        Dim vULongAddString = vULong + vString
        Assert.Equal(vULongAddString.GetType(), GetType(Double))
        Dim vDecimalAddBoolean = vDecimal + vBoolean
        Assert.Equal(vDecimalAddBoolean.GetType(), GetType(Decimal))
        Dim vDecimalAddSByte = vDecimal + vSByte
        Assert.Equal(vDecimalAddSByte.GetType(), GetType(Decimal))
        Dim vDecimalAddByte = vDecimal + vByte
        Assert.Equal(vDecimalAddByte.GetType(), GetType(Decimal))
        Dim vDecimalAddShort = vDecimal + vShort
        Assert.Equal(vDecimalAddShort.GetType(), GetType(Decimal))
        Dim vDecimalAddUShort = vDecimal + vUShort
        Assert.Equal(vDecimalAddUShort.GetType(), GetType(Decimal))
        Dim vDecimalAddInteger = vDecimal + vInteger
        Assert.Equal(vDecimalAddInteger.GetType(), GetType(Decimal))
        Dim vDecimalAddUInteger = vDecimal + vUInteger
        Assert.Equal(vDecimalAddUInteger.GetType(), GetType(Decimal))
        Dim vDecimalAddLong = vDecimal + vLong
        Assert.Equal(vDecimalAddLong.GetType(), GetType(Decimal))
        Dim vDecimalAddULong = vDecimal + vULong
        Assert.Equal(vDecimalAddULong.GetType(), GetType(Decimal))
        Dim vDecimalAddDecimal = vDecimal + vDecimal
        Assert.Equal(vDecimalAddDecimal.GetType(), GetType(Decimal))
        Dim vDecimalAddSingle = vDecimal + vSingle
        Assert.Equal(vDecimalAddSingle.GetType(), GetType(Single))
        Dim vDecimalAddDouble = vDecimal + vDouble
        Assert.Equal(vDecimalAddDouble.GetType(), GetType(Double))
        Dim vDecimalAddString = vDecimal + vString
        Assert.Equal(vDecimalAddString.GetType(), GetType(Double))
        Dim vSingleAddBoolean = vSingle + vBoolean
        Assert.Equal(vSingleAddBoolean.GetType(), GetType(Single))
        Dim vSingleAddSByte = vSingle + vSByte
        Assert.Equal(vSingleAddSByte.GetType(), GetType(Single))
        Dim vSingleAddByte = vSingle + vByte
        Assert.Equal(vSingleAddByte.GetType(), GetType(Single))
        Dim vSingleAddShort = vSingle + vShort
        Assert.Equal(vSingleAddShort.GetType(), GetType(Single))
        Dim vSingleAddUShort = vSingle + vUShort
        Assert.Equal(vSingleAddUShort.GetType(), GetType(Single))
        Dim vSingleAddInteger = vSingle + vInteger
        Assert.Equal(vSingleAddInteger.GetType(), GetType(Single))
        Dim vSingleAddUInteger = vSingle + vUInteger
        Assert.Equal(vSingleAddUInteger.GetType(), GetType(Single))
        Dim vSingleAddLong = vSingle + vLong
        Assert.Equal(vSingleAddLong.GetType(), GetType(Single))
        Dim vSingleAddULong = vSingle + vULong
        Assert.Equal(vSingleAddULong.GetType(), GetType(Single))
        Dim vSingleAddDecimal = vSingle + vDecimal
        Assert.Equal(vSingleAddDecimal.GetType(), GetType(Single))
        Dim vSingleAddSingle = vSingle + vSingle
        Assert.Equal(vSingleAddSingle.GetType(), GetType(Single))
        Dim vSingleAddDouble = vSingle + vDouble
        Assert.Equal(vSingleAddDouble.GetType(), GetType(Double))
        Dim vSingleAddString = vSingle + vString
        Assert.Equal(vSingleAddString.GetType(), GetType(Double))
        Dim vDoubleAddBoolean = vDouble + vBoolean
        Assert.Equal(vDoubleAddBoolean.GetType(), GetType(Double))
        Dim vDoubleAddSByte = vDouble + vSByte
        Assert.Equal(vDoubleAddSByte.GetType(), GetType(Double))
        Dim vDoubleAddByte = vDouble + vByte
        Assert.Equal(vDoubleAddByte.GetType(), GetType(Double))
        Dim vDoubleAddShort = vDouble + vShort
        Assert.Equal(vDoubleAddShort.GetType(), GetType(Double))
        Dim vDoubleAddUShort = vDouble + vUShort
        Assert.Equal(vDoubleAddUShort.GetType(), GetType(Double))
        Dim vDoubleAddInteger = vDouble + vInteger
        Assert.Equal(vDoubleAddInteger.GetType(), GetType(Double))
        Dim vDoubleAddUInteger = vDouble + vUInteger
        Assert.Equal(vDoubleAddUInteger.GetType(), GetType(Double))
        Dim vDoubleAddLong = vDouble + vLong
        Assert.Equal(vDoubleAddLong.GetType(), GetType(Double))
        Dim vDoubleAddULong = vDouble + vULong
        Assert.Equal(vDoubleAddULong.GetType(), GetType(Double))
        Dim vDoubleAddDecimal = vDouble + vDecimal
        Assert.Equal(vDoubleAddDecimal.GetType(), GetType(Double))
        Dim vDoubleAddSingle = vDouble + vSingle
        Assert.Equal(vDoubleAddSingle.GetType(), GetType(Double))
        Dim vDoubleAddDouble = vDouble + vDouble
        Assert.Equal(vDoubleAddDouble.GetType(), GetType(Double))
        Dim vDoubleAddString = vDouble + vString
        Assert.Equal(vDoubleAddString.GetType(), GetType(Double))
        Dim vDateAddDate = vDate + vDate
        Assert.Equal(vDateAddDate.GetType(), GetType(String))
        Dim vDateAddString = vDate + vString
        Assert.Equal(vDateAddString.GetType(), GetType(String))
        Dim vCharAddChar = vChar + vChar
        Assert.Equal(vCharAddChar.GetType(), GetType(String))
        Dim vCharAddString = vChar + vString
        Assert.Equal(vCharAddString.GetType(), GetType(String))
        Dim vStringAddBoolean = vString + vBoolean
        Assert.Equal(vStringAddBoolean.GetType(), GetType(Double))
        Dim vStringAddSByte = vString + vSByte
        Assert.Equal(vStringAddSByte.GetType(), GetType(Double))
        Dim vStringAddByte = vString + vByte
        Assert.Equal(vStringAddByte.GetType(), GetType(Double))
        Dim vStringAddShort = vString + vShort
        Assert.Equal(vStringAddShort.GetType(), GetType(Double))
        Dim vStringAddUShort = vString + vUShort
        Assert.Equal(vStringAddUShort.GetType(), GetType(Double))
        Dim vStringAddInteger = vString + vInteger
        Assert.Equal(vStringAddInteger.GetType(), GetType(Double))
        Dim vStringAddUInteger = vString + vUInteger
        Assert.Equal(vStringAddUInteger.GetType(), GetType(Double))
        Dim vStringAddLong = vString + vLong
        Assert.Equal(vStringAddLong.GetType(), GetType(Double))
        Dim vStringAddULong = vString + vULong
        Assert.Equal(vStringAddULong.GetType(), GetType(Double))
        Dim vStringAddDecimal = vString + vDecimal
        Assert.Equal(vStringAddDecimal.GetType(), GetType(Double))
        Dim vStringAddSingle = vString + vSingle
        Assert.Equal(vStringAddSingle.GetType(), GetType(Double))
        Dim vStringAddDouble = vString + vDouble
        Assert.Equal(vStringAddDouble.GetType(), GetType(Double))
        Dim vStringAddDate = vString + vDate
        Assert.Equal(vStringAddDate.GetType(), GetType(String))
        Dim vStringAddChar = vString + vChar
        Assert.Equal(vStringAddChar.GetType(), GetType(String))
        Dim vStringAddString = vString + vString
        Assert.Equal(vStringAddString.GetType(), GetType(String))
    End Sub

    <Fact>
    Sub TestSub()
        Dim vBoolean As Boolean = Nothing
        Dim vSByte As SByte = Nothing
        Dim vByte As Byte = Nothing
        Dim vShort As Short = Nothing
        Dim vUShort As UShort = Nothing
        Dim vInteger As Integer = Nothing
        Dim vUInteger As UInteger = Nothing
        Dim vLong As Long = Nothing
        Dim vULong As ULong = Nothing
        Dim vDecimal As Decimal = Nothing
        Dim vSingle As Single = Nothing
        Dim vDouble As Double = Nothing
        Dim vDate As Date = Nothing
        Dim vString As String = Nothing

        Dim vBooleanSubBoolean = vBoolean - vBoolean
        Assert.Equal(vBooleanSubBoolean.GetType(), GetType(Short))
        Dim vBooleanSubSByte = vBoolean - vSByte
        Assert.Equal(vBooleanSubSByte.GetType(), GetType(SByte))
        Dim vBooleanSubByte = vBoolean - vByte
        Assert.Equal(vBooleanSubByte.GetType(), GetType(Short))
        Dim vBooleanSubShort = vBoolean - vShort
        Assert.Equal(vBooleanSubShort.GetType(), GetType(Short))
        Dim vBooleanSubUShort = vBoolean - vUShort
        Assert.Equal(vBooleanSubUShort.GetType(), GetType(Integer))
        Dim vBooleanSubInteger = vBoolean - vInteger
        Assert.Equal(vBooleanSubInteger.GetType(), GetType(Integer))
        Dim vBooleanSubUInteger = vBoolean - vUInteger
        Assert.Equal(vBooleanSubUInteger.GetType(), GetType(Long))
        Dim vBooleanSubLong = vBoolean - vLong
        Assert.Equal(vBooleanSubLong.GetType(), GetType(Long))
        Dim vBooleanSubULong = vBoolean - vULong
        Assert.Equal(vBooleanSubULong.GetType(), GetType(Decimal))
        Dim vBooleanSubDecimal = vBoolean - vDecimal
        Assert.Equal(vBooleanSubDecimal.GetType(), GetType(Decimal))
        Dim vBooleanSubSingle = vBoolean - vSingle
        Assert.Equal(vBooleanSubSingle.GetType(), GetType(Single))
        Dim vBooleanSubDouble = vBoolean - vDouble
        Assert.Equal(vBooleanSubDouble.GetType(), GetType(Double))
        Dim vBooleanSubString = vBoolean - vString
        Assert.Equal(vBooleanSubString.GetType(), GetType(Double))
        Dim vSByteSubBoolean = vSByte - vBoolean
        Assert.Equal(vSByteSubBoolean.GetType(), GetType(SByte))
        Dim vSByteSubSByte = vSByte - vSByte
        Assert.Equal(vSByteSubSByte.GetType(), GetType(SByte))
        Dim vSByteSubByte = vSByte - vByte
        Assert.Equal(vSByteSubByte.GetType(), GetType(Short))
        Dim vSByteSubShort = vSByte - vShort
        Assert.Equal(vSByteSubShort.GetType(), GetType(Short))
        Dim vSByteSubUShort = vSByte - vUShort
        Assert.Equal(vSByteSubUShort.GetType(), GetType(Integer))
        Dim vSByteSubInteger = vSByte - vInteger
        Assert.Equal(vSByteSubInteger.GetType(), GetType(Integer))
        Dim vSByteSubUInteger = vSByte - vUInteger
        Assert.Equal(vSByteSubUInteger.GetType(), GetType(Long))
        Dim vSByteSubLong = vSByte - vLong
        Assert.Equal(vSByteSubLong.GetType(), GetType(Long))
        Dim vSByteSubULong = vSByte - vULong
        Assert.Equal(vSByteSubULong.GetType(), GetType(Decimal))
        Dim vSByteSubDecimal = vSByte - vDecimal
        Assert.Equal(vSByteSubDecimal.GetType(), GetType(Decimal))
        Dim vSByteSubSingle = vSByte - vSingle
        Assert.Equal(vSByteSubSingle.GetType(), GetType(Single))
        Dim vSByteSubDouble = vSByte - vDouble
        Assert.Equal(vSByteSubDouble.GetType(), GetType(Double))
        Dim vSByteSubString = vSByte - vString
        Assert.Equal(vSByteSubString.GetType(), GetType(Double))
        Dim vByteSubBoolean = vByte - vBoolean
        Assert.Equal(vByteSubBoolean.GetType(), GetType(Short))
        Dim vByteSubSByte = vByte - vSByte
        Assert.Equal(vByteSubSByte.GetType(), GetType(Short))
        Dim vByteSubByte = vByte - vByte
        Assert.Equal(vByteSubByte.GetType(), GetType(Byte))
        Dim vByteSubShort = vByte - vShort
        Assert.Equal(vByteSubShort.GetType(), GetType(Short))
        Dim vByteSubUShort = vByte - vUShort
        Assert.Equal(vByteSubUShort.GetType(), GetType(UShort))
        Dim vByteSubInteger = vByte - vInteger
        Assert.Equal(vByteSubInteger.GetType(), GetType(Integer))
        Dim vByteSubUInteger = vByte - vUInteger
        Assert.Equal(vByteSubUInteger.GetType(), GetType(UInteger))
        Dim vByteSubLong = vByte - vLong
        Assert.Equal(vByteSubLong.GetType(), GetType(Long))
        Dim vByteSubULong = vByte - vULong
        Assert.Equal(vByteSubULong.GetType(), GetType(ULong))
        Dim vByteSubDecimal = vByte - vDecimal
        Assert.Equal(vByteSubDecimal.GetType(), GetType(Decimal))
        Dim vByteSubSingle = vByte - vSingle
        Assert.Equal(vByteSubSingle.GetType(), GetType(Single))
        Dim vByteSubDouble = vByte - vDouble
        Assert.Equal(vByteSubDouble.GetType(), GetType(Double))
        Dim vByteSubString = vByte - vString
        Assert.Equal(vByteSubString.GetType(), GetType(Double))
        Dim vShortSubBoolean = vShort - vBoolean
        Assert.Equal(vShortSubBoolean.GetType(), GetType(Short))
        Dim vShortSubSByte = vShort - vSByte
        Assert.Equal(vShortSubSByte.GetType(), GetType(Short))
        Dim vShortSubByte = vShort - vByte
        Assert.Equal(vShortSubByte.GetType(), GetType(Short))
        Dim vShortSubShort = vShort - vShort
        Assert.Equal(vShortSubShort.GetType(), GetType(Short))
        Dim vShortSubUShort = vShort - vUShort
        Assert.Equal(vShortSubUShort.GetType(), GetType(Integer))
        Dim vShortSubInteger = vShort - vInteger
        Assert.Equal(vShortSubInteger.GetType(), GetType(Integer))
        Dim vShortSubUInteger = vShort - vUInteger
        Assert.Equal(vShortSubUInteger.GetType(), GetType(Long))
        Dim vShortSubLong = vShort - vLong
        Assert.Equal(vShortSubLong.GetType(), GetType(Long))
        Dim vShortSubULong = vShort - vULong
        Assert.Equal(vShortSubULong.GetType(), GetType(Decimal))
        Dim vShortSubDecimal = vShort - vDecimal
        Assert.Equal(vShortSubDecimal.GetType(), GetType(Decimal))
        Dim vShortSubSingle = vShort - vSingle
        Assert.Equal(vShortSubSingle.GetType(), GetType(Single))
        Dim vShortSubDouble = vShort - vDouble
        Assert.Equal(vShortSubDouble.GetType(), GetType(Double))
        Dim vShortSubString = vShort - vString
        Assert.Equal(vShortSubString.GetType(), GetType(Double))
        Dim vUShortSubBoolean = vUShort - vBoolean
        Assert.Equal(vUShortSubBoolean.GetType(), GetType(Integer))
        Dim vUShortSubSByte = vUShort - vSByte
        Assert.Equal(vUShortSubSByte.GetType(), GetType(Integer))
        Dim vUShortSubByte = vUShort - vByte
        Assert.Equal(vUShortSubByte.GetType(), GetType(UShort))
        Dim vUShortSubShort = vUShort - vShort
        Assert.Equal(vUShortSubShort.GetType(), GetType(Integer))
        Dim vUShortSubUShort = vUShort - vUShort
        Assert.Equal(vUShortSubUShort.GetType(), GetType(UShort))
        Dim vUShortSubInteger = vUShort - vInteger
        Assert.Equal(vUShortSubInteger.GetType(), GetType(Integer))
        Dim vUShortSubUInteger = vUShort - vUInteger
        Assert.Equal(vUShortSubUInteger.GetType(), GetType(UInteger))
        Dim vUShortSubLong = vUShort - vLong
        Assert.Equal(vUShortSubLong.GetType(), GetType(Long))
        Dim vUShortSubULong = vUShort - vULong
        Assert.Equal(vUShortSubULong.GetType(), GetType(ULong))
        Dim vUShortSubDecimal = vUShort - vDecimal
        Assert.Equal(vUShortSubDecimal.GetType(), GetType(Decimal))
        Dim vUShortSubSingle = vUShort - vSingle
        Assert.Equal(vUShortSubSingle.GetType(), GetType(Single))
        Dim vUShortSubDouble = vUShort - vDouble
        Assert.Equal(vUShortSubDouble.GetType(), GetType(Double))
        Dim vUShortSubString = vUShort - vString
        Assert.Equal(vUShortSubString.GetType(), GetType(Double))
        Dim vIntegerSubBoolean = vInteger - vBoolean
        Assert.Equal(vIntegerSubBoolean.GetType(), GetType(Integer))
        Dim vIntegerSubSByte = vInteger - vSByte
        Assert.Equal(vIntegerSubSByte.GetType(), GetType(Integer))
        Dim vIntegerSubByte = vInteger - vByte
        Assert.Equal(vIntegerSubByte.GetType(), GetType(Integer))
        Dim vIntegerSubShort = vInteger - vShort
        Assert.Equal(vIntegerSubShort.GetType(), GetType(Integer))
        Dim vIntegerSubUShort = vInteger - vUShort
        Assert.Equal(vIntegerSubUShort.GetType(), GetType(Integer))
        Dim vIntegerSubInteger = vInteger - vInteger
        Assert.Equal(vIntegerSubInteger.GetType(), GetType(Integer))
        Dim vIntegerSubUInteger = vInteger - vUInteger
        Assert.Equal(vIntegerSubUInteger.GetType(), GetType(Long))
        Dim vIntegerSubLong = vInteger - vLong
        Assert.Equal(vIntegerSubLong.GetType(), GetType(Long))
        Dim vIntegerSubULong = vInteger - vULong
        Assert.Equal(vIntegerSubULong.GetType(), GetType(Decimal))
        Dim vIntegerSubDecimal = vInteger - vDecimal
        Assert.Equal(vIntegerSubDecimal.GetType(), GetType(Decimal))
        Dim vIntegerSubSingle = vInteger - vSingle
        Assert.Equal(vIntegerSubSingle.GetType(), GetType(Single))
        Dim vIntegerSubDouble = vInteger - vDouble
        Assert.Equal(vIntegerSubDouble.GetType(), GetType(Double))
        Dim vIntegerSubString = vInteger - vString
        Assert.Equal(vIntegerSubString.GetType(), GetType(Double))
        Dim vUIntegerSubBoolean = vUInteger - vBoolean
        Assert.Equal(vUIntegerSubBoolean.GetType(), GetType(Long))
        Dim vUIntegerSubSByte = vUInteger - vSByte
        Assert.Equal(vUIntegerSubSByte.GetType(), GetType(Long))
        Dim vUIntegerSubByte = vUInteger - vByte
        Assert.Equal(vUIntegerSubByte.GetType(), GetType(UInteger))
        Dim vUIntegerSubShort = vUInteger - vShort
        Assert.Equal(vUIntegerSubShort.GetType(), GetType(Long))
        Dim vUIntegerSubUShort = vUInteger - vUShort
        Assert.Equal(vUIntegerSubUShort.GetType(), GetType(UInteger))
        Dim vUIntegerSubInteger = vUInteger - vInteger
        Assert.Equal(vUIntegerSubInteger.GetType(), GetType(Long))
        Dim vUIntegerSubUInteger = vUInteger - vUInteger
        Assert.Equal(vUIntegerSubUInteger.GetType(), GetType(UInteger))
        Dim vUIntegerSubLong = vUInteger - vLong
        Assert.Equal(vUIntegerSubLong.GetType(), GetType(Long))
        Dim vUIntegerSubULong = vUInteger - vULong
        Assert.Equal(vUIntegerSubULong.GetType(), GetType(ULong))
        Dim vUIntegerSubDecimal = vUInteger - vDecimal
        Assert.Equal(vUIntegerSubDecimal.GetType(), GetType(Decimal))
        Dim vUIntegerSubSingle = vUInteger - vSingle
        Assert.Equal(vUIntegerSubSingle.GetType(), GetType(Single))
        Dim vUIntegerSubDouble = vUInteger - vDouble
        Assert.Equal(vUIntegerSubDouble.GetType(), GetType(Double))
        Dim vUIntegerSubString = vUInteger - vString
        Assert.Equal(vUIntegerSubString.GetType(), GetType(Double))
        Dim vLongSubBoolean = vLong - vBoolean
        Assert.Equal(vLongSubBoolean.GetType(), GetType(Long))
        Dim vLongSubSByte = vLong - vSByte
        Assert.Equal(vLongSubSByte.GetType(), GetType(Long))
        Dim vLongSubByte = vLong - vByte
        Assert.Equal(vLongSubByte.GetType(), GetType(Long))
        Dim vLongSubShort = vLong - vShort
        Assert.Equal(vLongSubShort.GetType(), GetType(Long))
        Dim vLongSubUShort = vLong - vUShort
        Assert.Equal(vLongSubUShort.GetType(), GetType(Long))
        Dim vLongSubInteger = vLong - vInteger
        Assert.Equal(vLongSubInteger.GetType(), GetType(Long))
        Dim vLongSubUInteger = vLong - vUInteger
        Assert.Equal(vLongSubUInteger.GetType(), GetType(Long))
        Dim vLongSubLong = vLong - vLong
        Assert.Equal(vLongSubLong.GetType(), GetType(Long))
        Dim vLongSubULong = vLong - vULong
        Assert.Equal(vLongSubULong.GetType(), GetType(Decimal))
        Dim vLongSubDecimal = vLong - vDecimal
        Assert.Equal(vLongSubDecimal.GetType(), GetType(Decimal))
        Dim vLongSubSingle = vLong - vSingle
        Assert.Equal(vLongSubSingle.GetType(), GetType(Single))
        Dim vLongSubDouble = vLong - vDouble
        Assert.Equal(vLongSubDouble.GetType(), GetType(Double))
        Dim vLongSubString = vLong - vString
        Assert.Equal(vLongSubString.GetType(), GetType(Double))
        Dim vULongSubBoolean = vULong - vBoolean
        Assert.Equal(vULongSubBoolean.GetType(), GetType(Decimal))
        Dim vULongSubSByte = vULong - vSByte
        Assert.Equal(vULongSubSByte.GetType(), GetType(Decimal))
        Dim vULongSubByte = vULong - vByte
        Assert.Equal(vULongSubByte.GetType(), GetType(ULong))
        Dim vULongSubShort = vULong - vShort
        Assert.Equal(vULongSubShort.GetType(), GetType(Decimal))
        Dim vULongSubUShort = vULong - vUShort
        Assert.Equal(vULongSubUShort.GetType(), GetType(ULong))
        Dim vULongSubInteger = vULong - vInteger
        Assert.Equal(vULongSubInteger.GetType(), GetType(Decimal))
        Dim vULongSubUInteger = vULong - vUInteger
        Assert.Equal(vULongSubUInteger.GetType(), GetType(ULong))
        Dim vULongSubLong = vULong - vLong
        Assert.Equal(vULongSubLong.GetType(), GetType(Decimal))
        Dim vULongSubULong = vULong - vULong
        Assert.Equal(vULongSubULong.GetType(), GetType(ULong))
        Dim vULongSubDecimal = vULong - vDecimal
        Assert.Equal(vULongSubDecimal.GetType(), GetType(Decimal))
        Dim vULongSubSingle = vULong - vSingle
        Assert.Equal(vULongSubSingle.GetType(), GetType(Single))
        Dim vULongSubDouble = vULong - vDouble
        Assert.Equal(vULongSubDouble.GetType(), GetType(Double))
        Dim vULongSubString = vULong - vString
        Assert.Equal(vULongSubString.GetType(), GetType(Double))
        Dim vDecimalSubBoolean = vDecimal - vBoolean
        Assert.Equal(vDecimalSubBoolean.GetType(), GetType(Decimal))
        Dim vDecimalSubSByte = vDecimal - vSByte
        Assert.Equal(vDecimalSubSByte.GetType(), GetType(Decimal))
        Dim vDecimalSubByte = vDecimal - vByte
        Assert.Equal(vDecimalSubByte.GetType(), GetType(Decimal))
        Dim vDecimalSubShort = vDecimal - vShort
        Assert.Equal(vDecimalSubShort.GetType(), GetType(Decimal))
        Dim vDecimalSubUShort = vDecimal - vUShort
        Assert.Equal(vDecimalSubUShort.GetType(), GetType(Decimal))
        Dim vDecimalSubInteger = vDecimal - vInteger
        Assert.Equal(vDecimalSubInteger.GetType(), GetType(Decimal))
        Dim vDecimalSubUInteger = vDecimal - vUInteger
        Assert.Equal(vDecimalSubUInteger.GetType(), GetType(Decimal))
        Dim vDecimalSubLong = vDecimal - vLong
        Assert.Equal(vDecimalSubLong.GetType(), GetType(Decimal))
        Dim vDecimalSubULong = vDecimal - vULong
        Assert.Equal(vDecimalSubULong.GetType(), GetType(Decimal))
        Dim vDecimalSubDecimal = vDecimal - vDecimal
        Assert.Equal(vDecimalSubDecimal.GetType(), GetType(Decimal))
        Dim vDecimalSubSingle = vDecimal - vSingle
        Assert.Equal(vDecimalSubSingle.GetType(), GetType(Single))
        Dim vDecimalSubDouble = vDecimal - vDouble
        Assert.Equal(vDecimalSubDouble.GetType(), GetType(Double))
        Dim vDecimalSubString = vDecimal - vString
        Assert.Equal(vDecimalSubString.GetType(), GetType(Double))
        Dim vSingleSubBoolean = vSingle - vBoolean
        Assert.Equal(vSingleSubBoolean.GetType(), GetType(Single))
        Dim vSingleSubSByte = vSingle - vSByte
        Assert.Equal(vSingleSubSByte.GetType(), GetType(Single))
        Dim vSingleSubByte = vSingle - vByte
        Assert.Equal(vSingleSubByte.GetType(), GetType(Single))
        Dim vSingleSubShort = vSingle - vShort
        Assert.Equal(vSingleSubShort.GetType(), GetType(Single))
        Dim vSingleSubUShort = vSingle - vUShort
        Assert.Equal(vSingleSubUShort.GetType(), GetType(Single))
        Dim vSingleSubInteger = vSingle - vInteger
        Assert.Equal(vSingleSubInteger.GetType(), GetType(Single))
        Dim vSingleSubUInteger = vSingle - vUInteger
        Assert.Equal(vSingleSubUInteger.GetType(), GetType(Single))
        Dim vSingleSubLong = vSingle - vLong
        Assert.Equal(vSingleSubLong.GetType(), GetType(Single))
        Dim vSingleSubULong = vSingle - vULong
        Assert.Equal(vSingleSubULong.GetType(), GetType(Single))
        Dim vSingleSubDecimal = vSingle - vDecimal
        Assert.Equal(vSingleSubDecimal.GetType(), GetType(Single))
        Dim vSingleSubSingle = vSingle - vSingle
        Assert.Equal(vSingleSubSingle.GetType(), GetType(Single))
        Dim vSingleSubDouble = vSingle - vDouble
        Assert.Equal(vSingleSubDouble.GetType(), GetType(Double))
        Dim vSingleSubString = vSingle - vString
        Assert.Equal(vSingleSubString.GetType(), GetType(Double))
        Dim vDoubleSubBoolean = vDouble - vBoolean
        Assert.Equal(vDoubleSubBoolean.GetType(), GetType(Double))
        Dim vDoubleSubSByte = vDouble - vSByte
        Assert.Equal(vDoubleSubSByte.GetType(), GetType(Double))
        Dim vDoubleSubByte = vDouble - vByte
        Assert.Equal(vDoubleSubByte.GetType(), GetType(Double))
        Dim vDoubleSubShort = vDouble - vShort
        Assert.Equal(vDoubleSubShort.GetType(), GetType(Double))
        Dim vDoubleSubUShort = vDouble - vUShort
        Assert.Equal(vDoubleSubUShort.GetType(), GetType(Double))
        Dim vDoubleSubInteger = vDouble - vInteger
        Assert.Equal(vDoubleSubInteger.GetType(), GetType(Double))
        Dim vDoubleSubUInteger = vDouble - vUInteger
        Assert.Equal(vDoubleSubUInteger.GetType(), GetType(Double))
        Dim vDoubleSubLong = vDouble - vLong
        Assert.Equal(vDoubleSubLong.GetType(), GetType(Double))
        Dim vDoubleSubULong = vDouble - vULong
        Assert.Equal(vDoubleSubULong.GetType(), GetType(Double))
        Dim vDoubleSubDecimal = vDouble - vDecimal
        Assert.Equal(vDoubleSubDecimal.GetType(), GetType(Double))
        Dim vDoubleSubSingle = vDouble - vSingle
        Assert.Equal(vDoubleSubSingle.GetType(), GetType(Double))
        Dim vDoubleSubDouble = vDouble - vDouble
        Assert.Equal(vDoubleSubDouble.GetType(), GetType(Double))
        Dim vDoubleSubString = vDouble - vString
        Assert.Equal(vDoubleSubString.GetType(), GetType(Double))
        Dim vDateSubDate = vDate - vDate
        Assert.Equal(vDateSubDate.GetType(), GetType(TimeSpan))
        Dim vStringSubBoolean = vString - vBoolean
        Assert.Equal(vStringSubBoolean.GetType(), GetType(Double))
        Dim vStringSubSByte = vString - vSByte
        Assert.Equal(vStringSubSByte.GetType(), GetType(Double))
        Dim vStringSubByte = vString - vByte
        Assert.Equal(vStringSubByte.GetType(), GetType(Double))
        Dim vStringSubShort = vString - vShort
        Assert.Equal(vStringSubShort.GetType(), GetType(Double))
        Dim vStringSubUShort = vString - vUShort
        Assert.Equal(vStringSubUShort.GetType(), GetType(Double))
        Dim vStringSubInteger = vString - vInteger
        Assert.Equal(vStringSubInteger.GetType(), GetType(Double))
        Dim vStringSubUInteger = vString - vUInteger
        Assert.Equal(vStringSubUInteger.GetType(), GetType(Double))
        Dim vStringSubLong = vString - vLong
        Assert.Equal(vStringSubLong.GetType(), GetType(Double))
        Dim vStringSubULong = vString - vULong
        Assert.Equal(vStringSubULong.GetType(), GetType(Double))
        Dim vStringSubDecimal = vString - vDecimal
        Assert.Equal(vStringSubDecimal.GetType(), GetType(Double))
        Dim vStringSubSingle = vString - vSingle
        Assert.Equal(vStringSubSingle.GetType(), GetType(Double))
        Dim vStringSubDouble = vString - vDouble
        Assert.Equal(vStringSubDouble.GetType(), GetType(Double))
        Dim vStringSubString = vString - vString
        Assert.Equal(vStringSubString.GetType(), GetType(Double))
    End Sub

    <Fact>
    Sub TestDiv()
        Dim vBoolean As Boolean = True
        Dim vSByte As SByte = 1
        Dim vByte As Byte = 1
        Dim vShort As Short = 1
        Dim vUShort As UShort = 1
        Dim vInteger As Integer = 1
        Dim vUInteger As UInteger = 1
        Dim vLong As Long = 1
        Dim vULong As ULong = 1
        Dim vDecimal As Decimal = 1
        Dim vSingle As Single = 1
        Dim vDouble As Double = 1
        Dim vString As String = Nothing

        Dim vBooleanDivBoolean = vBoolean / vBoolean
        Assert.Equal(vBooleanDivBoolean.GetType(), GetType(Double))
        Dim vBooleanDivSByte = vBoolean / vSByte
        Assert.Equal(vBooleanDivSByte.GetType(), GetType(Double))
        Dim vBooleanDivByte = vBoolean / vByte
        Assert.Equal(vBooleanDivByte.GetType(), GetType(Double))
        Dim vBooleanDivShort = vBoolean / vShort
        Assert.Equal(vBooleanDivShort.GetType(), GetType(Double))
        Dim vBooleanDivUShort = vBoolean / vUShort
        Assert.Equal(vBooleanDivUShort.GetType(), GetType(Double))
        Dim vBooleanDivInteger = vBoolean / vInteger
        Assert.Equal(vBooleanDivInteger.GetType(), GetType(Double))
        Dim vBooleanDivUInteger = vBoolean / vUInteger
        Assert.Equal(vBooleanDivUInteger.GetType(), GetType(Double))
        Dim vBooleanDivLong = vBoolean / vLong
        Assert.Equal(vBooleanDivLong.GetType(), GetType(Double))
        Dim vBooleanDivULong = vBoolean / vULong
        Assert.Equal(vBooleanDivULong.GetType(), GetType(Double))
        Dim vBooleanDivDecimal = vBoolean / vDecimal
        Assert.Equal(vBooleanDivDecimal.GetType(), GetType(Decimal))
        Dim vBooleanDivSingle = vBoolean / vSingle
        Assert.Equal(vBooleanDivSingle.GetType(), GetType(Single))
        Dim vBooleanDivDouble = vBoolean / vDouble
        Assert.Equal(vBooleanDivDouble.GetType(), GetType(Double))
        Dim vBooleanDivString = vBoolean / vString
        Assert.Equal(vBooleanDivString.GetType(), GetType(Double))
        Dim vSByteDivBoolean = vSByte / vBoolean
        Assert.Equal(vSByteDivBoolean.GetType(), GetType(Double))
        Dim vSByteDivSByte = vSByte / vSByte
        Assert.Equal(vSByteDivSByte.GetType(), GetType(Double))
        Dim vSByteDivByte = vSByte / vByte
        Assert.Equal(vSByteDivByte.GetType(), GetType(Double))
        Dim vSByteDivShort = vSByte / vShort
        Assert.Equal(vSByteDivShort.GetType(), GetType(Double))
        Dim vSByteDivUShort = vSByte / vUShort
        Assert.Equal(vSByteDivUShort.GetType(), GetType(Double))
        Dim vSByteDivInteger = vSByte / vInteger
        Assert.Equal(vSByteDivInteger.GetType(), GetType(Double))
        Dim vSByteDivUInteger = vSByte / vUInteger
        Assert.Equal(vSByteDivUInteger.GetType(), GetType(Double))
        Dim vSByteDivLong = vSByte / vLong
        Assert.Equal(vSByteDivLong.GetType(), GetType(Double))
        Dim vSByteDivULong = vSByte / vULong
        Assert.Equal(vSByteDivULong.GetType(), GetType(Double))
        Dim vSByteDivDecimal = vSByte / vDecimal
        Assert.Equal(vSByteDivDecimal.GetType(), GetType(Decimal))
        Dim vSByteDivSingle = vSByte / vSingle
        Assert.Equal(vSByteDivSingle.GetType(), GetType(Single))
        Dim vSByteDivDouble = vSByte / vDouble
        Assert.Equal(vSByteDivDouble.GetType(), GetType(Double))
        Dim vSByteDivString = vSByte / vString
        Assert.Equal(vSByteDivString.GetType(), GetType(Double))
        Dim vByteDivBoolean = vByte / vBoolean
        Assert.Equal(vByteDivBoolean.GetType(), GetType(Double))
        Dim vByteDivSByte = vByte / vSByte
        Assert.Equal(vByteDivSByte.GetType(), GetType(Double))
        Dim vByteDivByte = vByte / vByte
        Assert.Equal(vByteDivByte.GetType(), GetType(Double))
        Dim vByteDivShort = vByte / vShort
        Assert.Equal(vByteDivShort.GetType(), GetType(Double))
        Dim vByteDivUShort = vByte / vUShort
        Assert.Equal(vByteDivUShort.GetType(), GetType(Double))
        Dim vByteDivInteger = vByte / vInteger
        Assert.Equal(vByteDivInteger.GetType(), GetType(Double))
        Dim vByteDivUInteger = vByte / vUInteger
        Assert.Equal(vByteDivUInteger.GetType(), GetType(Double))
        Dim vByteDivLong = vByte / vLong
        Assert.Equal(vByteDivLong.GetType(), GetType(Double))
        Dim vByteDivULong = vByte / vULong
        Assert.Equal(vByteDivULong.GetType(), GetType(Double))
        Dim vByteDivDecimal = vByte / vDecimal
        Assert.Equal(vByteDivDecimal.GetType(), GetType(Decimal))
        Dim vByteDivSingle = vByte / vSingle
        Assert.Equal(vByteDivSingle.GetType(), GetType(Single))
        Dim vByteDivDouble = vByte / vDouble
        Assert.Equal(vByteDivDouble.GetType(), GetType(Double))
        Dim vByteDivString = vByte / vString
        Assert.Equal(vByteDivString.GetType(), GetType(Double))
        Dim vShortDivBoolean = vShort / vBoolean
        Assert.Equal(vShortDivBoolean.GetType(), GetType(Double))
        Dim vShortDivSByte = vShort / vSByte
        Assert.Equal(vShortDivSByte.GetType(), GetType(Double))
        Dim vShortDivByte = vShort / vByte
        Assert.Equal(vShortDivByte.GetType(), GetType(Double))
        Dim vShortDivShort = vShort / vShort
        Assert.Equal(vShortDivShort.GetType(), GetType(Double))
        Dim vShortDivUShort = vShort / vUShort
        Assert.Equal(vShortDivUShort.GetType(), GetType(Double))
        Dim vShortDivInteger = vShort / vInteger
        Assert.Equal(vShortDivInteger.GetType(), GetType(Double))
        Dim vShortDivUInteger = vShort / vUInteger
        Assert.Equal(vShortDivUInteger.GetType(), GetType(Double))
        Dim vShortDivLong = vShort / vLong
        Assert.Equal(vShortDivLong.GetType(), GetType(Double))
        Dim vShortDivULong = vShort / vULong
        Assert.Equal(vShortDivULong.GetType(), GetType(Double))
        Dim vShortDivDecimal = vShort / vDecimal
        Assert.Equal(vShortDivDecimal.GetType(), GetType(Decimal))
        Dim vShortDivSingle = vShort / vSingle
        Assert.Equal(vShortDivSingle.GetType(), GetType(Single))
        Dim vShortDivDouble = vShort / vDouble
        Assert.Equal(vShortDivDouble.GetType(), GetType(Double))
        Dim vShortDivString = vShort / vString
        Assert.Equal(vShortDivString.GetType(), GetType(Double))
        Dim vUShortDivBoolean = vUShort / vBoolean
        Assert.Equal(vUShortDivBoolean.GetType(), GetType(Double))
        Dim vUShortDivSByte = vUShort / vSByte
        Assert.Equal(vUShortDivSByte.GetType(), GetType(Double))
        Dim vUShortDivByte = vUShort / vByte
        Assert.Equal(vUShortDivByte.GetType(), GetType(Double))
        Dim vUShortDivShort = vUShort / vShort
        Assert.Equal(vUShortDivShort.GetType(), GetType(Double))
        Dim vUShortDivUShort = vUShort / vUShort
        Assert.Equal(vUShortDivUShort.GetType(), GetType(Double))
        Dim vUShortDivInteger = vUShort / vInteger
        Assert.Equal(vUShortDivInteger.GetType(), GetType(Double))
        Dim vUShortDivUInteger = vUShort / vUInteger
        Assert.Equal(vUShortDivUInteger.GetType(), GetType(Double))
        Dim vUShortDivLong = vUShort / vLong
        Assert.Equal(vUShortDivLong.GetType(), GetType(Double))
        Dim vUShortDivULong = vUShort / vULong
        Assert.Equal(vUShortDivULong.GetType(), GetType(Double))
        Dim vUShortDivDecimal = vUShort / vDecimal
        Assert.Equal(vUShortDivDecimal.GetType(), GetType(Decimal))
        Dim vUShortDivSingle = vUShort / vSingle
        Assert.Equal(vUShortDivSingle.GetType(), GetType(Single))
        Dim vUShortDivDouble = vUShort / vDouble
        Assert.Equal(vUShortDivDouble.GetType(), GetType(Double))
        Dim vUShortDivString = vUShort / vString
        Assert.Equal(vUShortDivString.GetType(), GetType(Double))
        Dim vIntegerDivBoolean = vInteger / vBoolean
        Assert.Equal(vIntegerDivBoolean.GetType(), GetType(Double))
        Dim vIntegerDivSByte = vInteger / vSByte
        Assert.Equal(vIntegerDivSByte.GetType(), GetType(Double))
        Dim vIntegerDivByte = vInteger / vByte
        Assert.Equal(vIntegerDivByte.GetType(), GetType(Double))
        Dim vIntegerDivShort = vInteger / vShort
        Assert.Equal(vIntegerDivShort.GetType(), GetType(Double))
        Dim vIntegerDivUShort = vInteger / vUShort
        Assert.Equal(vIntegerDivUShort.GetType(), GetType(Double))
        Dim vIntegerDivInteger = vInteger / vInteger
        Assert.Equal(vIntegerDivInteger.GetType(), GetType(Double))
        Dim vIntegerDivUInteger = vInteger / vUInteger
        Assert.Equal(vIntegerDivUInteger.GetType(), GetType(Double))
        Dim vIntegerDivLong = vInteger / vLong
        Assert.Equal(vIntegerDivLong.GetType(), GetType(Double))
        Dim vIntegerDivULong = vInteger / vULong
        Assert.Equal(vIntegerDivULong.GetType(), GetType(Double))
        Dim vIntegerDivDecimal = vInteger / vDecimal
        Assert.Equal(vIntegerDivDecimal.GetType(), GetType(Decimal))
        Dim vIntegerDivSingle = vInteger / vSingle
        Assert.Equal(vIntegerDivSingle.GetType(), GetType(Single))
        Dim vIntegerDivDouble = vInteger / vDouble
        Assert.Equal(vIntegerDivDouble.GetType(), GetType(Double))
        Dim vIntegerDivString = vInteger / vString
        Assert.Equal(vIntegerDivString.GetType(), GetType(Double))
        Dim vUIntegerDivBoolean = vUInteger / vBoolean
        Assert.Equal(vUIntegerDivBoolean.GetType(), GetType(Double))
        Dim vUIntegerDivSByte = vUInteger / vSByte
        Assert.Equal(vUIntegerDivSByte.GetType(), GetType(Double))
        Dim vUIntegerDivByte = vUInteger / vByte
        Assert.Equal(vUIntegerDivByte.GetType(), GetType(Double))
        Dim vUIntegerDivShort = vUInteger / vShort
        Assert.Equal(vUIntegerDivShort.GetType(), GetType(Double))
        Dim vUIntegerDivUShort = vUInteger / vUShort
        Assert.Equal(vUIntegerDivUShort.GetType(), GetType(Double))
        Dim vUIntegerDivInteger = vUInteger / vInteger
        Assert.Equal(vUIntegerDivInteger.GetType(), GetType(Double))
        Dim vUIntegerDivUInteger = vUInteger / vUInteger
        Assert.Equal(vUIntegerDivUInteger.GetType(), GetType(Double))
        Dim vUIntegerDivLong = vUInteger / vLong
        Assert.Equal(vUIntegerDivLong.GetType(), GetType(Double))
        Dim vUIntegerDivULong = vUInteger / vULong
        Assert.Equal(vUIntegerDivULong.GetType(), GetType(Double))
        Dim vUIntegerDivDecimal = vUInteger / vDecimal
        Assert.Equal(vUIntegerDivDecimal.GetType(), GetType(Decimal))
        Dim vUIntegerDivSingle = vUInteger / vSingle
        Assert.Equal(vUIntegerDivSingle.GetType(), GetType(Single))
        Dim vUIntegerDivDouble = vUInteger / vDouble
        Assert.Equal(vUIntegerDivDouble.GetType(), GetType(Double))
        Dim vUIntegerDivString = vUInteger / vString
        Assert.Equal(vUIntegerDivString.GetType(), GetType(Double))
        Dim vLongDivBoolean = vLong / vBoolean
        Assert.Equal(vLongDivBoolean.GetType(), GetType(Double))
        Dim vLongDivSByte = vLong / vSByte
        Assert.Equal(vLongDivSByte.GetType(), GetType(Double))
        Dim vLongDivByte = vLong / vByte
        Assert.Equal(vLongDivByte.GetType(), GetType(Double))
        Dim vLongDivShort = vLong / vShort
        Assert.Equal(vLongDivShort.GetType(), GetType(Double))
        Dim vLongDivUShort = vLong / vUShort
        Assert.Equal(vLongDivUShort.GetType(), GetType(Double))
        Dim vLongDivInteger = vLong / vInteger
        Assert.Equal(vLongDivInteger.GetType(), GetType(Double))
        Dim vLongDivUInteger = vLong / vUInteger
        Assert.Equal(vLongDivUInteger.GetType(), GetType(Double))
        Dim vLongDivLong = vLong / vLong
        Assert.Equal(vLongDivLong.GetType(), GetType(Double))
        Dim vLongDivULong = vLong / vULong
        Assert.Equal(vLongDivULong.GetType(), GetType(Double))
        Dim vLongDivDecimal = vLong / vDecimal
        Assert.Equal(vLongDivDecimal.GetType(), GetType(Decimal))
        Dim vLongDivSingle = vLong / vSingle
        Assert.Equal(vLongDivSingle.GetType(), GetType(Single))
        Dim vLongDivDouble = vLong / vDouble
        Assert.Equal(vLongDivDouble.GetType(), GetType(Double))
        Dim vLongDivString = vLong / vString
        Assert.Equal(vLongDivString.GetType(), GetType(Double))
        Dim vULongDivBoolean = vULong / vBoolean
        Assert.Equal(vULongDivBoolean.GetType(), GetType(Double))
        Dim vULongDivSByte = vULong / vSByte
        Assert.Equal(vULongDivSByte.GetType(), GetType(Double))
        Dim vULongDivByte = vULong / vByte
        Assert.Equal(vULongDivByte.GetType(), GetType(Double))
        Dim vULongDivShort = vULong / vShort
        Assert.Equal(vULongDivShort.GetType(), GetType(Double))
        Dim vULongDivUShort = vULong / vUShort
        Assert.Equal(vULongDivUShort.GetType(), GetType(Double))
        Dim vULongDivInteger = vULong / vInteger
        Assert.Equal(vULongDivInteger.GetType(), GetType(Double))
        Dim vULongDivUInteger = vULong / vUInteger
        Assert.Equal(vULongDivUInteger.GetType(), GetType(Double))
        Dim vULongDivLong = vULong / vLong
        Assert.Equal(vULongDivLong.GetType(), GetType(Double))
        Dim vULongDivULong = vULong / vULong
        Assert.Equal(vULongDivULong.GetType(), GetType(Double))
        Dim vULongDivDecimal = vULong / vDecimal
        Assert.Equal(vULongDivDecimal.GetType(), GetType(Decimal))
        Dim vULongDivSingle = vULong / vSingle
        Assert.Equal(vULongDivSingle.GetType(), GetType(Single))
        Dim vULongDivDouble = vULong / vDouble
        Assert.Equal(vULongDivDouble.GetType(), GetType(Double))
        Dim vULongDivString = vULong / vString
        Assert.Equal(vULongDivString.GetType(), GetType(Double))
        Dim vDecimalDivBoolean = vDecimal / vBoolean
        Assert.Equal(vDecimalDivBoolean.GetType(), GetType(Decimal))
        Dim vDecimalDivSByte = vDecimal / vSByte
        Assert.Equal(vDecimalDivSByte.GetType(), GetType(Decimal))
        Dim vDecimalDivByte = vDecimal / vByte
        Assert.Equal(vDecimalDivByte.GetType(), GetType(Decimal))
        Dim vDecimalDivShort = vDecimal / vShort
        Assert.Equal(vDecimalDivShort.GetType(), GetType(Decimal))
        Dim vDecimalDivUShort = vDecimal / vUShort
        Assert.Equal(vDecimalDivUShort.GetType(), GetType(Decimal))
        Dim vDecimalDivInteger = vDecimal / vInteger
        Assert.Equal(vDecimalDivInteger.GetType(), GetType(Decimal))
        Dim vDecimalDivUInteger = vDecimal / vUInteger
        Assert.Equal(vDecimalDivUInteger.GetType(), GetType(Decimal))
        Dim vDecimalDivLong = vDecimal / vLong
        Assert.Equal(vDecimalDivLong.GetType(), GetType(Decimal))
        Dim vDecimalDivULong = vDecimal / vULong
        Assert.Equal(vDecimalDivULong.GetType(), GetType(Decimal))
        Dim vDecimalDivDecimal = vDecimal / vDecimal
        Assert.Equal(vDecimalDivDecimal.GetType(), GetType(Decimal))
        Dim vDecimalDivSingle = vDecimal / vSingle
        Assert.Equal(vDecimalDivSingle.GetType(), GetType(Single))
        Dim vDecimalDivDouble = vDecimal / vDouble
        Assert.Equal(vDecimalDivDouble.GetType(), GetType(Double))
        Dim vDecimalDivString = vDecimal / vString
        Assert.Equal(vDecimalDivString.GetType(), GetType(Double))
        Dim vSingleDivBoolean = vSingle / vBoolean
        Assert.Equal(vSingleDivBoolean.GetType(), GetType(Single))
        Dim vSingleDivSByte = vSingle / vSByte
        Assert.Equal(vSingleDivSByte.GetType(), GetType(Single))
        Dim vSingleDivByte = vSingle / vByte
        Assert.Equal(vSingleDivByte.GetType(), GetType(Single))
        Dim vSingleDivShort = vSingle / vShort
        Assert.Equal(vSingleDivShort.GetType(), GetType(Single))
        Dim vSingleDivUShort = vSingle / vUShort
        Assert.Equal(vSingleDivUShort.GetType(), GetType(Single))
        Dim vSingleDivInteger = vSingle / vInteger
        Assert.Equal(vSingleDivInteger.GetType(), GetType(Single))
        Dim vSingleDivUInteger = vSingle / vUInteger
        Assert.Equal(vSingleDivUInteger.GetType(), GetType(Single))
        Dim vSingleDivLong = vSingle / vLong
        Assert.Equal(vSingleDivLong.GetType(), GetType(Single))
        Dim vSingleDivULong = vSingle / vULong
        Assert.Equal(vSingleDivULong.GetType(), GetType(Single))
        Dim vSingleDivDecimal = vSingle / vDecimal
        Assert.Equal(vSingleDivDecimal.GetType(), GetType(Single))
        Dim vSingleDivSingle = vSingle / vSingle
        Assert.Equal(vSingleDivSingle.GetType(), GetType(Single))
        Dim vSingleDivDouble = vSingle / vDouble
        Assert.Equal(vSingleDivDouble.GetType(), GetType(Double))
        Dim vSingleDivString = vSingle / vString
        Assert.Equal(vSingleDivString.GetType(), GetType(Double))
        Dim vDoubleDivBoolean = vDouble / vBoolean
        Assert.Equal(vDoubleDivBoolean.GetType(), GetType(Double))
        Dim vDoubleDivSByte = vDouble / vSByte
        Assert.Equal(vDoubleDivSByte.GetType(), GetType(Double))
        Dim vDoubleDivByte = vDouble / vByte
        Assert.Equal(vDoubleDivByte.GetType(), GetType(Double))
        Dim vDoubleDivShort = vDouble / vShort
        Assert.Equal(vDoubleDivShort.GetType(), GetType(Double))
        Dim vDoubleDivUShort = vDouble / vUShort
        Assert.Equal(vDoubleDivUShort.GetType(), GetType(Double))
        Dim vDoubleDivInteger = vDouble / vInteger
        Assert.Equal(vDoubleDivInteger.GetType(), GetType(Double))
        Dim vDoubleDivUInteger = vDouble / vUInteger
        Assert.Equal(vDoubleDivUInteger.GetType(), GetType(Double))
        Dim vDoubleDivLong = vDouble / vLong
        Assert.Equal(vDoubleDivLong.GetType(), GetType(Double))
        Dim vDoubleDivULong = vDouble / vULong
        Assert.Equal(vDoubleDivULong.GetType(), GetType(Double))
        Dim vDoubleDivDecimal = vDouble / vDecimal
        Assert.Equal(vDoubleDivDecimal.GetType(), GetType(Double))
        Dim vDoubleDivSingle = vDouble / vSingle
        Assert.Equal(vDoubleDivSingle.GetType(), GetType(Double))
        Dim vDoubleDivDouble = vDouble / vDouble
        Assert.Equal(vDoubleDivDouble.GetType(), GetType(Double))
        Dim vDoubleDivString = vDouble / vString
        Assert.Equal(vDoubleDivString.GetType(), GetType(Double))
        Dim vStringDivBoolean = vString / vBoolean
        Assert.Equal(vStringDivBoolean.GetType(), GetType(Double))
        Dim vStringDivSByte = vString / vSByte
        Assert.Equal(vStringDivSByte.GetType(), GetType(Double))
        Dim vStringDivByte = vString / vByte
        Assert.Equal(vStringDivByte.GetType(), GetType(Double))
        Dim vStringDivShort = vString / vShort
        Assert.Equal(vStringDivShort.GetType(), GetType(Double))
        Dim vStringDivUShort = vString / vUShort
        Assert.Equal(vStringDivUShort.GetType(), GetType(Double))
        Dim vStringDivInteger = vString / vInteger
        Assert.Equal(vStringDivInteger.GetType(), GetType(Double))
        Dim vStringDivUInteger = vString / vUInteger
        Assert.Equal(vStringDivUInteger.GetType(), GetType(Double))
        Dim vStringDivLong = vString / vLong
        Assert.Equal(vStringDivLong.GetType(), GetType(Double))
        Dim vStringDivULong = vString / vULong
        Assert.Equal(vStringDivULong.GetType(), GetType(Double))
        Dim vStringDivDecimal = vString / vDecimal
        Assert.Equal(vStringDivDecimal.GetType(), GetType(Double))
        Dim vStringDivSingle = vString / vSingle
        Assert.Equal(vStringDivSingle.GetType(), GetType(Double))
        Dim vStringDivDouble = vString / vDouble
        Assert.Equal(vStringDivDouble.GetType(), GetType(Double))
        Dim vStringDivString = vString / vString
        Assert.Equal(vStringDivString.GetType(), GetType(Double))
    End Sub

    <Fact>
    Sub TestIntDiv()
        Dim vBoolean As Boolean = True
        Dim vSByte As SByte = 1
        Dim vByte As Byte = 1
        Dim vShort As Short = 1
        Dim vUShort As UShort = 1
        Dim vInteger As Integer = 1
        Dim vUInteger As UInteger = 1
        Dim vLong As Long = 1
        Dim vULong As ULong = 1
        Dim vDecimal As Decimal = 1
        Dim vSingle As Single = 1
        Dim vDouble As Double = 1
        Dim vString As String = "1"
        Dim vObject As Object = Nothing

        Dim vBooleanIntDivBoolean = vBoolean \ vBoolean
        Assert.Equal(vBooleanIntDivBoolean.GetType(), GetType(Short))
        Dim vBooleanIntDivSByte = vBoolean \ vSByte
        Assert.Equal(vBooleanIntDivSByte.GetType(), GetType(SByte))
        Dim vBooleanIntDivByte = vBoolean \ vByte
        Assert.Equal(vBooleanIntDivByte.GetType(), GetType(Short))
        Dim vBooleanIntDivShort = vBoolean \ vShort
        Assert.Equal(vBooleanIntDivShort.GetType(), GetType(Short))
        Dim vBooleanIntDivUShort = vBoolean \ vUShort
        Assert.Equal(vBooleanIntDivUShort.GetType(), GetType(Integer))
        Dim vBooleanIntDivInteger = vBoolean \ vInteger
        Assert.Equal(vBooleanIntDivInteger.GetType(), GetType(Integer))
        Dim vBooleanIntDivUInteger = vBoolean \ vUInteger
        Assert.Equal(vBooleanIntDivUInteger.GetType(), GetType(Long))
        Dim vBooleanIntDivLong = vBoolean \ vLong
        Assert.Equal(vBooleanIntDivLong.GetType(), GetType(Long))
        Dim vBooleanIntDivULong = vBoolean \ vULong
        Assert.Equal(vBooleanIntDivULong.GetType(), GetType(Long))
        Dim vBooleanIntDivDecimal = vBoolean \ vDecimal
        Assert.Equal(vBooleanIntDivDecimal.GetType(), GetType(Long))
        Dim vBooleanIntDivSingle = vBoolean \ vSingle
        Assert.Equal(vBooleanIntDivSingle.GetType(), GetType(Long))
        Dim vBooleanIntDivDouble = vBoolean \ vDouble
        Assert.Equal(vBooleanIntDivDouble.GetType(), GetType(Long))
        Dim vBooleanIntDivString = vBoolean \ vString
        Assert.Equal(vBooleanIntDivString.GetType(), GetType(Long))
        Dim vSByteIntDivBoolean = vSByte \ vBoolean
        Assert.Equal(vSByteIntDivBoolean.GetType(), GetType(SByte))
        Dim vSByteIntDivSByte = vSByte \ vSByte
        Assert.Equal(vSByteIntDivSByte.GetType(), GetType(SByte))
        Dim vSByteIntDivByte = vSByte \ vByte
        Assert.Equal(vSByteIntDivByte.GetType(), GetType(Short))
        Dim vSByteIntDivShort = vSByte \ vShort
        Assert.Equal(vSByteIntDivShort.GetType(), GetType(Short))
        Dim vSByteIntDivUShort = vSByte \ vUShort
        Assert.Equal(vSByteIntDivUShort.GetType(), GetType(Integer))
        Dim vSByteIntDivInteger = vSByte \ vInteger
        Assert.Equal(vSByteIntDivInteger.GetType(), GetType(Integer))
        Dim vSByteIntDivUInteger = vSByte \ vUInteger
        Assert.Equal(vSByteIntDivUInteger.GetType(), GetType(Long))
        Dim vSByteIntDivLong = vSByte \ vLong
        Assert.Equal(vSByteIntDivLong.GetType(), GetType(Long))
        Dim vSByteIntDivULong = vSByte \ vULong
        Assert.Equal(vSByteIntDivULong.GetType(), GetType(Long))
        Dim vSByteIntDivDecimal = vSByte \ vDecimal
        Assert.Equal(vSByteIntDivDecimal.GetType(), GetType(Long))
        Dim vSByteIntDivSingle = vSByte \ vSingle
        Assert.Equal(vSByteIntDivSingle.GetType(), GetType(Long))
        Dim vSByteIntDivDouble = vSByte \ vDouble
        Assert.Equal(vSByteIntDivDouble.GetType(), GetType(Long))
        Dim vSByteIntDivString = vSByte \ vString
        Assert.Equal(vSByteIntDivString.GetType(), GetType(Long))
        Dim vByteIntDivBoolean = vByte \ vBoolean
        Assert.Equal(vByteIntDivBoolean.GetType(), GetType(Short))
        Dim vByteIntDivSByte = vByte \ vSByte
        Assert.Equal(vByteIntDivSByte.GetType(), GetType(Short))
        Dim vByteIntDivByte = vByte \ vByte
        Assert.Equal(vByteIntDivByte.GetType(), GetType(Byte))
        Dim vByteIntDivShort = vByte \ vShort
        Assert.Equal(vByteIntDivShort.GetType(), GetType(Short))
        Dim vByteIntDivUShort = vByte \ vUShort
        Assert.Equal(vByteIntDivUShort.GetType(), GetType(UShort))
        Dim vByteIntDivInteger = vByte \ vInteger
        Assert.Equal(vByteIntDivInteger.GetType(), GetType(Integer))
        Dim vByteIntDivUInteger = vByte \ vUInteger
        Assert.Equal(vByteIntDivUInteger.GetType(), GetType(UInteger))
        Dim vByteIntDivLong = vByte \ vLong
        Assert.Equal(vByteIntDivLong.GetType(), GetType(Long))
        Dim vByteIntDivULong = vByte \ vULong
        Assert.Equal(vByteIntDivULong.GetType(), GetType(ULong))
        Dim vByteIntDivDecimal = vByte \ vDecimal
        Assert.Equal(vByteIntDivDecimal.GetType(), GetType(Long))
        Dim vByteIntDivSingle = vByte \ vSingle
        Assert.Equal(vByteIntDivSingle.GetType(), GetType(Long))
        Dim vByteIntDivDouble = vByte \ vDouble
        Assert.Equal(vByteIntDivDouble.GetType(), GetType(Long))
        Dim vByteIntDivString = vByte \ vString
        Assert.Equal(vByteIntDivString.GetType(), GetType(Long))
        Dim vShortIntDivBoolean = vShort \ vBoolean
        Assert.Equal(vShortIntDivBoolean.GetType(), GetType(Short))
        Dim vShortIntDivSByte = vShort \ vSByte
        Assert.Equal(vShortIntDivSByte.GetType(), GetType(Short))
        Dim vShortIntDivByte = vShort \ vByte
        Assert.Equal(vShortIntDivByte.GetType(), GetType(Short))
        Dim vShortIntDivShort = vShort \ vShort
        Assert.Equal(vShortIntDivShort.GetType(), GetType(Short))
        Dim vShortIntDivUShort = vShort \ vUShort
        Assert.Equal(vShortIntDivUShort.GetType(), GetType(Integer))
        Dim vShortIntDivInteger = vShort \ vInteger
        Assert.Equal(vShortIntDivInteger.GetType(), GetType(Integer))
        Dim vShortIntDivUInteger = vShort \ vUInteger
        Assert.Equal(vShortIntDivUInteger.GetType(), GetType(Long))
        Dim vShortIntDivLong = vShort \ vLong
        Assert.Equal(vShortIntDivLong.GetType(), GetType(Long))
        Dim vShortIntDivULong = vShort \ vULong
        Assert.Equal(vShortIntDivULong.GetType(), GetType(Long))
        Dim vShortIntDivDecimal = vShort \ vDecimal
        Assert.Equal(vShortIntDivDecimal.GetType(), GetType(Long))
        Dim vShortIntDivSingle = vShort \ vSingle
        Assert.Equal(vShortIntDivSingle.GetType(), GetType(Long))
        Dim vShortIntDivDouble = vShort \ vDouble
        Assert.Equal(vShortIntDivDouble.GetType(), GetType(Long))
        Dim vShortIntDivString = vShort \ vString
        Assert.Equal(vShortIntDivString.GetType(), GetType(Long))
        Dim vUShortIntDivBoolean = vUShort \ vBoolean
        Assert.Equal(vUShortIntDivBoolean.GetType(), GetType(Integer))
        Dim vUShortIntDivSByte = vUShort \ vSByte
        Assert.Equal(vUShortIntDivSByte.GetType(), GetType(Integer))
        Dim vUShortIntDivByte = vUShort \ vByte
        Assert.Equal(vUShortIntDivByte.GetType(), GetType(UShort))
        Dim vUShortIntDivShort = vUShort \ vShort
        Assert.Equal(vUShortIntDivShort.GetType(), GetType(Integer))
        Dim vUShortIntDivUShort = vUShort \ vUShort
        Assert.Equal(vUShortIntDivUShort.GetType(), GetType(UShort))
        Dim vUShortIntDivInteger = vUShort \ vInteger
        Assert.Equal(vUShortIntDivInteger.GetType(), GetType(Integer))
        Dim vUShortIntDivUInteger = vUShort \ vUInteger
        Assert.Equal(vUShortIntDivUInteger.GetType(), GetType(UInteger))
        Dim vUShortIntDivLong = vUShort \ vLong
        Assert.Equal(vUShortIntDivLong.GetType(), GetType(Long))
        Dim vUShortIntDivULong = vUShort \ vULong
        Assert.Equal(vUShortIntDivULong.GetType(), GetType(ULong))
        Dim vUShortIntDivDecimal = vUShort \ vDecimal
        Assert.Equal(vUShortIntDivDecimal.GetType(), GetType(Long))
        Dim vUShortIntDivSingle = vUShort \ vSingle
        Assert.Equal(vUShortIntDivSingle.GetType(), GetType(Long))
        Dim vUShortIntDivDouble = vUShort \ vDouble
        Assert.Equal(vUShortIntDivDouble.GetType(), GetType(Long))
        Dim vUShortIntDivString = vUShort \ vString
        Assert.Equal(vUShortIntDivString.GetType(), GetType(Long))
        Dim vIntegerIntDivBoolean = vInteger \ vBoolean
        Assert.Equal(vIntegerIntDivBoolean.GetType(), GetType(Integer))
        Dim vIntegerIntDivSByte = vInteger \ vSByte
        Assert.Equal(vIntegerIntDivSByte.GetType(), GetType(Integer))
        Dim vIntegerIntDivByte = vInteger \ vByte
        Assert.Equal(vIntegerIntDivByte.GetType(), GetType(Integer))
        Dim vIntegerIntDivShort = vInteger \ vShort
        Assert.Equal(vIntegerIntDivShort.GetType(), GetType(Integer))
        Dim vIntegerIntDivUShort = vInteger \ vUShort
        Assert.Equal(vIntegerIntDivUShort.GetType(), GetType(Integer))
        Dim vIntegerIntDivInteger = vInteger \ vInteger
        Assert.Equal(vIntegerIntDivInteger.GetType(), GetType(Integer))
        Dim vIntegerIntDivUInteger = vInteger \ vUInteger
        Assert.Equal(vIntegerIntDivUInteger.GetType(), GetType(Long))
        Dim vIntegerIntDivLong = vInteger \ vLong
        Assert.Equal(vIntegerIntDivLong.GetType(), GetType(Long))
        Dim vIntegerIntDivULong = vInteger \ vULong
        Assert.Equal(vIntegerIntDivULong.GetType(), GetType(Long))
        Dim vIntegerIntDivDecimal = vInteger \ vDecimal
        Assert.Equal(vIntegerIntDivDecimal.GetType(), GetType(Long))
        Dim vIntegerIntDivSingle = vInteger \ vSingle
        Assert.Equal(vIntegerIntDivSingle.GetType(), GetType(Long))
        Dim vIntegerIntDivDouble = vInteger \ vDouble
        Assert.Equal(vIntegerIntDivDouble.GetType(), GetType(Long))
        Dim vIntegerIntDivString = vInteger \ vString
        Assert.Equal(vIntegerIntDivString.GetType(), GetType(Long))
        Dim vUIntegerIntDivBoolean = vUInteger \ vBoolean
        Assert.Equal(vUIntegerIntDivBoolean.GetType(), GetType(Long))
        Dim vUIntegerIntDivSByte = vUInteger \ vSByte
        Assert.Equal(vUIntegerIntDivSByte.GetType(), GetType(Long))
        Dim vUIntegerIntDivByte = vUInteger \ vByte
        Assert.Equal(vUIntegerIntDivByte.GetType(), GetType(UInteger))
        Dim vUIntegerIntDivShort = vUInteger \ vShort
        Assert.Equal(vUIntegerIntDivShort.GetType(), GetType(Long))
        Dim vUIntegerIntDivUShort = vUInteger \ vUShort
        Assert.Equal(vUIntegerIntDivUShort.GetType(), GetType(UInteger))
        Dim vUIntegerIntDivInteger = vUInteger \ vInteger
        Assert.Equal(vUIntegerIntDivInteger.GetType(), GetType(Long))
        Dim vUIntegerIntDivUInteger = vUInteger \ vUInteger
        Assert.Equal(vUIntegerIntDivUInteger.GetType(), GetType(UInteger))
        Dim vUIntegerIntDivLong = vUInteger \ vLong
        Assert.Equal(vUIntegerIntDivLong.GetType(), GetType(Long))
        Dim vUIntegerIntDivULong = vUInteger \ vULong
        Assert.Equal(vUIntegerIntDivULong.GetType(), GetType(ULong))
        Dim vUIntegerIntDivDecimal = vUInteger \ vDecimal
        Assert.Equal(vUIntegerIntDivDecimal.GetType(), GetType(Long))
        Dim vUIntegerIntDivSingle = vUInteger \ vSingle
        Assert.Equal(vUIntegerIntDivSingle.GetType(), GetType(Long))
        Dim vUIntegerIntDivDouble = vUInteger \ vDouble
        Assert.Equal(vUIntegerIntDivDouble.GetType(), GetType(Long))
        Dim vUIntegerIntDivString = vUInteger \ vString
        Assert.Equal(vUIntegerIntDivString.GetType(), GetType(Long))
        Dim vLongIntDivBoolean = vLong \ vBoolean
        Assert.Equal(vLongIntDivBoolean.GetType(), GetType(Long))
        Dim vLongIntDivSByte = vLong \ vSByte
        Assert.Equal(vLongIntDivSByte.GetType(), GetType(Long))
        Dim vLongIntDivByte = vLong \ vByte
        Assert.Equal(vLongIntDivByte.GetType(), GetType(Long))
        Dim vLongIntDivShort = vLong \ vShort
        Assert.Equal(vLongIntDivShort.GetType(), GetType(Long))
        Dim vLongIntDivUShort = vLong \ vUShort
        Assert.Equal(vLongIntDivUShort.GetType(), GetType(Long))
        Dim vLongIntDivInteger = vLong \ vInteger
        Assert.Equal(vLongIntDivInteger.GetType(), GetType(Long))
        Dim vLongIntDivUInteger = vLong \ vUInteger
        Assert.Equal(vLongIntDivUInteger.GetType(), GetType(Long))
        Dim vLongIntDivLong = vLong \ vLong
        Assert.Equal(vLongIntDivLong.GetType(), GetType(Long))
        Dim vLongIntDivULong = vLong \ vULong
        Assert.Equal(vLongIntDivULong.GetType(), GetType(Long))
        Dim vLongIntDivDecimal = vLong \ vDecimal
        Assert.Equal(vLongIntDivDecimal.GetType(), GetType(Long))
        Dim vLongIntDivSingle = vLong \ vSingle
        Assert.Equal(vLongIntDivSingle.GetType(), GetType(Long))
        Dim vLongIntDivDouble = vLong \ vDouble
        Assert.Equal(vLongIntDivDouble.GetType(), GetType(Long))
        Dim vLongIntDivString = vLong \ vString
        Assert.Equal(vLongIntDivString.GetType(), GetType(Long))
        Dim vULongIntDivBoolean = vULong \ vBoolean
        Assert.Equal(vULongIntDivBoolean.GetType(), GetType(Long))
        Dim vULongIntDivSByte = vULong \ vSByte
        Assert.Equal(vULongIntDivSByte.GetType(), GetType(Long))
        Dim vULongIntDivByte = vULong \ vByte
        Assert.Equal(vULongIntDivByte.GetType(), GetType(ULong))
        Dim vULongIntDivShort = vULong \ vShort
        Assert.Equal(vULongIntDivShort.GetType(), GetType(Long))
        Dim vULongIntDivUShort = vULong \ vUShort
        Assert.Equal(vULongIntDivUShort.GetType(), GetType(ULong))
        Dim vULongIntDivInteger = vULong \ vInteger
        Assert.Equal(vULongIntDivInteger.GetType(), GetType(Long))
        Dim vULongIntDivUInteger = vULong \ vUInteger
        Assert.Equal(vULongIntDivUInteger.GetType(), GetType(ULong))
        Dim vULongIntDivLong = vULong \ vLong
        Assert.Equal(vULongIntDivLong.GetType(), GetType(Long))
        Dim vULongIntDivULong = vULong \ vULong
        Assert.Equal(vULongIntDivULong.GetType(), GetType(ULong))
        Dim vULongIntDivDecimal = vULong \ vDecimal
        Assert.Equal(vULongIntDivDecimal.GetType(), GetType(Long))
        Dim vULongIntDivSingle = vULong \ vSingle
        Assert.Equal(vULongIntDivSingle.GetType(), GetType(Long))
        Dim vULongIntDivDouble = vULong \ vDouble
        Assert.Equal(vULongIntDivDouble.GetType(), GetType(Long))
        Dim vULongIntDivString = vULong \ vString
        Assert.Equal(vULongIntDivString.GetType(), GetType(Long))
        Dim vDecimalIntDivBoolean = vDecimal \ vBoolean
        Assert.Equal(vDecimalIntDivBoolean.GetType(), GetType(Long))
        Dim vDecimalIntDivSByte = vDecimal \ vSByte
        Assert.Equal(vDecimalIntDivSByte.GetType(), GetType(Long))
        Dim vDecimalIntDivByte = vDecimal \ vByte
        Assert.Equal(vDecimalIntDivByte.GetType(), GetType(Long))
        Dim vDecimalIntDivShort = vDecimal \ vShort
        Assert.Equal(vDecimalIntDivShort.GetType(), GetType(Long))
        Dim vDecimalIntDivUShort = vDecimal \ vUShort
        Assert.Equal(vDecimalIntDivUShort.GetType(), GetType(Long))
        Dim vDecimalIntDivInteger = vDecimal \ vInteger
        Assert.Equal(vDecimalIntDivInteger.GetType(), GetType(Long))
        Dim vDecimalIntDivUInteger = vDecimal \ vUInteger
        Assert.Equal(vDecimalIntDivUInteger.GetType(), GetType(Long))
        Dim vDecimalIntDivLong = vDecimal \ vLong
        Assert.Equal(vDecimalIntDivLong.GetType(), GetType(Long))
        Dim vDecimalIntDivULong = vDecimal \ vULong
        Assert.Equal(vDecimalIntDivULong.GetType(), GetType(Long))
        Dim vDecimalIntDivDecimal = vDecimal \ vDecimal
        Assert.Equal(vDecimalIntDivDecimal.GetType(), GetType(Long))
        Dim vDecimalIntDivSingle = vDecimal \ vSingle
        Assert.Equal(vDecimalIntDivSingle.GetType(), GetType(Long))
        Dim vDecimalIntDivDouble = vDecimal \ vDouble
        Assert.Equal(vDecimalIntDivDouble.GetType(), GetType(Long))
        Dim vDecimalIntDivString = vDecimal \ vString
        Assert.Equal(vDecimalIntDivString.GetType(), GetType(Long))
        Dim vSingleIntDivBoolean = vSingle \ vBoolean
        Assert.Equal(vSingleIntDivBoolean.GetType(), GetType(Long))
        Dim vSingleIntDivSByte = vSingle \ vSByte
        Assert.Equal(vSingleIntDivSByte.GetType(), GetType(Long))
        Dim vSingleIntDivByte = vSingle \ vByte
        Assert.Equal(vSingleIntDivByte.GetType(), GetType(Long))
        Dim vSingleIntDivShort = vSingle \ vShort
        Assert.Equal(vSingleIntDivShort.GetType(), GetType(Long))
        Dim vSingleIntDivUShort = vSingle \ vUShort
        Assert.Equal(vSingleIntDivUShort.GetType(), GetType(Long))
        Dim vSingleIntDivInteger = vSingle \ vInteger
        Assert.Equal(vSingleIntDivInteger.GetType(), GetType(Long))
        Dim vSingleIntDivUInteger = vSingle \ vUInteger
        Assert.Equal(vSingleIntDivUInteger.GetType(), GetType(Long))
        Dim vSingleIntDivLong = vSingle \ vLong
        Assert.Equal(vSingleIntDivLong.GetType(), GetType(Long))
        Dim vSingleIntDivULong = vSingle \ vULong
        Assert.Equal(vSingleIntDivULong.GetType(), GetType(Long))
        Dim vSingleIntDivDecimal = vSingle \ vDecimal
        Assert.Equal(vSingleIntDivDecimal.GetType(), GetType(Long))
        Dim vSingleIntDivSingle = vSingle \ vSingle
        Assert.Equal(vSingleIntDivSingle.GetType(), GetType(Long))
        Dim vSingleIntDivDouble = vSingle \ vDouble
        Assert.Equal(vSingleIntDivDouble.GetType(), GetType(Long))
        Dim vSingleIntDivString = vSingle \ vString
        Assert.Equal(vSingleIntDivString.GetType(), GetType(Long))
        Dim vDoubleIntDivBoolean = vDouble \ vBoolean
        Assert.Equal(vDoubleIntDivBoolean.GetType(), GetType(Long))
        Dim vDoubleIntDivSByte = vDouble \ vSByte
        Assert.Equal(vDoubleIntDivSByte.GetType(), GetType(Long))
        Dim vDoubleIntDivByte = vDouble \ vByte
        Assert.Equal(vDoubleIntDivByte.GetType(), GetType(Long))
        Dim vDoubleIntDivShort = vDouble \ vShort
        Assert.Equal(vDoubleIntDivShort.GetType(), GetType(Long))
        Dim vDoubleIntDivUShort = vDouble \ vUShort
        Assert.Equal(vDoubleIntDivUShort.GetType(), GetType(Long))
        Dim vDoubleIntDivInteger = vDouble \ vInteger
        Assert.Equal(vDoubleIntDivInteger.GetType(), GetType(Long))
        Dim vDoubleIntDivUInteger = vDouble \ vUInteger
        Assert.Equal(vDoubleIntDivUInteger.GetType(), GetType(Long))
        Dim vDoubleIntDivLong = vDouble \ vLong
        Assert.Equal(vDoubleIntDivLong.GetType(), GetType(Long))
        Dim vDoubleIntDivULong = vDouble \ vULong
        Assert.Equal(vDoubleIntDivULong.GetType(), GetType(Long))
        Dim vDoubleIntDivDecimal = vDouble \ vDecimal
        Assert.Equal(vDoubleIntDivDecimal.GetType(), GetType(Long))
        Dim vDoubleIntDivSingle = vDouble \ vSingle
        Assert.Equal(vDoubleIntDivSingle.GetType(), GetType(Long))
        Dim vDoubleIntDivDouble = vDouble \ vDouble
        Assert.Equal(vDoubleIntDivDouble.GetType(), GetType(Long))
        Dim vDoubleIntDivString = vDouble \ vString
        Assert.Equal(vDoubleIntDivString.GetType(), GetType(Long))
        Dim vStringIntDivBoolean = vString \ vBoolean
        Assert.Equal(vStringIntDivBoolean.GetType(), GetType(Long))
        Dim vStringIntDivSByte = vString \ vSByte
        Assert.Equal(vStringIntDivSByte.GetType(), GetType(Long))
        Dim vStringIntDivByte = vString \ vByte
        Assert.Equal(vStringIntDivByte.GetType(), GetType(Long))
        Dim vStringIntDivShort = vString \ vShort
        Assert.Equal(vStringIntDivShort.GetType(), GetType(Long))
        Dim vStringIntDivUShort = vString \ vUShort
        Assert.Equal(vStringIntDivUShort.GetType(), GetType(Long))
        Dim vStringIntDivInteger = vString \ vInteger
        Assert.Equal(vStringIntDivInteger.GetType(), GetType(Long))
        Dim vStringIntDivUInteger = vString \ vUInteger
        Assert.Equal(vStringIntDivUInteger.GetType(), GetType(Long))
        Dim vStringIntDivLong = vString \ vLong
        Assert.Equal(vStringIntDivLong.GetType(), GetType(Long))
        Dim vStringIntDivULong = vString \ vULong
        Assert.Equal(vStringIntDivULong.GetType(), GetType(Long))
        Dim vStringIntDivDecimal = vString \ vDecimal
        Assert.Equal(vStringIntDivDecimal.GetType(), GetType(Long))
        Dim vStringIntDivSingle = vString \ vSingle
        Assert.Equal(vStringIntDivSingle.GetType(), GetType(Long))
        Dim vStringIntDivDouble = vString \ vDouble
        Assert.Equal(vStringIntDivDouble.GetType(), GetType(Long))
        Dim vStringIntDivString = vString \ vString
        Assert.Equal(vStringIntDivString.GetType(), GetType(Long))
    End Sub
End Class
