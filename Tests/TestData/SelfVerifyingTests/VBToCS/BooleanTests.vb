Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Xunit

Public Class BooleanTests

    Private Property CallsA As Integer
    Private _a As Boolean?
    Private Property A As Boolean?
        Get
            CallsA += 1
            Return _a
        End Get
        Set
            _a = Value
        End Set
    End Property

    Private Property CallsB As Integer
    Private _b As Boolean?
    Private Property B As Boolean?
        Get
            CallsB += 1
            Return _b
        End Get
        Set
            _b = Value
        End Set
    End Property

    Private Property CallsBoolProp As Integer
    Private _boolProp As Boolean
    Private Property BoolProp As Boolean
        Get
            CallsBoolProp += 1
            Return _boolProp
        End Get
        Set
            _boolProp = Value
        End Set
    End Property

    <Fact>
    Public Sub CastingNullableBooleanToBoolean()
        Dim nullBoolean As Boolean? = Nothing
        Dim trueBool As Boolean? = True
        Dim falseBool As Boolean? = False

        Dim act = Sub()
            Dim x As Boolean = nullBoolean
        End Sub 
        Dim a As Boolean = trueBool
        Dim b As Boolean = falseBool

        Assert.Throws(Of InvalidOperationException)(act)
        Assert.True(a)
        Assert.False(b)
    End Sub

    <Fact>
    Public Sub ComplexBinaryExpressionsAreCorrectlyEvaluated()
        Dim a As Integer? = 5
        Dim b As Integer? = Nothing
        Dim result As Boolean

        If a <> 0 AndAlso a <> b Then result = True Else result = False
        Assert.False(result)

        If a <> 0 OrElse a <> b Then result = True Else result = False
        Assert.True(result)

        If a <> 0 And a <> b Then result = True Else result = False
        Assert.False(result)

        If a <> 0 Or a <> b Then result = True Else result = False
        Assert.True(result)

        If b <> 0 OrElse b = 5 AndAlso (a <> 4 OrElse a = 5) Then result = True Else result = False
        Assert.False(result)

        If True <> (b = 3) Then result = True Else result = False
        Assert.False(result)

    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, Nothing)>
    <InlineData(Nothing, True, Nothing)>
    <InlineData(Nothing, False, Nothing)>
    <InlineData(True, Nothing, Nothing)>
    <InlineData(True, True, False)>
    <InlineData(True, False, True)>
    <InlineData(False, Nothing, Nothing)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub NotEqualOperatorsOnNullableBoolean(a As Boolean?, b As Boolean?, result As Boolean?)
        Dim actualResult= a <> b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, Nothing, Nothing)>
    <InlineData(True, True, False)>
    <InlineData(True, False, True)>
    <InlineData(False, Nothing, Nothing)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub NotEqualOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?)
        Dim actualResult= a <> b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(Nothing, True, Nothing)>
    <InlineData(Nothing, False, Nothing)>
    <InlineData(True, True, False)>
    <InlineData(True, False, True)>
    <InlineData(False, True, True)>
    <InlineData(False, False, False)>
    Public Sub NotEqualOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?)
        Dim actualResult= a <> b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, Nothing)>
    <InlineData(Nothing, True, Nothing)>
    <InlineData(Nothing, False, Nothing)>
    <InlineData(True, Nothing, Nothing)>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, Nothing, Nothing)>
    <InlineData(False, True, False)>
    <InlineData(False, False, True)>
    Public Sub EqualOperatorsOnNullableBoolean(a As Boolean?, b As Boolean?, result As Boolean?)
        Dim actualResult= a = b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(True, Nothing, Nothing)>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, Nothing, Nothing)>
    <InlineData(False, True, False)>
    <InlineData(False, False, True)>
    Public Sub EqualOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?)
        Dim actualResult= a = b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(Nothing, True, Nothing)>
    <InlineData(Nothing, False, Nothing)>
    <InlineData(True, True, True)>
    <InlineData(True, False, False)>
    <InlineData(False, True, False)>
    <InlineData(False, False, True)>
    Public Sub EqualOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?)
        Dim actualResult= a = b
        Assert.Equal(actualResult, result)
        Assert.Equal(Not actualResult, Not result)
    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, Nothing, 21505, 10, 6)>
    <InlineData(Nothing, True, Nothing, 21505, 10, 6)>
    <InlineData(Nothing, False, False, 21505, 9, 6)>
    <InlineData(True, Nothing, Nothing, 21505, 10, 6)>
    <InlineData(True, True, True, 5187, 10, 6)>
    <InlineData(True, False, False, 21505, 9, 6)>
    <InlineData(False, Nothing, False, 21505, 10, 3)>
    <InlineData(False, True, False, 21505, 10, 3)>
    <InlineData(False, False, False, 21505, 9, 3)>
    Public Sub AndOperatorsOnNullableBoolean(a As Boolean?, b As Boolean?, result As Boolean?, expectedCheck as Integer, expectedCallsA As Integer, expectedCallsB As Integer)
        Dim check1 As Integer = 1
        Dim check2 As Integer = 1
        Me.A = a
        Me.B = b
        
        Dim actual = a And b
        Dim actualAndAlso = a AndAlso b

        Dim actualAndProp = Me.A And Me.B
        Dim actualAndAlsoProp = Me.A AndAlso Me.B

        Dim actualAndMix = b And Me.A
        Dim actualAndAlsoMix = b AndAlso Me.A
        Dim actualAndMix2 = Me.A And b
        Dim actualAndAlsoMix2 = Me.A AndAlso b

        If a And b Then check1 *= 3 Else check1 *= 5
        If a AndAlso b Then check1 *= 7 Else check1 *= 11
        If Me.A And Me.B Then check1 *= 13 Else check1 *= 17
        If Me.A AndAlso Me.B Then check1 *= 19 Else check1 *= 23

        If a And Me.B Then check2 *= 3 Else check2 *= 5
        If a AndAlso Me.B Then check2 *= 7 Else check2 *= 11
        If Me.A And b Then check2 *= 13 Else check2 *= 17
        If Me.A AndAlso b Then check2 *= 19 Else check2 *= 23

        Assert.Equal(result, actual)
        Assert.Equal(result, actualAndAlso)
        Assert.Equal(Not result, Not actual)
        Assert.Equal(Not result, Not actualAndAlso)

        Assert.Equal(result, actualAndProp)
        Assert.Equal(result, actualAndAlsoProp)
        Assert.Equal(Not result, Not actualAndProp)
        Assert.Equal(Not result, Not actualAndAlsoProp)

        Assert.Equal(result, actualAndMix)
        Assert.Equal(result, actualAndAlsoMix)
        Assert.Equal(result, actualAndMix2)
        Assert.Equal(result, actualAndAlsoMix2)
        Assert.Equal(Not result, Not actualAndMix)
        Assert.Equal(Not result, Not actualAndAlsoMix)
        Assert.Equal(Not result, Not actualAndMix2)
        Assert.Equal(Not result, Not actualAndAlsoMix2)

        Assert.Equal(expectedCallsA, Me.CallsA)
        Assert.Equal(expectedCallsB, Me.CallsB)
        Assert.Equal(expectedCheck, check1)
        Assert.Equal(expectedCheck, check2)
    End Sub

    <Theory>
    <InlineData(True, Nothing, Nothing, 21505, 7)>
    <InlineData(True, True, True, 5187, 7)>
    <InlineData(True, False, False, 21505, 7)>
    <InlineData(False, Nothing, False, 21505, 4)>
    <InlineData(False, True, False, 21505, 4)>
    <InlineData(False, False, False, 21505, 4)>
    Public Sub AndOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?, expectedCheck As Integer, expectedCallsB As Integer)
        Dim check1 As Integer = 1
        Dim check2 As Integer = 1
        Me.BoolProp = a
        Me.B = b

        Dim actualAnd = a And b
        Dim actualAndAlso = a AndAlso b
        Dim actualAndWithConst = False And b
        Dim actualAndAlsoWithConst = False AndAlso b
        Dim actualAndProp = Me.BoolProp And Me.B
        Dim actualAndAlsoProp = Me.BoolProp AndAlso Me.B
        Dim actualAndPropWithConst = False And Me.B
        Dim actualAndAlsoPropWithConst = False AndAlso Me.B

        If Me.BoolProp And Me.B Then check1 *= 3 Else check1 *= 5
        If Me.BoolProp AndAlso Me.B Then check1 *= 7 Else check1 *= 11
        If a And Me.B Then check1 *= 13 Else check1 *= 17
        If a AndAlso Me.B Then check1 *= 19 Else check1 *= 23

        If Me.BoolProp And b Then check2 *= 3 Else check2 *= 5
        If Me.BoolProp AndAlso b Then check2 *= 7 Else check2 *= 11
        If a And b Then check2 *= 13 Else check2 *= 17
        If a AndAlso b Then check2 *= 19 Else check2 *= 23

        Assert.Equal(result, actualAnd)
        Assert.Equal(result, actualAndAlso)
        Assert.Equal(False, actualAndWithConst)
        Assert.Equal(False, actualAndAlsoWithConst)
        Assert.Equal(result, actualAndProp)
        Assert.Equal(result, actualAndAlsoProp)
        Assert.Equal(False, actualAndPropWithConst)
        Assert.Equal(False, actualAndAlsoPropWithConst)

        Assert.Equal(Not result, Not actualAnd)
        Assert.Equal(Not result, Not actualAndAlso)
        Assert.Equal(True, Not actualAndWithConst)
        Assert.Equal(True, Not actualAndAlsoWithConst)

        Assert.Equal(expectedCheck, check1)
        Assert.Equal(expectedCheck, check2)
        Assert.Equal(expectedCallsB, Me.CallsB)
        Assert.Equal(6, Me.CallsBoolProp)
    End Sub

    <Theory>
    <InlineData(Nothing, True, Nothing, 21505, 6)>
    <InlineData(Nothing, False, False, 21505, 6)>
    <InlineData(True, True, True, 5187, 6)>
    <InlineData(True, False, False, 21505, 6)>
    <InlineData(False, True, False, 21505, 3)>
    <InlineData(False, False, False, 21505, 3)>
    Public Sub AndOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?, expectedCheck As Integer, expectedCallsBoolProp As Integer)
        Dim check1 As Integer = 1
        Dim check2 As Integer = 1
        Me.A = a
        Me.BoolProp = b

        Dim actualAnd = a And b
        Dim actualAndAlso = a AndAlso b
        Dim actualAndWithConst = a And False
        Dim actualAndAlsoWithConst = a AndAlso False
        Dim actualAndProp = Me.A And Me.BoolProp
        Dim actualAndAlsoProp = Me.A AndAlso Me.BoolProp
        Dim actualAndPropWithConst = Me.A And False
        Dim actualAndAlsoPropWithConst = Me.A AndAlso False

        If Me.A And Me.BoolProp Then check1 *= 3 Else check1 *= 5
        If Me.A AndAlso Me.BoolProp Then check1 *= 7 Else check1 *= 11
        If Me.A And b Then check1 *= 13 Else check1 *= 17
        If Me.A AndAlso b Then check1 *= 19 Else check1 *= 23

        If a And Me.BoolProp Then check2 *= 3 Else check2 *= 5
        If a AndAlso Me.BoolProp Then check2 *= 7 Else check2 *= 11
        If a And b Then check2 *= 13 Else check2 *= 17
        If a AndAlso b Then check2 *= 19 Else check2 *= 23

        Assert.Equal(result, actualAnd)
        Assert.Equal(result, actualAndAlso)
        Assert.Equal(False, actualAndWithConst)
        Assert.Equal(False, actualAndAlsoWithConst)
        Assert.Equal(result, actualAndProp)
        Assert.Equal(result, actualAndAlsoProp)
        Assert.Equal(False, actualAndPropWithConst)
        Assert.Equal(False, actualAndAlsoPropWithConst)

        Assert.Equal(Not result, Not actualAnd)
        Assert.Equal(Not result, Not actualAndAlso)
        Assert.Equal(True, Not actualAndWithConst)
        Assert.Equal(True, Not actualAndAlsoWithConst)

        Assert.Equal(expectedCheck, check1)
        Assert.Equal(expectedCheck, check2)
        Assert.Equal(8, Me.CallsA)
        Assert.Equal(expectedCallsBoolProp, Me.CallsBoolProp)
    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, Nothing, 21505, 10, 6)>
    <InlineData(Nothing, True, True, 5187, 9, 6)>
    <InlineData(Nothing, False, Nothing, 21505, 10, 6)>
    <InlineData(True, Nothing, True, 5187, 10, 3)>
    <InlineData(True, True, True, 5187, 9, 3)>
    <InlineData(True, False, True, 5187, 10, 3)>
    <InlineData(False, Nothing, Nothing, 21505, 10, 6)>
    <InlineData(False, True, True, 5187, 9, 6)>
    <InlineData(False, False, False, 21505, 10, 6)>
    Public Sub OrOperatorsOnNullableBoolean(a As Boolean?, b As Boolean?, result As Boolean?, expectedCheck as Integer, expectedCallsA As Integer, expectedCallsB As Integer)
        Dim check1 As Integer = 1
        Dim check2 As Integer = 1
        Me.A = a
        Me.B = b
        
        Dim actual = a Or b
        Dim actualOrElse = a OrElse b

        Dim actualOrProp = Me.A Or Me.B
        Dim actualOrElseProp = Me.A OrElse Me.B

        Dim actualOrMix = b Or Me.A
        Dim actualOrElseMix = b OrElse Me.A
        Dim actualOrMix2 = Me.A Or b
        Dim actualOrElseMix2 = Me.A OrElse b

        If a Or b Then check1 *= 3 Else check1 *= 5
        If a OrElse b Then check1 *= 7 Else check1 *= 11
        If Me.A Or Me.B Then check1 *= 13 Else check1 *= 17
        If Me.A OrElse Me.B Then check1 *= 19 Else check1 *= 23

        If a Or Me.B Then check2 *= 3 Else check2 *= 5
        If a OrElse Me.B Then check2 *= 7 Else check2 *= 11
        If Me.A Or b Then check2 *= 13 Else check2 *= 17
        If Me.A OrElse b Then check2 *= 19 Else check2 *= 23

        Assert.Equal(result, actual)
        Assert.Equal(result, actualOrElse)
        Assert.Equal(Not result, Not actual)
        Assert.Equal(Not result, Not actualOrElse)

        Assert.Equal(result, actualOrProp)
        Assert.Equal(result, actualOrElseProp)
        Assert.Equal(Not result, Not actualOrProp)
        Assert.Equal(Not result, Not actualOrElseProp)

        Assert.Equal(result, actualOrMix)
        Assert.Equal(result, actualOrElseMix)
        Assert.Equal(result, actualOrMix2)
        Assert.Equal(result, actualOrElseMix2)
        Assert.Equal(Not result, Not actualOrMix)
        Assert.Equal(Not result, Not actualOrElseMix)
        Assert.Equal(Not result, Not actualOrMix2)
        Assert.Equal(Not result, Not actualOrElseMix2)

        Assert.Equal(expectedCallsA, Me.CallsA)
        Assert.Equal(expectedCallsB, Me.CallsB)
        Assert.Equal(expectedCheck, check1)
        Assert.Equal(expectedCheck, check2)
    End Sub

    <Theory>
    <InlineData(True, Nothing, True, 5187, 4)>
    <InlineData(True, True, True, 5187, 4)>
    <InlineData(True, False, True, 5187, 4)>
    <InlineData(False, Nothing, Nothing, 21505, 7)>
    <InlineData(False, True, True, 5187, 7)>
    <InlineData(False, False, False, 21505, 7)>
    Public Sub OrOperatorsOnNormalAndNullableBooleans(a As Boolean, b As Boolean?, result As Boolean?, expectedCheck As Integer, expectedCallsB As Integer)
        Dim check1 As Integer = 1
        Dim check2 As Integer = 1
        Me.BoolProp = a
        Me.B = b

        Dim actualOr = a Or b
        Dim actualOrElse = a OrElse b
        Dim actualOrWithConst = True Or b
        Dim actualOrElseWithConst = True OrElse b
        Dim actualOrProp = Me.BoolProp Or Me.B
        Dim actualOrElseProp = Me.BoolProp OrElse Me.B
        Dim actualOrPropWithConst = True Or Me.B
        Dim actualOrElsePropWithConst = True OrElse Me.B

        If Me.BoolProp Or Me.B Then check1 *= 3 Else check1 *= 5
        If Me.BoolProp OrElse Me.B Then check1 *= 7 Else check1 *= 11
        If a Or Me.B Then check1 *= 13 Else check1 *= 17
        If a OrElse Me.B Then check1 *= 19 Else check1 *= 23

        If Me.BoolProp Or b Then check2 *= 3 Else check2 *= 5
        If Me.BoolProp OrElse b Then check2 *= 7 Else check2 *= 11
        If a Or b Then check2 *= 13 Else check2 *= 17
        If a OrElse b Then check2 *= 19 Else check2 *= 23

        Assert.Equal(result, actualOr)
        Assert.Equal(result, actualOrElse)
        Assert.Equal(True, actualOrWithConst)
        Assert.Equal(True, actualOrElseWithConst)
        Assert.Equal(result, actualOrProp)
        Assert.Equal(result, actualOrElseProp)
        Assert.Equal(True, actualOrPropWithConst)
        Assert.Equal(True, actualOrElsePropWithConst)

        Assert.Equal(Not result, Not actualOr)
        Assert.Equal(Not result, Not actualOrElse)
        Assert.Equal(False, Not actualOrWithConst)
        Assert.Equal(False, Not actualOrElseWithConst)

        Assert.Equal(expectedCheck, check1)
        Assert.Equal(expectedCheck, check2)
        Assert.Equal(expectedCallsB, Me.CallsB)
        Assert.Equal(6, Me.CallsBoolProp)
    End Sub

    <Theory>
    <InlineData(Nothing, True, True, 5187, 6)>
    <InlineData(Nothing, False, Nothing, 21505, 6)>
    <InlineData(True, True, True, 5187, 3)>
    <InlineData(True, False, True, 5187, 3)>
    <InlineData(False, True, True, 5187, 6)>
    <InlineData(False, False, False, 21505, 6)>
    Public Sub OrOperatorsOnNullableAndNormalBoolean(a As Boolean?, b As Boolean, result As Boolean?, expectedCheck As Integer, expectedCallsBoolProp As Integer)
        Dim check1 As Integer = 1
        Dim check2 As Integer = 1
        Me.A = a
        Me.BoolProp = b

        Dim actualOr = a Or b
        Dim actualOrElse = a OrElse b
        Dim actualOrWithConst = a Or True
        Dim actualOrElseWithConst = a OrElse True
        Dim actualOrProp = Me.A Or Me.BoolProp
        Dim actualOrElseProp = Me.A OrElse Me.BoolProp
        Dim actualOrPropWithConst = Me.A Or True
        Dim actualOrElsePropWithConst = Me.A OrElse True

        If Me.A Or Me.BoolProp Then check1 *= 3 Else check1 *= 5
        If Me.A OrElse Me.BoolProp Then check1 *= 7 Else check1 *= 11
        If Me.A Or b Then check1 *= 13 Else check1 *= 17
        If Me.A OrElse b Then check1 *= 19 Else check1 *= 23

        If a Or Me.BoolProp Then check2 *= 3 Else check2 *= 5
        If a OrElse Me.BoolProp Then check2 *= 7 Else check2 *= 11
        If a Or b Then check2 *= 13 Else check2 *= 17
        If a OrElse b Then check2 *= 19 Else check2 *= 23

        Assert.Equal(result, actualOr)
        Assert.Equal(result, actualOrElse)
        Assert.Equal(True, actualOrWithConst)
        Assert.Equal(True, actualOrElseWithConst)
        Assert.Equal(result, actualOrProp)
        Assert.Equal(result, actualOrElseProp)
        Assert.Equal(True, actualOrPropWithConst)
        Assert.Equal(True, actualOrElsePropWithConst)

        Assert.Equal(Not result, Not actualOr)
        Assert.Equal(Not result, Not actualOrElse)
        Assert.Equal(False, Not actualOrWithConst)
        Assert.Equal(False, Not actualOrElseWithConst)

        Assert.Equal(expectedCheck, check1)
        Assert.Equal(expectedCheck, check2)
        Assert.Equal(8, Me.CallsA)
        Assert.Equal(expectedCallsBoolProp, Me.CallsBoolProp)
    End Sub

    <Theory>
    <InlineData(Nothing, Nothing, 55, 2)>
    <InlineData(True, False, 55, 2)>
    <InlineData(False, True, 21, 2)>
    Public Sub NegateNullableBoolean(x as Boolean?, expectedResult As Boolean?, expectedCheck As Integer, expectedCalls As Integer)
        Dim check As Integer = 1
        Me.A = x

        Dim result1 = Not x
        Dim result2 = Not Me.A

        If Not x Then check *= 3 Else check *= 5
        If Not Me.A Then check *= 7 Else check *= 11

        Assert.Equal(expectedResult, result1)
        Assert.Equal(expectedResult, result2)
        Assert.Equal(expectedCheck, check)
        Assert.Equal(expectedCalls, Me.CallsA)
    End Sub

    <Fact>
    Public Sub NullableBoolsOnNothing()
        Dim x As Object = Nothing
        Dim res = 1

        If Not x?.Equals(4) Then res *= 2 Else res *= 3  '3 branch taken

        res *= If(x?.Equals(4), 5, 7) '7 branch taken

        Assert.Equal(21, res)
    End Sub

    <Fact>
     Public Sub NullableBoolsFalse()
        Dim x As Object = 5
        Dim res = 1

        If (Not x?.Equals(4))' x != 4
            res *= 2 'Branch taken
        Else
            res *= 3
        End If


        If (x?.Equals(4))' x == 4
            res *= 5
        Else
            res *= 7 'Branch taken
        End If

        Assert.Equal(14, res)
    End Sub

    <Fact>
    Public Sub NullableBoolsTrue()
        Dim x As Object = 4
        Dim res = 1

        If (Not x?.Equals(4)) Then ' x != 4
            res *= 2
        Else
            res *= 3 'Branch taken
        End If


        If (x?.Equals(4)) Then ' x == 4
            res *= 5 'Branch taken
        Else
            res *= 7
        End If

        Assert.Equal(15, res)
    End Sub

    <Fact>
    Public Sub VisualBasicEqualityOfNormalObjectsNotSubjectToSpecialStringConversionRules()
        Dim a1 As Object = 3
        Dim a2 As Object = 3
        Dim b As Object = 4
        Assert.True(a1 = a2, "Identical values stored in objects should be equal")
        Assert.False(a1 = b, "Different values stored in objects should not be equal")
    End Sub
End Class

